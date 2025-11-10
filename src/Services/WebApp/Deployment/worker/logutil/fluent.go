package logutil

import (
	"log"
	"os"
	"strconv"
	"strings"

	"github.com/fluent/fluent-logger-golang/fluent"
)

func NewFluentClient() *fluent.Fluent {
	addr := os.Getenv("FLUENTD_ADDRESS")

	network := "tcp"
	host := "localhost"
	port := 24224
	socketPath := ""

	if strings.Contains(addr, "://") {
		parts := strings.SplitN(addr, "://", 2)
		network = parts[0]
		address := parts[1]

		switch network {
		case "tcp":
			hp := strings.SplitN(address, ":", 2)
			host = hp[0]
			if len(hp) > 1 {
				if p, err := strconv.Atoi(hp[1]); err == nil {
					port = p
				}
			}
		case "unix":
			socketPath = address
		}
	} else if strings.Contains(addr, ":") {
		// Fallback dạng host:port
		hp := strings.SplitN(addr, ":", 2)
		host = hp[0]
		if len(hp) > 1 {
			if p, err := strconv.Atoi(hp[1]); err == nil {
				port = p
			}
		}
	}

	client, err := fluent.New(fluent.Config{
		FluentNetwork:    network,
		FluentHost:       host,
		FluentPort:       port,
		FluentSocketPath: socketPath,
	})
	if err != nil {
		log.Fatalf("❌ Failed to connect to Fluentd (%s): %v", addr, err)
	}

	log.Printf("✅ Connected to Fluentd (%s) [%s:%d]", network, host, port)
	return client
}
