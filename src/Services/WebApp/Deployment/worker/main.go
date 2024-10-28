package main

import (
	"bytes"
	"encoding/json"
	"fmt"
	"io"
	"log"
	"mime/multipart"
	"net/http"
	"os"
	"os/exec"
	"path/filepath"
	"strings"
	"sync"

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

// ProcessJob simulates job processing asynchronously
func ProcessJob(jsonString string, wg *sync.WaitGroup, ch *amqp.Channel, q amqp.Queue) {
	defer wg.Done()

	var repo BuildMessage
	err := json.Unmarshal([]byte(jsonString), &repo)
	failOnError(err, "Failed to unmarshal JSON")
	log.Printf("Processing... Id: %s, RepoFullName: %s", repo.Id, repo.RepoFullName)

	// clone the repo
	log.Printf("Cloning repo %s", repo.RepoFullName)
	// create a directory with name as repo.Id
	dir := repo.Id
	err = os.RemoveAll(dir)
	failOnError(err, "Failed to remove directory")
	err = os.Mkdir(dir, 0755)
	failOnError(err, "Failed to create directory")

	// clone the repo to the directory
	cloneCmd := exec.Command("git", "clone", repo.CloneUrl, dir)
	err = cloneCmd.Run()
	failOnError(err, "Failed to clone repository")

	// run npm install
	log.Printf("Running npm install")
	installCmd := exec.Command("npm", "install")
	installCmd.Dir = dir + "/" + repo.Directory
	err = installCmd.Run()
	failOnError(err, "Failed to run npm install")

	// run npm run build
	log.Printf("Running npm run build")
	buildCmdStr := "npm run build"
	buildCmd := exec.Command("sh", "-c", buildCmdStr)
	buildCmd.Dir = dir + "/" + repo.Directory
	err = buildCmd.Run()
	failOnError(err, "Failed to run npm run build")

	distDir := dir + "/" + repo.OutDir
	RecursiveUpload(distDir)

	// publish completion message
	statusMessage := struct {
		Id     string `json:"Id"`
		Status string `json:"Status"`
		Prefix string `json:"Prefix"`
	}{
		Id:     repo.Id,
		Status: "done",
		Prefix: distDir,
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

	log.Printf("Finished processing. Id: %s", repo.Id)
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
			log.Printf("Uploading %s", key)
			url := os.Getenv("UPLOAD_URL") + "/" + key
			fieldName := file.Name()
			err = UploadFile(url, key, fieldName, access_token)
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

	forever := make(chan bool)

	go func() {
		for d := range msgs {
			job := string(d.Body)

			// Limit concurrency by using a buffered channel
			jobLimit <- struct{}{}
			wg.Add(1)

			go func(job string) {
				defer func() { <-jobLimit }() // Release the slot once the job is done
				ProcessJob(job, wg, ch, q2)
			}(job)
		}
	}()

	log.Printf(" [*] Waiting for messages. To exit press CTRL+C")
	<-forever

	wg.Wait() // Wait for all goroutines to finish
}
