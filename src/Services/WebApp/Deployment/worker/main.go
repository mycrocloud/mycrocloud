package main

import (
	"context"
	"encoding/json"
	"fmt"
	"log"
	"mycrocloud/worker/api_client"
	"mycrocloud/worker/logutil"
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
	"github.com/fluent/fluent-logger-golang/fluent"
	"github.com/joho/godotenv"
	"github.com/streadway/amqp"
)

// Global limits loaded from environment
var limits Limits

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

// ProcessJob processes a build job and returns an error if it fails
func ProcessJob(ctx context.Context, jsonString string, ch *amqp.Channel, l *fluent.Fluent) error {
	var buildMsg BuildMessage
	if err := json.Unmarshal([]byte(jsonString), &buildMsg); err != nil {
		return err
	}

	// Validate input
	if result := ValidateBuildMessage(&buildMsg, limits); !result.IsValid() {
		log.Printf("Validation failed for build %s: %s", buildMsg.BuildId, result.Error())
		return fmt.Errorf("validation failed: %s", result.Error())
	}

	// Get job-specific limits from plan (capped by system max)
	jobLimits := limits.GetJobLimits(buildMsg.Limits)
	log.Printf("Job limits: memory=%s, cpu=%d%%, timeout=%ds, artifact=%s",
		formatBytes(jobLimits.MemoryBytes),
		jobLimits.CPUQuota/1000,
		jobLimits.BuildDuration,
		formatBytes(jobLimits.MaxArtifactSize))

	log.Printf("Processing... Id: %s, RepoFullName: %s", buildMsg.BuildId, buildMsg.RepoFullName)
	logFluentd(l, "Processing", buildMsg.BuildId)

	cli, err := client.NewClientWithOpts(client.FromEnv, client.WithAPIVersionNegotiation())
	if err != nil {
		return err
	}
	defer cli.Close()

	// Create output directory
	log.Printf("Creating container")
	builderImage := os.Getenv("BUILDER_IMAGE")

	jobID := buildMsg.BuildId
	baseOut := getOutputBaseDir()
	jobOut := filepath.Join(baseOut, jobID)

	if err := os.MkdirAll(jobOut, 0755); err != nil {
		return err
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

	// Get secure host config with resource limits
	hostConfig := GetSecureHostConfig(jobLimits)
	hostConfig.Mounts = mounts
	hostConfig.LogConfig = logConf
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
				stopCtx, stopCancel := context.WithTimeout(context.Background(), 10*time.Second)
				_ = cli.ContainerStop(stopCtx, resp.ID, container.StopOptions{})
				stopCancel()
			}
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

	if containerFailed {
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
			publishJobStatusChangedEventMessage(ch, BuildStatusChangedEventMessage{
				BuildId: buildMsg.BuildId,
				Status:  Failed,
			})
			return err
		}

		if err := uploader.UploadArtifacts(strings.TrimSuffix(buildMsg.ArtifactsUploadUrl, "/"), jobOut, buildMsg.OutDir, token, "deployment-worker"); err != nil {
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
	}

	// Publish completion message
	publishJobStatusChangedEventMessage(ch, BuildStatusChangedEventMessage{
		BuildId: buildMsg.BuildId,
		Status:  Done,
	})

	log.Printf("Finished processing. Id: %s", buildMsg.BuildId)
	logFluentd(l, "Finished processing", buildMsg.BuildId)
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
	if err := godotenv.Load(".conf"); err != nil && !os.IsNotExist(err) {
		log.Printf("Warning: failed to load .conf file: %v", err)
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
		log.Fatalf("Failed to declare exchange: %v", err)
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

	fluentdLogger := logutil.NewFluentClient()
	defer fluentdLogger.Close()

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
					if err := ProcessJob(ctx, job, chPublisher, fluentdLogger); err != nil {
						log.Printf("Job failed: %v", err)
						// Nack and requeue on error (could be transient)
						if nackErr := delivery.Nack(false, true); nackErr != nil {
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
