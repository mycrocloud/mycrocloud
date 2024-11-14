package main

import (
	"encoding/json"

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
