package main

import (
	"encoding/json"
	"os"
)

type Config struct {
	DatabaseURL    string `json:"database_url"`
	BuildOutputDir string `json:"build_output_dir"`
	Auth0          struct {
		Domain       string `json:"domain"`
		ClientID     string `json:"client_id"`
		ClientSecret string `json:"client_secret"`
		Audience     string `json:"audience"`
	} `json:"auth0"`
	API struct {
		BaseURL         string `json:"base_url"`
		UploadArtifacts bool   `json:"upload_artifacts"`
	} `json:"api"`
	Builder struct {
		AutoRemove bool `json:"auto_remove"`
	} `json:"builder"`
}

func LoadConfig(path string) (Config, error) {
	data, err := os.ReadFile(path)
	if err != nil {
		return Config{}, err
	}
	var cfg Config
	if err := json.Unmarshal(data, &cfg); err != nil {
		return Config{}, err
	}
	return cfg, nil
}
