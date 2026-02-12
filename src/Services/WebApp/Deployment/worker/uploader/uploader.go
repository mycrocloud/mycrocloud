package uploader

import (
	"bytes"
	"crypto/sha256"
	"encoding/hex"
	"encoding/json"
	"fmt"
	"io"
	"log"
	"mime/multipart"
	"net/http"
	"os"
	"path/filepath"
	"time"
)

var client = &http.Client{
	Timeout: 60 * time.Second,
}

// UploadFile uploads a single file using PUT with form field "file".
// Also computes SHA256 hash and sends it as "contentHash" form field.
// Returns the artifactId from the API response.
func UploadFile(url, filePath, accessToken, userAgent string) (string, error) {
	log.Printf("UploadFile: %s -> %s", filePath, url)

	// Read entire file to compute hash
	fileData, err := os.ReadFile(filePath)
	if err != nil {
		return "", fmt.Errorf("read file: %w", err)
	}

	// Compute SHA256 hash
	hash := sha256.Sum256(fileData)
	contentHash := hex.EncodeToString(hash[:])
	log.Printf("Computed SHA256 hash: %s", contentHash)

	body := &bytes.Buffer{}
	writer := multipart.NewWriter(body)

	// Add file field
	part, err := writer.CreateFormFile("file", filepath.Base(filePath))
	if err != nil {
		return "", fmt.Errorf("create form file: %w", err)
	}

	if _, err := part.Write(fileData); err != nil {
		return "", fmt.Errorf("write file data: %w", err)
	}

	// Add contentHash field
	if err := writer.WriteField("contentHash", contentHash); err != nil {
		return "", fmt.Errorf("write contentHash field: %w", err)
	}

	if err := writer.Close(); err != nil {
		return "", fmt.Errorf("close writer: %w", err)
	}

	req, err := http.NewRequest("PUT", url, body)
	if err != nil {
		return "", fmt.Errorf("create request: %w", err)
	}

	req.Header.Set("Authorization", "Bearer "+accessToken)
	req.Header.Set("Content-Type", writer.FormDataContentType())
	if userAgent != "" {
		req.Header.Set("User-Agent", userAgent)
	} else {
		req.Header.Set("User-Agent", "Uploader/1.0")
	}

	res, err := client.Do(req)
	if err != nil {
		return "", fmt.Errorf("send request: %w", err)
	}
	defer res.Body.Close()

	respBytes, _ := io.ReadAll(res.Body)
	if res.StatusCode < 200 || res.StatusCode >= 300 {
		return "", fmt.Errorf("upload failed (%d): %s", res.StatusCode, string(respBytes))
	}

	// Parse response to get artifactId
	var result struct {
		ArtifactId string `json:"artifactId"`
	}
	if err := json.Unmarshal(respBytes, &result); err != nil {
		log.Printf("Warning: failed to parse upload response: %v", err)
		return "", nil // Return empty artifactId but no error
	}

	log.Printf("Upload successful, artifactId: %s", result.ArtifactId)
	return result.ArtifactId, nil
}

// UploadLogs uploads a JSONL log file to the API.
func UploadLogs(url string, logsData []byte, accessToken, userAgent string) error {
	log.Printf("UploadLogs: %d bytes -> %s", len(logsData), url)

	hash := sha256.Sum256(logsData)
	contentHash := hex.EncodeToString(hash[:])

	body := &bytes.Buffer{}
	writer := multipart.NewWriter(body)

	part, err := writer.CreateFormFile("file", "build.log.jsonl")
	if err != nil {
		return fmt.Errorf("create form file: %w", err)
	}
	if _, err := part.Write(logsData); err != nil {
		return fmt.Errorf("write log data: %w", err)
	}
	if err := writer.WriteField("contentHash", contentHash); err != nil {
		return fmt.Errorf("write contentHash: %w", err)
	}
	if err := writer.Close(); err != nil {
		return fmt.Errorf("close writer: %w", err)
	}

	req, err := http.NewRequest("PUT", url, body)
	if err != nil {
		return fmt.Errorf("create request: %w", err)
	}

	req.Header.Set("Authorization", "Bearer "+accessToken)
	req.Header.Set("Content-Type", writer.FormDataContentType())
	if userAgent != "" {
		req.Header.Set("User-Agent", userAgent)
	}

	res, err := client.Do(req)
	if err != nil {
		return fmt.Errorf("send request: %w", err)
	}
	defer res.Body.Close()

	if res.StatusCode < 200 || res.StatusCode >= 300 {
		respBytes, _ := io.ReadAll(res.Body)
		return fmt.Errorf("upload failed (%d): %s", res.StatusCode, string(respBytes))
	}

	log.Printf("Log upload successful")
	return nil
}

// UploadArtifacts uploads the zipped artifact file from rootDir to baseURL.
// The zip file is named {outDir}.zip (e.g., dist.zip, build.zip).
// Returns the artifactId from the API response.
func UploadArtifacts(baseURL, rootDir, outDir, accessToken, userAgent string) (string, error) {
	zipName := outDir + ".zip"
	zipPath := filepath.Join(rootDir, zipName)

	if _, err := os.Stat(zipPath); os.IsNotExist(err) {
		return "", fmt.Errorf("%s not found in %s", zipName, rootDir)
	}

	return UploadFile(baseURL, zipPath, accessToken, userAgent)
}
