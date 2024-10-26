package main

import (
	"encoding/json"
	"log"
	"os"
	"os/exec"
	"sync"

	"github.com/streadway/amqp"
)

type BuildMessage struct {
	Id           string `json:"Id"`
	RepoFullName string `json:"RepoFullName"`
	CloneUrl     string `json:"CloneUrl"`
}

func failOnError(err error, msg string) {
	if err != nil {
		log.Fatalf("%s: %s", msg, err)
	}
}

// MaxConcurrentJobs is the limit for concurrent jobs being processed
const MaxConcurrentJobs = 3

// ProcessJob simulates job processing asynchronously
func ProcessJob(jsonString string, wg *sync.WaitGroup, ch *amqp.Channel) {
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

	//print node version
	log.Printf("Printing node version")
	printNodeVersionCmd := exec.Command("node", "--version")
	printNodeVersionCmd.Dir = dir
	err = printNodeVersionCmd.Run()
	failOnError(err, "Failed to print node version")

	// run npm install
	log.Printf("Running npm install")
	installCmd := exec.Command("npm", "install")
	installCmd.Dir = dir
	err = installCmd.Run()
	failOnError(err, "Failed to run npm install")

	// run npm run build
	log.Printf("Running npm run build")
	buildCmd := exec.Command("npm", "run", "build")
	buildCmd.Dir = dir
	err = buildCmd.Run()
	failOnError(err, "Failed to run npm run build")

	// upload the build to S3
	// log.Printf("Uploading build to S3")
	// uploadCmd := exec.Command("aws", "s3", "cp", "--recursive", "build", "s3://my-bucket")
	// uploadCmd.Dir = dir
	// err = uploadCmd.Run()
	// failOnError(err, "Failed to upload build to S3")

	// publish completion message
	status := "done"
	err = ch.Publish(
		"",           // exchange
		"job_status", // routing key
		false,        // mandatory
		false,        // immediate
		amqp.Publishing{
			ContentType: "application/json",
			Body:        []byte(`{"Id": "` + repo.Id + `", "Status": "` + status + `"}`),
		})
	failOnError(err, "Failed to publish a message")

	log.Printf("Finished processing. Id: %s", repo.Id)
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
				ProcessJob(job, wg, ch)
			}(job)
		}
	}()

	log.Printf(" [*] Waiting for messages. To exit press CTRL+C")
	<-forever

	wg.Wait() // Wait for all goroutines to finish
}
