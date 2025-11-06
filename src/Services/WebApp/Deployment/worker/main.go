package main

import (
	"bytes"
	"context"
	"encoding/json"
	"fmt"
	"io"
	"log"
	"mime/multipart"
	"net/http"
	"os"
	"path/filepath"
	"strings"
	"sync"
	"time"

	"github.com/docker/docker/api/types/container"
	"github.com/docker/docker/api/types/mount"
	"github.com/docker/docker/client"
	elasticsearch7 "github.com/elastic/go-elasticsearch/v7"
	elasticsearch8 "github.com/elastic/go-elasticsearch/v8"
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

func logJob(es7 *elasticsearch7.Client, es8 *elasticsearch8.Client, level string, msg string, jobID string) {
	doc := struct {
		Timestamp time.Time `json:"@timestamp"`
		Level     string    `json:"level"`
		Message   string    `json:"message"`
		JobID     string    `json:"job_id"`
		Source    string    `json:"source"`
	}{
		Timestamp: time.Now(),
		Level:     level,
		Message:   msg,
		JobID:     jobID,
		Source:    "worker",
	}
	data, err := json.Marshal(doc)
	failOnError(err, "Failed to marshal log data")

	if ES_VERSION == "7" {
		if _, err := es7.Index(LogIndex, bytes.NewReader(data)); err != nil {
			log.Printf("Failed to index document in Elasticsearch 7: %v", err)
		}
	} else if ES_VERSION == "8" {
		if _, err := es8.Index(LogIndex, bytes.NewReader(data)); err != nil {
			log.Printf("Failed to index document in Elasticsearch 8: %v", err)
		}
	} else {
		log.Printf("Invalid Elasticsearch version: %s", ES_VERSION)
	}
}

func getMountConfig(jobID string) mount.Mount {
	base := os.Getenv("HOST_OUT_DIR")
	hostPath := filepath.Join(base, jobID)

	if err := os.MkdirAll(hostPath, 0755); err != nil {
		failOnError(err, "Failed to create output dir")
	}

	log.Printf("Mount output: %s → /output", hostPath)
	return mount.Mount{
		Type:   mount.TypeBind,
		Source: hostPath,
		Target: "/output",
	}
}

func getLogConfig(jobID string) container.LogConfig {
	driver := os.Getenv("LOGGER_DRIVER")
	if driver == "" {
		driver = "json-file"
	}

	cfg := container.LogConfig{Type: driver}

	switch driver {
	case "fluentd":
		addr := os.Getenv("FLUENTD_ADDRESS")
		if addr == "" {
			addr = "localhost:24224"
		}
		cfg.Config = map[string]string{
			"fluentd-address": addr,
			"tag":             fmt.Sprintf("mycrocloud.builder.%s", jobID),
		}
		log.Printf("[builder:%s] Fluentd logging enabled (%s)", jobID, addr)

	case "json-file":
		log.Printf("[builder:%s] Using local json-file logging", jobID)

	case "none":
		log.Printf("[builder:%s] Logging disabled (none)", jobID)

	default:
		log.Printf("[builder:%s] Unknown LOGGER_DRIVER=%s, using default json-file", jobID, driver)
		cfg.Type = "json-file"
	}

	return cfg
}

// ProcessJob simulates job processing asynchronously
func ProcessJob(jsonString string, wg *sync.WaitGroup, ch *amqp.Channel, es7 *elasticsearch7.Client, es8 *elasticsearch8.Client) {
	defer wg.Done()

	var buildMsg BuildMessage
	err := json.Unmarshal([]byte(jsonString), &buildMsg)
	failOnError(err, "Failed to unmarshal JSON")
	log.Printf("Processing... Id: %s, RepoFullName: %s", buildMsg.JobId, buildMsg.RepoFullName)
	logJob(es7, es8, "info", "Processing", buildMsg.JobId)

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
			Labels: map[string]string{"job_id": buildMsg.JobId},
		},
		&container.HostConfig{
			Mounts: []mount.Mount{
				{
					Type:   mount.TypeBind,
					Source: jobOut,
					Target: "/output",
				},
			},
			AutoRemove: true,
			LogConfig:  getLogConfig(buildMsg.JobId),
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
		RecursiveUpload(jobOut)
	}

	// publish completion message
	publishJobStatusChangedEventMessage(ch, JobStatusChangedEventMessage{
		JobId:              buildMsg.JobId,
		Status:             Done,
		ArtifactsKeyPrefix: "output/" + buildMsg.JobId,
	})

	log.Printf("Finished processing. Id: %s", buildMsg.JobId)
	logJob(es7, es8, "info", "Finished processing", buildMsg.JobId)
}

func getOutputBaseDir() string {
	dir := os.Getenv("HOST_OUT_DIR")
	if dir == "" {
		log.Fatal("❌ HOST_OUT_DIR must be set (compose-only mode)")
	}
	return dir
}

func RecursiveUpload(dir string) {
	files, err := os.ReadDir(dir)
	failOnError(err, "Failed to read directory")
	access_token := GetAccessToken()

	for _, file := range files {
		if file.IsDir() {
			RecursiveUpload(dir + "/" + file.Name())
		} else {
			key := dir + "/" + file.Name()
			url := os.Getenv("UPLOAD_URL") + "/" + key
			fileName := file.Name()
			log.Printf("Uploading file: %s", key)
			err = UploadFile(url, key, fileName, access_token)
			failOnError(err, "Failed to upload file")
		}
	}
}

// UploadFile uploads a file to the specified URL with the given field name
func UploadFile(url string, fp string, fieldName string, access_token string) error {
	method := "PUT"

	payload := &bytes.Buffer{}
	writer := multipart.NewWriter(payload)
	file, errFile1 := os.Open(fp)
	failOnError(errFile1, "Failed to open file")

	defer file.Close()
	part1,
		errFile1 := writer.CreateFormFile("file", filepath.Base(fp))
	failOnError(errFile1, "Failed to create form file")
	_, errFile1 = io.Copy(part1, file)
	failOnError(errFile1, "Failed to copy file content")
	err := writer.Close()
	failOnError(err, "Failed to close writer")

	client := &http.Client{}
	req, err := http.NewRequest(method, url, payload)

	failOnError(err, "Failed to create request")
	req.Header.Add("Authorization", "Bearer "+access_token)

	req.Header.Set("Content-Type", writer.FormDataContentType())
	res, err := client.Do(req)
	failOnError(err, "Failed to send request")
	defer res.Body.Close()

	body, err := io.ReadAll(res.Body)
	failOnError(err, "Failed to read response body")
	fmt.Println(string(body))
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
	log.Printf("Connecting to RabbitMQ server at %s", rabbitMQURL)
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

	host := os.Getenv("ES_HOST")
	username := os.Getenv("ES_USERNAME")
	password := os.Getenv("ES_PASSWORD")

	// Configure the Elasticsearch client
	es7, err := elasticsearch7.NewClient(elasticsearch7.Config{
		Addresses: []string{host},
		Username:  username,
		Password:  password,
	})
	failOnError(err, "Failed to create elasticsearch client")
	es8, err := elasticsearch8.NewClient(elasticsearch8.Config{
		Addresses: []string{host},
		Username:  username,
		Password:  password,
	})
	failOnError(err, "Failed to create elasticsearch client")

	switch ES_VERSION {
	case "7":
		_, err := es7.Indices.Create(LogIndex)
		failOnError(err, "Failed to create index")
	case "8":
		es8.Indices.Create(LogIndex)
		failOnError(err, "Failed to create index")
	}

	forever := make(chan bool)

	go func() {
		for d := range msgs {
			job := string(d.Body)

			// Limit concurrency by using a buffered channel
			jobLimit <- struct{}{}
			wg.Add(1)

			go func(job string) {
				defer func() { <-jobLimit }() // Release the slot once the job is done
				ProcessJob(job, wg, chPublisher, es7, es8)
			}(job)
		}
	}()

	log.Printf(" [*] Waiting for messages. To exit press CTRL+C")
	<-forever

	wg.Wait() // Wait for all goroutines to finish
}
