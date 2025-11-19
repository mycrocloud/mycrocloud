//go:build integration
// +build integration

package uploader

import (
	"mycrocloud/worker/api_client"
	"os"
	"path/filepath"
	"testing"
)

var uploadURL = "https://office-api.smashup1805.dpdns.org/apps/1/builds/6fa5fdea-8760-4faf-994c-c94d6f25223c/artifacts" //TODO: fix this

func TestUploadFile_Integration(t *testing.T) {
	if uploadURL == "" {
		t.Skip("Missing UPLOAD_ENDPOINT environment variable")
	}

	cfg := api_client.Config{
		Domain:       os.Getenv("AUTH0_DOMAIN"),
		ClientID:     os.Getenv("AUTH0_CLIENT_ID"),
		ClientSecret: os.Getenv("AUTH0_SECRET"),
		Audience:     os.Getenv("AUTH0_AUDIENCE"),
	}

	token, err := api_client.GetAccessToken(cfg)
	if err != nil {
		t.Fatalf("Failed to get access token: %v", err)
	}

	tmpFile, err := os.CreateTemp("", "upload_test_*.txt")
	if err != nil {
		t.Fatalf("Failed to create temp file: %v", err)
	}
	defer os.Remove(tmpFile.Name())

	tmpFile.WriteString("Hello from uploader integration test!")

	// Step 3: Gọi uploader thật
	err = UploadFile(uploadURL+"/"+tmpFile.Name(), tmpFile.Name(), token)
	if err != nil {
		t.Fatalf("UploadFile failed: %v", err)
	}
}
func TestUploadArtifacts_Integration(t *testing.T) {
	if uploadURL == "" {
		t.Skip("Missing UPLOAD_ENDPOINT environment variable")
	}

	cfg := api_client.Config{
		Domain:       os.Getenv("AUTH0_DOMAIN"),
		ClientID:     os.Getenv("AUTH0_CLIENT_ID"),
		ClientSecret: os.Getenv("AUTH0_SECRET"),
		Audience:     os.Getenv("AUTH0_AUDIENCE"),
	}

	token, err := api_client.GetAccessToken(cfg)
	if err != nil {
		t.Fatalf("Failed to get access token: %v", err)
	}

	tmpDir, err := os.MkdirTemp("", "upload_dir_test")
	if err != nil {
		t.Fatalf("Failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tmpDir)

	os.WriteFile(filepath.Join(tmpDir, "a.txt"), []byte("AAA"), 0644)
	os.WriteFile(filepath.Join(tmpDir, "b.txt"), []byte("BBB"), 0644)

	subDir := filepath.Join(tmpDir, "sub")
	os.Mkdir(subDir, 0755)
	os.WriteFile(filepath.Join(subDir, "c.txt"), []byte("CCC"), 0644)

	err = UploadArtifacts(uploadURL, tmpDir, token)
	if err != nil {
		t.Fatal(err)
	}
}

func TestUploadArtifacts_FromExistingFolder(t *testing.T) {
	rootDir := "/tmp/build-outputs/6fa5fdea-8760-4faf-994c-c94d6f25223c"
	// Check folder exists
	if _, err := os.Stat(rootDir); err != nil {
		t.Skip("Folder does not exist on this machine: " + rootDir)
	}

	// Get Auth0 token
	cfg := api_client.Config{
		Domain:       os.Getenv("AUTH0_DOMAIN"),
		ClientID:     os.Getenv("AUTH0_CLIENT_ID"),
		ClientSecret: os.Getenv("AUTH0_SECRET"),
		Audience:     os.Getenv("AUTH0_AUDIENCE"),
	}

	token, err := api_client.GetAccessToken(cfg)
	if err != nil {
		t.Fatalf("GetAccessToken: %v", err)
	}

	err = UploadArtifacts(uploadURL, rootDir, token)
	if err != nil {
		t.Fatalf("UploadArtifacts failed: %v", err)
	}
}
