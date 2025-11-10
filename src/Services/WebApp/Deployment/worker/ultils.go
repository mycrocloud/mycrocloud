package main

import (
	"encoding/json"
	"os"
	"strings"

	"github.com/streadway/amqp"
)

func publishJobStatusChangedEventMessage(ch *amqp.Channel, message JobStatusChangedEventMessage) {
	jsonMessage, err := json.Marshal(message)
	failOnError(err, "Failed to marshal JSON")

	err = ch.Publish(
		"app.build.events",
		"",
		false,
		false,
		amqp.Publishing{
			ContentType: "application/json",
			Body:        jsonMessage,
		})
	failOnError(err, "Failed to publish a message")
}

func isInContainer() bool {
	if os.Getenv("GO_RUNNING_IN_CONTAINER") == "true" {
		return true
	}

	if _, err := os.Stat("/.dockerenv"); err == nil {
		return true
	}

	return false
}

func stripProtocol(addr string) string {
	if idx := strings.Index(addr, "://"); idx != -1 {
		return addr[idx+3:]
	}
	return addr
}
