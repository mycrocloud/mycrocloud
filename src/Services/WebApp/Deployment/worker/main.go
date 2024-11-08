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
	"github.com/elastic/go-elasticsearch/v8"
	"github.com/streadway/amqp"
)

type BuildMessage struct {
	Id           string `json:"Id"`
	RepoFullName string `json:"RepoFullName"`
	CloneUrl     string `json:"CloneUrl"`
	Directory    string `json:"Directory"`
	OutDir       string `json:"OutDir"`
}

func failOnError(err error, msg string) {
	if err != nil {
		log.Fatalf("%s: %s", msg, err)
	}
}

// MaxConcurrentJobs is the limit for concurrent jobs being processed
const MaxConcurrentJobs = 3

var LogIndex = os.Getenv("ES_BUILD_LOGS_INDEX")

func logJob(esClient *elasticsearch.Client, level string, msg string, jobID string) {
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
	data, _ := json.Marshal(doc)
	esClient.Index(LogIndex, bytes.NewReader(data))
}

// ProcessJob simulates job processing asynchronously
func ProcessJob(jsonString string, wg *sync.WaitGroup, ch *amqp.Channel, q amqp.Queue, esClient *elasticsearch.Client) {
	defer wg.Done()

	var buildMsg BuildMessage
	err := json.Unmarshal([]byte(jsonString), &buildMsg)
	failOnError(err, "Failed to unmarshal JSON")

	// Publish log entry
	failOnError(err, "Failed to marshal log entry")
	log.Printf("Processing... Id: %s, RepoFullName: %s", buildMsg.Id, buildMsg.RepoFullName)
	logJob(esClient, "INFO", "Processing job", buildMsg.Id)

	ctx := context.Background()
	cli, err := client.NewClientWithOpts(client.FromEnv, client.WithAPIVersionNegotiation())
	failOnError(err, "Failed to create docker client")

	// Create a container
	log.Printf("Creating container")
	builderImage := os.Getenv("BUILDER_IMAGE")
	hostOutDir := os.Getenv("HOST_OUT_DIR")
	distDir := path.Join("/output", buildMsg.Id)

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
		},
		Labels: map[string]string{"job_id": buildMsg.Id},
	}, &container.HostConfig{
		Mounts: []mount.Mount{
			{
				Type:   mount.TypeBind,
				Source: hostOutDir + "/" + buildMsg.Id,
				Target: "/output",
			},
		},
		AutoRemove: true,
	}, nil, nil, "")
	failOnError(err, "Failed to create container")

	// Start the container
	log.Printf("Starting container")
	err = cli.ContainerStart(ctx, resp.ID, container.StartOptions{})
	failOnError(err, "Failed to start container")

	// Wait for the container to finish
	statusCh, errCh := cli.ContainerWait(ctx, resp.ID, container.WaitConditionNotRunning)
	select {
	case err := <-errCh:
		failOnError(err, "Failed to wait for container")
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
	statusMessage := struct {
		Id     string `json:"Id"`
		Status string `json:"Status"`
		Prefix string `json:"Prefix"`
	}{
		Id:     buildMsg.Id,
		Status: "done",
		Prefix: "output/" + buildMsg.Id,
	}

	body, err := json.Marshal(statusMessage)
	failOnError(err, "Failed to marshal status message")
	err = ch.Publish(
		"",     // exchange
		q.Name, // routing key
		false,  // mandatory
		false,  // immediate
		amqp.Publishing{
			ContentType: "application/json",
			Body:        body,
		})
	failOnError(err, "Failed to publish a message")

	log.Printf("Finished processing. Id: %s", buildMsg.Id)
	logJob(esClient, "INFO", "Finished processing.", buildMsg.Id)
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
	cfg := elasticsearch.Config{
		Addresses: []string{host},
		Username:  username,
		Password:  password,
	}

	esClient, err := elasticsearch.NewClient(cfg)
	failOnError(err, "Failed to create elasticsearch client")

	esClient.Indices.Create(LogIndex)

	forever := make(chan bool)

	go func() {
		for d := range msgs {
			job := string(d.Body)

			// Limit concurrency by using a buffered channel
			jobLimit <- struct{}{}
			wg.Add(1)

			go func(job string) {
				defer func() { <-jobLimit }() // Release the slot once the job is done
				ProcessJob(job, wg, ch, q2, esClient)
			}(job)
		}
	}()

	log.Printf(" [*] Waiting for messages. To exit press CTRL+C")
	<-forever

	wg.Wait() // Wait for all goroutines to finish
}
