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
	"time"
)

var client = &http.Client{
	Timeout: 60 * time.Second,
}

// UploadFile uploads a single file using PUT with form field "file".
func UploadFile(url, filePath, accessToken, userAgent string) error {
	log.Printf("UploadFile: %s -> %s", filePath, url)

	file, err := os.Open(filePath)
	if err != nil {
		return fmt.Errorf("open file: %w", err)
	}
	defer file.Close()

	body := &bytes.Buffer{}
	writer := multipart.NewWriter(body)

	part, err := writer.CreateFormFile("file", filepath.Base(filePath))
	if err != nil {
		return fmt.Errorf("create form file: %w", err)
	}

	if _, err := io.Copy(part, file); err != nil {
		return fmt.Errorf("copy file: %w", err)
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
	} else {
		req.Header.Set("User-Agent", "Uploader/1.0")
	}

	res, err := client.Do(req)
	if err != nil {
		return fmt.Errorf("send request: %w", err)
	}
	defer res.Body.Close()

	respBytes, _ := io.ReadAll(res.Body)
	if res.StatusCode < 200 || res.StatusCode >= 300 {
		return fmt.Errorf("upload failed (%d): %s", res.StatusCode, string(respBytes))
	}

	return nil
}

// UploadArtifacts uploads the zipped artifact file from rootDir to baseURL.
// The zip file is named {outDir}.zip (e.g., dist.zip, build.zip).
func UploadArtifacts(baseURL, rootDir, outDir, accessToken, userAgent string) error {
	zipName := outDir + ".zip"
	zipPath := filepath.Join(rootDir, zipName)

	if _, err := os.Stat(zipPath); os.IsNotExist(err) {
		return fmt.Errorf("%s not found in %s", zipName, rootDir)
	}

	return UploadFile(baseURL, zipPath, accessToken, userAgent)
}
