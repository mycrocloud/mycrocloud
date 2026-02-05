package main

import (
	"context"
	"encoding/json"
	"log"
	"mycrocloud/worker/api_client"
	"mycrocloud/worker/logutil"
	"mycrocloud/worker/uploader"
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
	log.Printf("Processing... Id: %s, RepoFullName: %s", buildMsg.BuildId, buildMsg.RepoFullName)
	logFluentd(l, "Processing", buildMsg.BuildId)

	ctx := context.Background()
	cli, err := client.NewClientWithOpts(client.FromEnv, client.WithAPIVersionNegotiation())
	failOnError(err, "Failed to create docker client")

	// Create a container
	log.Printf("Creating container")
	builderImage := os.Getenv("BUILDER_IMAGE")

	jobID := buildMsg.BuildId
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
			log.Printf("[builder:%s] Configured fluentd socket mount %s", buildMsg.BuildId, socketPath)
		}
	}

	autoRemove := os.Getenv("BUILDER_AUTO_REMOVE") != "false"

	// Prepare environment variables for the container
	envVars := []string{
		"REPO_URL=" + buildMsg.CloneUrl,
		"WORK_DIR=" + buildMsg.Directory,
		"OUT_DIR=" + buildMsg.OutDir,
		"INSTALL_CMD=" + buildMsg.InstallCommand,
		"BUILD_CMD=" + buildMsg.BuildCommand,
	}

	if buildMsg.NodeVersion != "" {
		envVars = append(envVars, "NODE_VERSION="+buildMsg.NodeVersion)
	}

	// Serialize EnvVars to JSON for the builder to parse
	if len(buildMsg.EnvVars) > 0 {
		envVarsJSON, err := json.Marshal(buildMsg.EnvVars)
		if err != nil {
			log.Printf("Failed to marshal env vars: %v", err)
		} else {
			envVars = append(envVars, "ENV_VARS="+string(envVarsJSON))
		}
	}

	resp, err := cli.ContainerCreate(ctx,
		&container.Config{
			Image:  builderImage,
			Tty:    false,
			Env:    envVars,
			Labels: map[string]string{"build_id": buildMsg.BuildId},
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

	publishJobStatusChangedEventMessage(ch, BuildStatusChangedEventMessage{
		BuildId:     buildMsg.BuildId,
		Status:      Started,
		ContainerId: resp.ID,
	})

	// Wait for the container to finish
	statusCh, errCh := cli.ContainerWait(ctx, resp.ID, container.WaitConditionNotRunning)
	select {
	case err := <-errCh:
		failOnError(err, "Failed to wait for container")
		publishJobStatusChangedEventMessage(ch, BuildStatusChangedEventMessage{
			BuildId: buildMsg.BuildId,
			Status:  Failed,
		})

	case status := <-statusCh:
		log.Printf("Container finished with status %d", status.StatusCode)
	}

	//distDir := path.Join("/output", repo.Id)
	log.Printf("Dist dir: %s", jobOut)
	shouldUploadArtifacts := os.Getenv("UPLOAD_ARTIFACTS") != "false"

	if shouldUploadArtifacts {
		cfg := api_client.Config{
			Domain:       os.Getenv("AUTH0_DOMAIN"),
			ClientID:     os.Getenv("AUTH0_CLIENT_ID"),
			ClientSecret: os.Getenv("AUTH0_SECRET"),
			Audience:     os.Getenv("AUTH0_AUDIENCE"),
		}

		token, err := api_client.GetAccessToken(cfg)
		if err != nil {
			log.Fatalf("Failed to get token: %v", err)
		}

		if err := uploader.UploadArtifacts(strings.TrimSuffix(buildMsg.ArtifactsUploadUrl, "/"), jobOut, token, "deployment-worker"); err != nil {
			log.Fatalf("Upload failed: %v", err)
		}
	}

	// publish completion message
	publishJobStatusChangedEventMessage(ch, BuildStatusChangedEventMessage{
		BuildId: buildMsg.BuildId,
		Status:  Done,
	})

	log.Printf("Finished processing. Id: %s", buildMsg.BuildId)
	logFluentd(l, "Finished processing", buildMsg.BuildId)
}

func getOutputBaseDir() string {
	dir := os.Getenv("HOST_OUT_DIR")
	if dir == "" {
		log.Fatal("❌ HOST_OUT_DIR must be set (compose-only mode)")
	}
	return dir
}

func main() {
	if err := godotenv.Load(".conf"); err != nil && !os.IsNotExist(err) {
		failOnError(err, "Failed to load .conf file")
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
