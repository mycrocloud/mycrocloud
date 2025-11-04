package main

import (
	"encoding/json"
	"os"

	"github.com/streadway/amqp"
)

func publishJobStatusChangedEventMessage(ch *amqp.Channel, q amqp.Queue, message JobStatusChangedEventMessage) {
	jsonMessage, err := json.Marshal(message)
	failOnError(err, "Failed to marshal JSON")

	err = ch.Publish(
		"",
		q.Name,
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
