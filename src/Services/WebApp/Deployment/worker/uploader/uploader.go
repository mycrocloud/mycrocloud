package uploader

import (
	"bytes"
	"fmt"
	"io"
	"log"
	"mime/multipart"
	"net/http"
	"os"
	"path/filepath"
	"strings"
	"time"
)

// UploadFile uploads a single file to: PUT {fullURL} with form field "file"
func UploadFile(url string, filePath string, accessToken string) error {
	log.Printf("UploadFile. %s -> %s", filePath, url)
	f, err := os.Open(filePath)
	if err != nil {
		return fmt.Errorf("open: %w", err)
	}
	defer f.Close()

	body := &bytes.Buffer{}
	writer := multipart.NewWriter(body)

	part, err := writer.CreateFormFile("file", filepath.Base(filePath))
	if err != nil {
		return fmt.Errorf("formfile: %w", err)
	}

	if _, err := io.Copy(part, f); err != nil {
		return fmt.Errorf("copy: %w", err)
	}

	if err := writer.Close(); err != nil {
		return fmt.Errorf("writer: %w", err)
	}

	req, err := http.NewRequest("PUT", url, body)
	if err != nil {
		return fmt.Errorf("request: %w", err)
	}

	req.Header.Set("Authorization", "Bearer "+accessToken)
	req.Header.Set("Content-Type", writer.FormDataContentType())

	client := &http.Client{Timeout: 60 * time.Second}
	res, err := client.Do(req)
	if err != nil {
		return fmt.Errorf("send: %w", err)
	}
	defer res.Body.Close()

	respBytes, _ := io.ReadAll(res.Body)

	if res.StatusCode < 200 || res.StatusCode >= 300 {
		return fmt.Errorf("upload failed (%d): %s", res.StatusCode, string(respBytes))
	}

	return nil
}

// UploadArtifacts uploads a directory recursively.
// baseURL example:
//
//	https://api/.../apps/{id}/builds/{buildId}/artifacts
func UploadArtifacts(baseURL string, rootDir string, accessToken string) error {
	return filepath.Walk(rootDir, func(path string, info os.FileInfo, walkErr error) error {
		if walkErr != nil {
			return walkErr
		}

		if info.IsDir() {
			return nil
		}

		// Compute relative path inside rootDir
		rel, err := filepath.Rel(rootDir, path)
		if err != nil {
			return err
		}

		// Always use "/" for URLs
		rel = filepath.ToSlash(rel)

		uploadURL := strings.TrimRight(baseURL, "/") + "/" + rel

		return UploadFile(uploadURL, path, accessToken)
	})
}
