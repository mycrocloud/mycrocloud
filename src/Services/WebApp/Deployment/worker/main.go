package main

import (
	"bufio"
	"context"
	"encoding/binary"
	"encoding/json"
	"fmt"
	"io"
	"log"
	"mycrocloud/worker/api_client"
	"mycrocloud/worker/logcollector"
	"mycrocloud/worker/uploader"
	"os"
	"os/signal"
	"path/filepath"
	"strings"
	"sync"
	"syscall"
	"time"

	"github.com/docker/docker/api/types/container"
	"github.com/docker/docker/api/types/mount"
	"github.com/docker/docker/client"
	"github.com/joho/godotenv"
	"github.com/streadway/amqp"
)

// Global limits loaded from environment
var limits Limits

// streamContainerLogs reads the Docker multiplexed log stream and feeds each line to the collector.
func streamContainerLogs(ctx context.Context, cli *client.Client, containerID string, collector *logcollector.Collector) {
	reader, err := cli.ContainerLogs(ctx, containerID, container.LogsOptions{
		ShowStdout: true,
		ShowStderr: true,
		Follow:     true,
		Timestamps: false,
	})
	if err != nil {
		log.Printf("Failed to attach to container logs: %v", err)
		return
	}
	defer reader.Close()

	// Docker multiplexed stream: 8-byte header per frame
	// byte 0: stream type (1=stdout, 2=stderr)
	// bytes 4-7: frame size (big-endian uint32)
	hdr := make([]byte, 8)
	for {
		if _, err := io.ReadFull(reader, hdr); err != nil {
			break
		}
		streamType := hdr[0]
		size := binary.BigEndian.Uint32(hdr[4:8])
		if size == 0 {
			continue
		}
		payload := make([]byte, size)
		if _, err := io.ReadFull(reader, payload); err != nil {
			break
		}

		source := "stdout"
		if streamType == 2 {
			source = "stderr"
		}

		// Split payload into lines (a single frame may contain multiple lines)
		scanner := bufio.NewScanner(strings.NewReader(string(payload)))
		for scanner.Scan() {
			line := scanner.Text()
			if line != "" {
				collector.Append(line, source, "app.builder", containerID)
			}
		}
	}
}

// uploadBuildLogs uploads the collected build logs to the API.
func uploadBuildLogs(buildMsg BuildMessage, collector *logcollector.Collector) {
	if buildMsg.LogsUploadPath == "" || collector.Count() == 0 {
		return
	}

	logsData, err := collector.ToJSONL()
	if err != nil {
		log.Printf("Failed to serialize logs: %v", err)
		return
	}

	cfg := api_client.Config{
		Domain:       os.Getenv("AUTH0_DOMAIN"),
		ClientID:     os.Getenv("AUTH0_CLIENT_ID"),
		ClientSecret: os.Getenv("AUTH0_SECRET"),
		Audience:     os.Getenv("AUTH0_AUDIENCE"),
	}
	token, err := api_client.GetAccessToken(cfg)
	if err != nil {
		log.Printf("Failed to get token for log upload: %v", err)
		return
	}

	apiBaseURL := os.Getenv("API_BASE_URL")
	if apiBaseURL == "" {
		log.Printf("API_BASE_URL not set, cannot upload logs")
		return
	}

	logsURL := strings.TrimSuffix(apiBaseURL, "/") + buildMsg.LogsUploadPath
	if err := uploader.UploadLogs(logsURL, logsData, token, "deployment-worker"); err != nil {
		log.Printf("Failed to upload logs: %v", err)
	} else {
		log.Printf("Uploaded %d log entries", collector.Count())
	}
}

// ProcessJob processes a build job and returns an error if it fails
func ProcessJob(ctx context.Context, jsonString string, ch *amqp.Channel) error {
	var buildMsg BuildMessage
	if err := json.Unmarshal([]byte(jsonString), &buildMsg); err != nil {
		return err
	}

	// Create log collector for this build
	collector := logcollector.New(buildMsg.BuildId, ch)

	// Get job-specific limits from plan (capped by system max)
	jobLimits := limits.GetJobLimits(buildMsg.Limits)
	log.Printf("Job limits: memory=%s, cpu=%d%%, timeout=%ds, artifact=%s",
		formatBytes(jobLimits.MemoryBytes),
		jobLimits.CPUQuota/1000,
		jobLimits.BuildDuration,
		formatBytes(jobLimits.MaxArtifactSize))

	log.Printf("Processing... Id: %s, RepoFullName: %s", buildMsg.BuildId, buildMsg.RepoFullName)
	collector.Append("Processing build "+buildMsg.BuildId, "stdout", "app.worker", "")

	cli, err := client.NewClientWithOpts(client.FromEnv, client.WithAPIVersionNegotiation())
	if err != nil {
		return err
	}
	defer cli.Close()

	// Create output directory
	log.Printf("Creating container")
	builderImage := os.Getenv("BUILDER_IMAGE")
	log.Printf("Using builder image: %s", builderImage)

	jobID := buildMsg.BuildId
	baseOut := getOutputBaseDir()
	jobOut := filepath.Join(baseOut, jobID)

	if err := os.MkdirAll(jobOut, 0777); err != nil {
		return err
	}
	// Ensure permissions are actually 0777 regardless of umask
	_ = os.Chmod(jobOut, 0777)

	log.Printf("Starting build job: %s", jobID)
	log.Printf("HOST_OUT_DIR: %s", baseOut)
	log.Printf("Job output dir: %s", jobOut)

	// Check if artifact file already exists
	zipPath := filepath.Join(jobOut, buildMsg.OutDir+".zip")
	if fileInfo, err := os.Stat(zipPath); err == nil && fileInfo.Size() > 0 {
		log.Printf("Artifact already exists at %s (size: %d bytes), skipping build", zipPath, fileInfo.Size())

		// Verify artifact size is within limits
		sizeCheck, err := CheckArtifactSize(zipPath, jobLimits)
		if err == nil && !sizeCheck.ExceedsHard {
			// Upload existing artifact
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
					log.Printf("Failed to get access token for existing artifact: %v", err)
					// Continue with rebuild
				} else {
					apiBaseURL := os.Getenv("API_BASE_URL")
					if apiBaseURL == "" {
						log.Printf("API_BASE_URL not set, cannot upload artifact")
					} else {
						uploadURL := strings.TrimSuffix(apiBaseURL, "/") + buildMsg.ArtifactsUploadPath
						artifactId, err := uploader.UploadArtifacts(uploadURL, jobOut, buildMsg.OutDir, token, "deployment-worker")
						if err != nil {
							log.Printf("Failed to upload existing artifact: %v", err)
							// Continue with rebuild
						} else {
							log.Printf("Successfully uploaded existing artifact: %s", artifactId)
							collector.Append("Finished processing (existing artifact)", "stdout", "app.worker", "")
							uploadBuildLogs(buildMsg, collector)
							publishJobStatusChangedEventMessage(ch, BuildStatusChangedEventMessage{
								BuildId:    buildMsg.BuildId,
								Status:     Done,
								ArtifactId: artifactId,
							})
							return nil
						}
					}
				}
			}
		}
		log.Printf("Existing artifact invalid or upload failed, proceeding with rebuild")
	}

	mounts := []mount.Mount{
		{
			Type:   mount.TypeBind,
			Source: jobOut,
			Target: "/output",
		},
	}

	autoRemove := os.Getenv("BUILDER_AUTO_REMOVE") != "false"

	// Get secure host config with resource limits
	hostConfig := GetSecureHostConfig(jobLimits)
	hostConfig.Mounts = mounts
	hostConfig.AutoRemove = autoRemove

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
		hostConfig,
		nil, nil, "")
	if err != nil {
		return err
	}

	// Start the container
	log.Printf("Starting container")
	if err := cli.ContainerStart(ctx, resp.ID, container.StartOptions{}); err != nil {
		return err
	}

	publishJobStatusChangedEventMessage(ch, BuildStatusChangedEventMessage{
		BuildId:     buildMsg.BuildId,
		Status:      Started,
		ContainerId: resp.ID,
	})

	// Stream container logs in background
	logsDone := make(chan struct{})
	go func() {
		defer close(logsDone)
		streamContainerLogs(ctx, cli, resp.ID, collector)
	}()

	// Wait for container with timeout
	jobTimeout := time.Duration(jobLimits.BuildDuration) * time.Second
	timeoutCtx, cancel := context.WithTimeout(ctx, jobTimeout)
	defer cancel()

	statusCh, errCh := cli.ContainerWait(timeoutCtx, resp.ID, container.WaitConditionNotRunning)

	var containerFailed bool
	select {
	case err := <-errCh:
		if err != nil {
			// Try to stop container if timeout
			if timeoutCtx.Err() == context.DeadlineExceeded {
				log.Printf("Job timeout, stopping container %s", resp.ID)
				collector.Append("Build timed out", "stderr", "app.worker", "")
				stopCtx, stopCancel := context.WithTimeout(context.Background(), 10*time.Second)
				_ = cli.ContainerStop(stopCtx, resp.ID, container.StopOptions{})
				stopCancel()
			}
			// Wait for log streaming to finish
			<-logsDone
			uploadBuildLogs(buildMsg, collector)
			publishJobStatusChangedEventMessage(ch, BuildStatusChangedEventMessage{
				BuildId: buildMsg.BuildId,
				Status:  Failed,
			})
			return err
		}

	case status := <-statusCh:
		log.Printf("Container finished with status %d", status.StatusCode)
		if status.StatusCode != 0 {
			containerFailed = true
		}
	}

	// Wait for log streaming to finish
	<-logsDone

	if containerFailed {
		collector.Append("Build failed (non-zero exit code)", "stderr", "app.worker", "")
		uploadBuildLogs(buildMsg, collector)
		publishJobStatusChangedEventMessage(ch, BuildStatusChangedEventMessage{
			BuildId: buildMsg.BuildId,
			Status:  Failed,
		})
		return nil // Job processed, but build failed
	}

	// Upload artifacts
	log.Printf("Dist dir: %s", jobOut)
	shouldUploadArtifacts := os.Getenv("UPLOAD_ARTIFACTS") != "false"

	if shouldUploadArtifacts {
		// Check output directory for suspicious files
		if err := CheckOutputDirectory(jobOut); err != nil {
			log.Printf("Warning: output directory check failed: %v", err)
		}

		// Check artifact size
		zipPath := filepath.Join(jobOut, buildMsg.OutDir+".zip")
		sizeCheck, err := CheckArtifactSize(zipPath, jobLimits)
		if err != nil {
			log.Printf("Warning: artifact size check failed: %v", err)
		} else if sizeCheck.ExceedsHard {
			log.Printf("Artifact size exceeds hard limit: %s", sizeCheck.Message)
			collector.Append("Artifact size exceeds limit: "+sizeCheck.Message, "stderr", "app.worker", "")
			uploadBuildLogs(buildMsg, collector)
			publishJobStatusChangedEventMessage(ch, BuildStatusChangedEventMessage{
				BuildId: buildMsg.BuildId,
				Status:  Failed,
			})
			return fmt.Errorf("artifact too large: %s", sizeCheck.Message)
		} else if sizeCheck.ExceedsSoft {
			log.Printf("Warning: %s", sizeCheck.Message)
		}

		cfg := api_client.Config{
			Domain:       os.Getenv("AUTH0_DOMAIN"),
			ClientID:     os.Getenv("AUTH0_CLIENT_ID"),
			ClientSecret: os.Getenv("AUTH0_SECRET"),
			Audience:     os.Getenv("AUTH0_AUDIENCE"),
		}

		token, err := api_client.GetAccessToken(cfg)
		if err != nil {
			collector.Append("Failed to get access token: "+err.Error(), "stderr", "app.worker", "")
			uploadBuildLogs(buildMsg, collector)
			publishJobStatusChangedEventMessage(ch, BuildStatusChangedEventMessage{
				BuildId: buildMsg.BuildId,
				Status:  Failed,
			})
			return err
		}

		apiBaseURL := os.Getenv("API_BASE_URL")
		if apiBaseURL == "" {
			log.Printf("API_BASE_URL not set, cannot upload artifact")
			collector.Append("API_BASE_URL not configured", "stderr", "app.worker", "")
			uploadBuildLogs(buildMsg, collector)
			publishJobStatusChangedEventMessage(ch, BuildStatusChangedEventMessage{
				BuildId: buildMsg.BuildId,
				Status:  Failed,
			})
			return fmt.Errorf("API_BASE_URL not configured")
		}

		collector.Append("Uploading artifact...", "stdout", "app.worker", "")
		uploadURL := strings.TrimSuffix(apiBaseURL, "/") + buildMsg.ArtifactsUploadPath
		artifactId, err := uploader.UploadArtifacts(uploadURL, jobOut, buildMsg.OutDir, token, "deployment-worker")
		if err != nil {
			collector.Append("Artifact upload failed: "+err.Error(), "stderr", "app.worker", "")
			uploadBuildLogs(buildMsg, collector)
			publishJobStatusChangedEventMessage(ch, BuildStatusChangedEventMessage{
				BuildId: buildMsg.BuildId,
				Status:  Failed,
			})
			return err
		}

		// Cleanup job output directory after successful upload
		if err := os.RemoveAll(jobOut); err != nil {
			log.Printf("Warning: failed to cleanup job output dir %s: %v", jobOut, err)
		} else {
			log.Printf("Cleaned up job output dir: %s", jobOut)
		}

		collector.Append("Build completed successfully", "stdout", "app.worker", "")
		uploadBuildLogs(buildMsg, collector)

		// Publish completion message with artifactId
		publishJobStatusChangedEventMessage(ch, BuildStatusChangedEventMessage{
			BuildId:    buildMsg.BuildId,
			Status:     Done,
			ArtifactId: artifactId,
		})
	} else {
		collector.Append("Build completed (upload disabled)", "stdout", "app.worker", "")
		uploadBuildLogs(buildMsg, collector)

		// Publish completion message without artifactId if upload is disabled
		publishJobStatusChangedEventMessage(ch, BuildStatusChangedEventMessage{
			BuildId: buildMsg.BuildId,
			Status:  Done,
		})
	}

	log.Printf("Finished processing. Id: %s", buildMsg.BuildId)
	return nil
}

func getOutputBaseDir() string {
	dir := os.Getenv("HOST_OUT_DIR")
	if dir == "" {
		log.Fatal("HOST_OUT_DIR must be set")
	}
	return dir
}

func main() {
	// Load config from .conf first, then .env (with override)
	if err := godotenv.Load(".conf"); err == nil {
		log.Printf("Loaded configuration from .conf")
	}
	if err := godotenv.Overload(".env"); err == nil {
		log.Printf("Loaded and overrode configuration from .env")
	}

	// Load limits from environment
	limits = LoadLimitsFromEnv()
	log.Printf("System max: memory=%s, cpu=%d%%, timeout=%ds, artifact=%s",
		formatBytes(limits.System.MaxMemoryBytes),
		limits.System.MaxCPUPercent,
		limits.System.MaxBuildDuration,
		formatBytes(limits.System.MaxArtifactSize))
	log.Printf("Default job: memory=%s, cpu=%d%%, timeout=%ds, artifact=%s",
		formatBytes(limits.DefaultJob.MemoryBytes),
		limits.DefaultJob.CPUQuota/1000,
		limits.DefaultJob.BuildDuration,
		formatBytes(limits.DefaultJob.MaxArtifactSize))

	inContainer := isInContainer()
	log.Printf("Running in container: %v", inContainer)

	// Setup signal handling for graceful shutdown
	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()

	sigChan := make(chan os.Signal, 1)
	signal.Notify(sigChan, syscall.SIGINT, syscall.SIGTERM)

	rabbitMQURL := os.Getenv("RABBITMQ_URL")
	conn, err := amqp.Dial(rabbitMQURL)
	if err != nil {
		log.Fatalf("Failed to connect to RabbitMQ: %v", err)
	}
	defer conn.Close()

	chConsumer, err := conn.Channel()
	if err != nil {
		log.Fatalf("Failed to open consumer channel: %v", err)
	}
	defer chConsumer.Close()

	qBuildJob, err := chConsumer.QueueDeclare(
		"job_queue",
		true,  // durable
		false, // delete when unused
		false, // exclusive
		false, // no-wait
		nil,
	)
	if err != nil {
		log.Fatalf("Failed to declare queue: %v", err)
	}

	chPublisher, err := conn.Channel()
	if err != nil {
		log.Fatalf("Failed to open publisher channel: %v", err)
	}
	defer chPublisher.Close()

	if err := chPublisher.ExchangeDeclare(
		"app.build.events",
		"fanout",
		true,
		false,
		false,
		false,
		nil,
	); err != nil {
		log.Fatalf("Failed to declare events exchange: %v", err)
	}

	// Declare the log exchange (replaces fluentd → RabbitMQ)
	if err := chPublisher.ExchangeDeclare(
		"app.build.logs",
		"topic",
		false, // non-durable (matches existing setting)
		false,
		false,
		false,
		nil,
	); err != nil {
		log.Fatalf("Failed to declare logs exchange: %v", err)
	}

	// Set prefetch to limit concurrent processing
	if err := chConsumer.Qos(limits.MaxConcurrentJobs, 0, false); err != nil {
		log.Fatalf("Failed to set QoS: %v", err)
	}

	// Manual ack mode
	msgs, err := chConsumer.Consume(
		qBuildJob.Name,
		"",
		false, // auto-ack = false (manual ack)
		false,
		false,
		false,
		nil,
	)
	if err != nil {
		log.Fatalf("Failed to register consumer: %v", err)
	}

	var wg sync.WaitGroup
	jobLimit := make(chan struct{}, limits.MaxConcurrentJobs)

	// Message processing goroutine
	go func() {
		for {
			select {
			case <-ctx.Done():
				log.Printf("Context cancelled, stopping message consumption")
				return
			case d, ok := <-msgs:
				if !ok {
					log.Printf("Message channel closed")
					return
				}

				jobLimit <- struct{}{}
				wg.Add(1)

				go func(delivery amqp.Delivery) {
					defer func() {
						<-jobLimit
						wg.Done()
					}()

					job := string(delivery.Body)
					if err := ProcessJob(ctx, job, chPublisher); err != nil {
						log.Printf("Job failed: %v", err)
						// Nack without requeue to avoid infinite loop
						if nackErr := delivery.Nack(false, false); nackErr != nil {
							log.Printf("Failed to nack message: %v", nackErr)
						}
						return
					}

					// Ack on success
					if err := delivery.Ack(false); err != nil {
						log.Printf("Failed to ack message: %v", err)
					}
				}(d)
			}
		}
	}()

	log.Printf(" [*] Waiting for messages. Press Ctrl+C to exit")

	// Wait for shutdown signal
	sig := <-sigChan
	log.Printf("Received signal %v, initiating graceful shutdown...", sig)

	// Cancel context to stop accepting new jobs
	cancel()

	// Wait for in-flight jobs with timeout
	done := make(chan struct{})
	go func() {
		wg.Wait()
		close(done)
	}()

	shutdownTimeout := 60 * time.Second
	select {
	case <-done:
		log.Printf("All jobs completed, shutting down")
	case <-time.After(shutdownTimeout):
		log.Printf("Shutdown timeout after %v, forcing exit", shutdownTimeout)
	}
}
