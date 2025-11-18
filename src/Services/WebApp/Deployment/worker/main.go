package main

import (
	"bytes"
	"context"
	"encoding/json"
	"fmt"
	"io"
	"log"
	"mime/multipart"
	"mycrocloud/worker/logutil"
	"net/http"
	"os"
	"path/filepath"
	"strings"
	"sync"
	"time"

	"github.com/docker/docker/api/types/container"
	"github.com/docker/docker/api/types/mount"
	"github.com/docker/docker/client"
	"github.com/fluent/fluent-logger-golang/fluent"
	"github.com/joho/godotenv"
	"github.com/streadway/amqp"
)

func failOnError(err error, msg string) {
	if err != nil {
		log.Fatalf("%s: %s", msg, err)
	}
}

// MaxConcurrentJobs is the limit for concurrent jobs being processed
const MaxConcurrentJobs = 3

var LogIndex = os.Getenv("ES_BUILD_LOGS_INDEX")
var ES_VERSION = os.Getenv("ES_VERSION")

func logFluentd(l *fluent.Fluent, msg string, jobID string) {
	data := map[string]string{
		"log":      msg,
		"build_id": jobID,
	}

	tag := "app.worker"

	err := l.PostWithTime(tag, time.Now(), data)
	if err != nil {
		log.Printf("Failed to send log to fluentd: %v", err)
	}
}

func getLogConfig() container.LogConfig {
	addr := stripProtocol(os.Getenv("BUILDER_FLUENTD_ADDRESS"))

	cfg := container.LogConfig{Type: "fluentd"}
	cfg.Config = map[string]string{
		"fluentd-address": addr,
		"tag":             "app.builder",
		"labels":          "build_id",
	}

	log.Printf("Fluentd logging enabled (%s)", addr)
	return cfg
}

// ProcessJob simulates job processing asynchronously
func ProcessJob(jsonString string, wg *sync.WaitGroup, ch *amqp.Channel, l *fluent.Fluent) {
	defer wg.Done()

	var buildMsg BuildMessage
	err := json.Unmarshal([]byte(jsonString), &buildMsg)
	failOnError(err, "Failed to unmarshal JSON")
	log.Printf("Processing... Id: %s, RepoFullName: %s", buildMsg.JobId, buildMsg.RepoFullName)
	logFluentd(l, "Processing", buildMsg.JobId)

	ctx := context.Background()
	cli, err := client.NewClientWithOpts(client.FromEnv, client.WithAPIVersionNegotiation())
	failOnError(err, "Failed to create docker client")

	// Create a container
	log.Printf("Creating container")
	builderImage := os.Getenv("BUILDER_IMAGE")

	jobID := buildMsg.JobId
	baseOut := getOutputBaseDir()
	jobOut := filepath.Join(baseOut, jobID)

	if err := os.MkdirAll(jobOut, 0755); err != nil {
		log.Fatalf("❌ Failed to create job output dir: %v", err)
	}

	log.Printf("📦 Starting build job: %s", jobID)
	log.Printf("HOST_OUT_DIR: %s", baseOut)
	log.Printf("Job output dir: %s", jobOut)

	mounts := []mount.Mount{
		{
			Type:   mount.TypeBind,
			Source: jobOut,
			Target: "/output",
		},
	}

	logConf := getLogConfig()

	if logConf.Type == "fluentd" {
		addr := ""
		if logConf.Config != nil {
			addr = logConf.Config["fluentd-address"]
		}
		// If using a unix socket address, mount it into the builder container.
		// We do not pre-check the path from inside the worker container; the
		// Docker daemon will validate the source path on the host.
		if strings.HasPrefix(addr, "unix://") {
			socketPath := strings.TrimPrefix(addr, "unix://")
			mounts = append(mounts, mount.Mount{
				Type:   mount.TypeBind,
				Source: socketPath,
				Target: socketPath,
			})
			log.Printf("[builder:%s] Configured fluentd socket mount %s", buildMsg.JobId, socketPath)
		}
	}

	autoRemove := os.Getenv("BUILDER_AUTO_REMOVE") != "false"

	resp, err := cli.ContainerCreate(ctx,
		&container.Config{
			Image: builderImage,
			Tty:   false,
			Env: []string{
				"REPO_URL=" + buildMsg.CloneUrl,
				"WORK_DIR=" + buildMsg.Directory,
				"OUT_DIR=" + buildMsg.OutDir,
				"INSTALL_CMD=" + buildMsg.InstallCommand,
				"BUILD_CMD=" + buildMsg.BuildCommand,
			},
			Labels: map[string]string{"build_id": buildMsg.JobId},
		},
		&container.HostConfig{
			Mounts:     mounts,
			LogConfig:  logConf,
			AutoRemove: autoRemove,
		},
		nil, nil, "")
	failOnError(err, "Failed to create container")

	// Start the container
	log.Printf("Starting container")
	err = cli.ContainerStart(ctx, resp.ID, container.StartOptions{})
	failOnError(err, "Failed to start container")

	publishJobStatusChangedEventMessage(ch, JobStatusChangedEventMessage{
		JobId:       buildMsg.JobId,
		Status:      Started,
		ContainerId: resp.ID,
	})

	// Wait for the container to finish
	statusCh, errCh := cli.ContainerWait(ctx, resp.ID, container.WaitConditionNotRunning)
	select {
	case err := <-errCh:
		failOnError(err, "Failed to wait for container")
		publishJobStatusChangedEventMessage(ch, JobStatusChangedEventMessage{
			JobId:  buildMsg.JobId,
			Status: Failed,
		})

	case status := <-statusCh:
		log.Printf("Container finished with status %d", status.StatusCode)
	}

	//distDir := path.Join("/output", repo.Id)
	log.Printf("Dist dir: %s", jobOut)
	shouldUploadArtifacts := os.Getenv("UPLOAD_ARTIFACTS") != "false"

	if shouldUploadArtifacts {
		UploadArtifacts(jobOut, strings.TrimSuffix(buildMsg.ArtifactsUploadUrl, "/"))
	}

	// publish completion message
	publishJobStatusChangedEventMessage(ch, JobStatusChangedEventMessage{
		JobId:  buildMsg.JobId,
		Status: Done,
	})

	log.Printf("Finished processing. Id: %s", buildMsg.JobId)
	logFluentd(l, "Finished processing", buildMsg.JobId)
}

func getOutputBaseDir() string {
	dir := os.Getenv("HOST_OUT_DIR")
	if dir == "" {
		log.Fatal("❌ HOST_OUT_DIR must be set (compose-only mode)")
	}
	return dir
}

// UploadArtifacts uploads all files from a directory (recursively) to the upload URL
func UploadArtifacts(dir string, uploadUrl string) error {
	accessToken := GetAccessToken()
	log.Printf("Uploading %s to %s", dir, uploadUrl)
	return uploadArtifactsRecursive(dir, uploadUrl, "", accessToken)
}

func uploadArtifactsRecursive(dir string, uploadUrl string, prefix string, accessToken string) error {
	files, err := os.ReadDir(dir)
	if err != nil {
		return err
	}

	for _, file := range files {
		fullPath := filepath.Join(dir, file.Name())
		key := filepath.Join(prefix, file.Name())

		if file.IsDir() {
			// Recursively upload subdirectory
			err = uploadArtifactsRecursive(fullPath, uploadUrl, key, accessToken)
			if err != nil {
				return err
			}
		} else {
			// Upload file with preserved path structure
			url := uploadUrl + "/" + key
			log.Printf("Uploading: %s -> %s", fullPath, url)

			err = UploadFile(url, fullPath, accessToken)
			if err != nil {
				return err
			}
		}
	}

	return nil
}

// UploadFile uploads a file to the specified URL with the given field name
func UploadFile(url string, fp string, accessToken string) error {
	file, err := os.Open(fp)
	if err != nil {
		return fmt.Errorf("failed to open file: %w", err)
	}
	defer file.Close()

	payload := &bytes.Buffer{}
	writer := multipart.NewWriter(payload)

	part, err := writer.CreateFormFile("file", filepath.Base(fp))
	if err != nil {
		return fmt.Errorf("failed to create form file: %w", err)
	}

	if _, err := io.Copy(part, file); err != nil {
		return fmt.Errorf("failed to copy file content: %w", err)
	}

	if err := writer.Close(); err != nil {
		return fmt.Errorf("failed to close writer: %w", err)
	}

	req, err := http.NewRequest("PUT", url, payload)
	if err != nil {
		return fmt.Errorf("failed to create request: %w", err)
	}

	req.Header.Set("Authorization", "Bearer "+accessToken)
	req.Header.Set("Content-Type", writer.FormDataContentType())
	req.Header.Set("X-Upload-Source", "build-service") // Mark as internal upload
	req.Header.Set("X-Grant-Type", "client-credentials")

	client := &http.Client{
		Timeout: 30 * time.Second,
	}

	res, err := client.Do(req)
	if err != nil {
		return fmt.Errorf("failed to send request: %w", err)
	}
	defer res.Body.Close()

	body, err := io.ReadAll(res.Body)
	if err != nil {
		return fmt.Errorf("failed to read response body: %w", err)
	}

	if res.StatusCode < 200 || res.StatusCode >= 300 {
		return fmt.Errorf("upload failed with status %d: %s", res.StatusCode, string(body))
	}

	log.Printf("Upload successful: %s - Response: %s", url, string(body))
	return nil
}

func GetAccessToken() string {
	url := os.Getenv("AUTH0_DOMAIN") + "/oauth/token"
	data := struct {
		ClientId     string `json:"client_id"`
		ClientSecret string `json:"client_secret"`
		Audience     string `json:"audience"`
		GrantType    string `json:"grant_type"`
	}{
		ClientId:     os.Getenv("AUTH0_CLIENT_ID"),
		ClientSecret: os.Getenv("AUTH0_SECRET"),
		Audience:     os.Getenv("AUTH0_AUDIENCE"),
		GrantType:    "client_credentials",
	}
	jsonString, err := json.Marshal(data)
	failOnError(err, "Failed to marshal data")
	payload := strings.NewReader(string(jsonString))
	req, _ := http.NewRequest("POST", url, payload)
	req.Header.Add("content-type", "application/json")

	res, err := http.DefaultClient.Do(req)
	failOnError(err, "Failed to send request")

	defer res.Body.Close()
	body, err := io.ReadAll(res.Body)
	failOnError(err, "Failed to read response body")

	var response struct {
		AccessToken string `json:"access_token"`
		TokenType   string `json:"token_type"`
	}
	err = json.Unmarshal(body, &response)
	failOnError(err, "Failed to unmarshal response")
	return response.AccessToken
}

func main() {
	if err := godotenv.Load(); err != nil && !os.IsNotExist(err) {
		failOnError(err, "Failed to load .env file")
	}

	inContainer := isInContainer()
	log.Printf("Running in container: %v", inContainer)

	rabbitMQURL := os.Getenv("RABBITMQ_URL")
	// Connect to RabbitMQ server
	conn, err := amqp.Dial(rabbitMQURL)
	failOnError(err, "Failed to connect to RabbitMQ")
	defer conn.Close()

	// Open a channel
	chConsumer, err := conn.Channel()
	failOnError(err, "Failed to open a channel")
	defer chConsumer.Close()

	// Declare the queue from which jobs are consumed
	qBuildJob, err := chConsumer.QueueDeclare(
		"job_queue", // name
		true,        // durable
		false,       // delete when unused
		false,       // exclusive
		false,       // no-wait
		nil,         // arguments
	)
	failOnError(err, "Failed to declare a queue")

	//
	chPublisher, err := conn.Channel()
	failOnError(err, "Failed to open a channel")
	defer chPublisher.Close()

	err = chPublisher.ExchangeDeclare(
		"app.build.events",
		"fanout",
		true,
		false,
		false,
		false,
		nil,
	)
	failOnError(err, "Failed to declare exchange")

	// Create a channel to limit the number of concurrent jobs
	jobLimit := make(chan struct{}, MaxConcurrentJobs)
	wg := &sync.WaitGroup{}

	// RabbitMQ Consumer setup
	msgs, err := chConsumer.Consume(
		qBuildJob.Name, // queue
		"",             // consumer
		true,           // auto-ack
		false,          // exclusive
		false,          // no-local
		false,          // no-wait
		nil,            // args
	)
	failOnError(err, "Failed to register a consumer")

	fluentdLogger := logutil.NewFluentClient()
	defer fluentdLogger.Close()

	forever := make(chan bool)

	go func() {
		for d := range msgs {
			job := string(d.Body)

			// Limit concurrency by using a buffered channel
			jobLimit <- struct{}{}
			wg.Add(1)

			go func(job string) {
				defer func() { <-jobLimit }() // Release the slot once the job is done
				ProcessJob(job, wg, chPublisher, fluentdLogger)
			}(job)
		}
	}()

	log.Printf(" [*] Waiting for messages. To exit press CTRL+C")
	<-forever

	wg.Wait() // Wait for all goroutines to finish
}
