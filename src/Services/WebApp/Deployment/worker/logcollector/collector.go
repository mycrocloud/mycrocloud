package logcollector

import (
	"crypto/rand"
	"encoding/json"
	"fmt"
	"log"
	"sync"
	"time"

	"github.com/streadway/amqp"
)

// LogEntry matches the frontend ILogEntry interface shape.
type LogEntry struct {
	Log         string `json:"log"`
	ContainerID string `json:"container_id,omitempty"`
	Source      string `json:"source"`
	Tag         string `json:"tag"`
	Time        string `json:"time"`
	UUID        string `json:"uuid"`
	BuildID     string `json:"build_id"`
}

// Collector buffers log lines in memory and publishes each line to RabbitMQ for live SSE.
type Collector struct {
	buildID string
	ch      *amqp.Channel
	mu      sync.Mutex
	entries []LogEntry
}

// New creates a new Collector for the given build.
func New(buildID string, ch *amqp.Channel) *Collector {
	return &Collector{
		buildID: buildID,
		ch:      ch,
		entries: make([]LogEntry, 0, 1024),
	}
}

// Append adds a log line, publishes it to RabbitMQ for live SSE, and buffers it.
func (c *Collector) Append(line string, source string, tag string, containerID string) {
	entry := LogEntry{
		Log:         line,
		Source:      source,
		Tag:         tag,
		Time:        time.Now().UTC().Format(time.RFC3339Nano),
		UUID:        generateUUID(),
		BuildID:     c.buildID,
		ContainerID: containerID,
	}

	c.mu.Lock()
	c.entries = append(c.entries, entry)
	c.mu.Unlock()

	// Publish to RabbitMQ for live SSE (best-effort)
	data, err := json.Marshal(entry)
	if err != nil {
		return
	}

	routingKey := fmt.Sprintf("app.build.logs.%s", c.buildID)
	if err := c.ch.Publish(
		"app.build.logs",
		routingKey,
		false,
		false,
		amqp.Publishing{
			ContentType: "application/json",
			Body:        data,
		},
	); err != nil {
		log.Printf("Failed to publish log to RabbitMQ: %v", err)
	}
}

// ToJSONL serializes all buffered entries as newline-delimited JSON.
func (c *Collector) ToJSONL() ([]byte, error) {
	c.mu.Lock()
	defer c.mu.Unlock()

	var buf []byte
	for _, entry := range c.entries {
		line, err := json.Marshal(entry)
		if err != nil {
			return nil, err
		}
		buf = append(buf, line...)
		buf = append(buf, '\n')
	}
	return buf, nil
}

// Count returns the number of buffered log entries.
func (c *Collector) Count() int {
	c.mu.Lock()
	defer c.mu.Unlock()
	return len(c.entries)
}

func generateUUID() string {
	b := make([]byte, 16)
	_, _ = rand.Read(b)
	// Set version 4 and variant bits
	b[6] = (b[6] & 0x0f) | 0x40
	b[8] = (b[8] & 0x3f) | 0x80
	return fmt.Sprintf("%08x-%04x-%04x-%04x-%012x",
		b[0:4], b[4:6], b[6:8], b[8:10], b[10:16])
}
