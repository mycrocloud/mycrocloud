package main

import (
	"encoding/json"
	"log"
	"os"
	"strings"

	"github.com/streadway/amqp"
)

func publishJobStatusChangedEventMessage(ch *amqp.Channel, message BuildStatusChangedEventMessage) {
	jsonMessage, err := json.Marshal(message)
	if err != nil {
		log.Printf("Failed to marshal status message: %v", err)
		return
	}

	err = ch.Publish(
		"app.build.events",
		"",
		false,
		false,
		amqp.Publishing{
			ContentType: "application/json",
			Body:        jsonMessage,
		})
	if err != nil {
		log.Printf("Failed to publish status message: %v", err)
	}
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
