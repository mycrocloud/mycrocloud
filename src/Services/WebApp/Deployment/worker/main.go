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
	"path"
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
	}
	data, err := json.Marshal(doc)
	failOnError(err, "Failed to marshal log data")
	if _, err := es7.Index(LogIndex, bytes.NewReader(data)); err != nil {
		log.Printf("Failed to index document in Elasticsearch 7: %v", err)
	}
	if _, err := es8.Index(LogIndex, bytes.NewReader(data)); err != nil {
		log.Printf("Failed to index document in Elasticsearch 8: %v", err)
	}
}

// ProcessJob simulates job processing asynchronously
func ProcessJob(jsonString string, wg *sync.WaitGroup, ch *amqp.Channel, q amqp.Queue, es7 *elasticsearch7.Client, es8 *elasticsearch8.Client) {
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
	hostOutDir := os.Getenv("HOST_OUT_DIR")
	distDir := path.Join("/output", buildMsg.JobId)

	log.Printf("Creating output directory: %s", distDir)
	if err := os.MkdirAll(distDir, 0755); err != nil {
		log.Fatalf("Failed to create output directory: %v", err)
	}

	resp, err := cli.ContainerCreate(ctx, &container.Config{
		Image: builderImage,
		Tty:   false,
		Env: []string{"REPO_URL=" + buildMsg.CloneUrl,
			"WORK_DIR=" + buildMsg.Directory,
			"OUT_DIR=" + buildMsg.OutDir,
			"INSTALL_CMD=" + buildMsg.InstallCommand,
			"BUILD_CMD=" + buildMsg.BuildCommand,
		},
		Labels: map[string]string{"job_id": buildMsg.JobId},
	}, &container.HostConfig{
		Mounts: []mount.Mount{
			{
				Type:   mount.TypeBind,
				Source: hostOutDir + "/" + buildMsg.JobId,
				Target: "/output",
			},
		},
		AutoRemove: true,
		LogConfig: container.LogConfig{
			Type: "fluentd",
			Config: map[string]string{
				"fluentd-address": "localhost:24224",
				"tag":             "mycrocloud.builder",
			},
		},
	}, nil, nil, "")
	failOnError(err, "Failed to create container")

	// Start the container
	log.Printf("Starting container")
	err = cli.ContainerStart(ctx, resp.ID, container.StartOptions{})
	failOnError(err, "Failed to start container")

	publishJobStatusChangedEventMessage(ch, q, JobStatusChangedEventMessage{
		JobId:       buildMsg.JobId,
		Status:      Started,
		ContainerId: resp.ID,
	})

	// Wait for the container to finish
	statusCh, errCh := cli.ContainerWait(ctx, resp.ID, container.WaitConditionNotRunning)
	select {
	case err := <-errCh:
		failOnError(err, "Failed to wait for container")
		publishJobStatusChangedEventMessage(ch, q, JobStatusChangedEventMessage{
			JobId:  buildMsg.JobId,
			Status: Failed,
		})

	case status := <-statusCh:
		log.Printf("Container finished with status %d", status.StatusCode)
	}

	//distDir := path.Join("/output", repo.Id)
	log.Printf("Dist dir: %s", distDir)
	shouldUploadArtifacts := os.Getenv("UPLOAD_ARTIFACTS") != "false"

	if shouldUploadArtifacts {
		RecursiveUpload(distDir)
	}

	// publish completion message
	publishJobStatusChangedEventMessage(ch, q, JobStatusChangedEventMessage{
		JobId:              buildMsg.JobId,
		Status:             Done,
		ArtifactsKeyPrefix: "output/" + buildMsg.JobId,
	})

	log.Printf("Finished processing. Id: %s", buildMsg.JobId)
	logJob(es7, es8, "info", "Finished processing", buildMsg.JobId)
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
	err := godotenv.Load()
	failOnError(err, "Failed to load .env file")
	rabbitMQURL := os.Getenv("RABBITMQ_URL")
	// Connect to RabbitMQ server
	log.Printf("Connecting to RabbitMQ server at %s", rabbitMQURL)
	conn, err := amqp.Dial(rabbitMQURL)
	failOnError(err, "Failed to connect to RabbitMQ")
	defer conn.Close()

	// Open a channel
	ch, err := conn.Channel()
	failOnError(err, "Failed to open a channel")
	defer ch.Close()

	// Declare the queue from which jobs are consumed
	q, err := ch.QueueDeclare(
		"job_queue", // name
		true,        // durable
		false,       // delete when unused
		false,       // exclusive
		false,       // no-wait
		nil,         // arguments
	)
	failOnError(err, "Failed to declare a queue")

	q2, err := ch.QueueDeclare(
		"job_status", // name
		true,         // durable
		false,        // delete when unused
		false,        // exclusive
		false,        // no-wait
		nil,          // arguments
	)
	failOnError(err, "Failed to declare a queue")

	// Create a channel to limit the number of concurrent jobs
	jobLimit := make(chan struct{}, MaxConcurrentJobs)
	wg := &sync.WaitGroup{}

	// RabbitMQ Consumer setup
	msgs, err := ch.Consume(
		q.Name, // queue
		"",     // consumer
		true,   // auto-ack
		false,  // exclusive
		false,  // no-local
		false,  // no-wait
		nil,    // args
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
	es8, err := elasticsearch8.NewClient(elasticsearch8.Config{
		Addresses: []string{host},
		Username:  username,
		Password:  password,
	})
	failOnError(err, "Failed to create elasticsearch client")

	es7.Indices.Create(LogIndex)
	es8.Indices.Create(LogIndex)

	forever := make(chan bool)

	go func() {
		for d := range msgs {
			job := string(d.Body)

			// Limit concurrency by using a buffered channel
			jobLimit <- struct{}{}
			wg.Add(1)

			go func(job string) {
				defer func() { <-jobLimit }() // Release the slot once the job is done
				ProcessJob(job, wg, ch, q2, es7, es8)
			}(job)
		}
	}()

	log.Printf(" [*] Waiting for messages. To exit press CTRL+C")
	<-forever

	wg.Wait() // Wait for all goroutines to finish
}
