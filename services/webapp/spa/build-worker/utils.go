package main

import (
	"bytes"
	"encoding/json"
	"fmt"
	"log"
	"mycrocloud/worker/api_client"
	"net/http"
	"strings"
	"time"
)

var httpClient = &http.Client{
	Timeout: 30 * time.Second,
}

// publishBuildStatus sends a build status update to the API via HTTP POST.
// This replaces the previous RabbitMQ-based status publishing.
func publishBuildStatus(buildMsg BuildMessage, message BuildStatusChangedEventMessage, cfg Config) {
	apiClientCfg := api_client.Config{
		Domain:       cfg.Auth0.Domain,
		ClientID:     cfg.Auth0.ClientID,
		ClientSecret: cfg.Auth0.ClientSecret,
		Audience:     cfg.Auth0.Audience,
	}

	token, err := api_client.GetAccessToken(apiClientCfg)
	if err != nil {
		log.Printf("Failed to get token for status update: %v", err)
		return
	}

	// Extract appId from the artifacts upload path (e.g., "/apps/123/spa/builds/{buildId}/artifacts")
	appId := extractAppIdFromPath(buildMsg.ArtifactsUploadPath)
	if appId == "" {
		log.Printf("Failed to extract appId from path: %s", buildMsg.ArtifactsUploadPath)
		return
	}

	statusURL := fmt.Sprintf("%s/apps/%s/spa/builds/%s/status",
		strings.TrimSuffix(cfg.API.BaseURL, "/"), appId, buildMsg.BuildId)

	jsonBody, err := json.Marshal(message)
	if err != nil {
		log.Printf("Failed to marshal status message: %v", err)
		return
	}

	req, err := http.NewRequest("POST", statusURL, bytes.NewReader(jsonBody))
	if err != nil {
		log.Printf("Failed to create status request: %v", err)
		return
	}
	req.Header.Set("Authorization", "Bearer "+token)
	req.Header.Set("Content-Type", "application/json")
	req.Header.Set("User-Agent", "spa-build-worker")

	resp, err := httpClient.Do(req)
	if err != nil {
		log.Printf("Failed to send status update: %v", err)
		return
	}
	defer resp.Body.Close()

	if resp.StatusCode < 200 || resp.StatusCode >= 300 {
		log.Printf("Status update failed (%d) for build %s", resp.StatusCode, message.BuildId)
	} else {
		log.Printf("Published status %d for build %s", message.Status, message.BuildId)
	}
}

// extractAppIdFromPath extracts the app ID from a path like "/apps/123/spa/builds/..."
func extractAppIdFromPath(path string) string {
	parts := strings.Split(strings.TrimPrefix(path, "/"), "/")
	if len(parts) >= 2 && parts[0] == "apps" {
		return parts[1]
	}
	return ""
}

func stripProtocol(addr string) string {
	if idx := strings.Index(addr, "://"); idx != -1 {
		return addr[idx+3:]
	}
	return addr
}
