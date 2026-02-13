package api_client

import (
	"bytes"
	"encoding/json"
	"fmt"
	"io"
	"net/http"
)

type Config struct {
	Domain       string
	ClientID     string
	ClientSecret string
	Audience     string
}

type TokenResponse struct {
	AccessToken string `json:"access_token"`
	TokenType   string `json:"token_type"`
}

func GetAccessToken(cfg Config) (string, error) {
	url := cfg.Domain + "/oauth/token"

	payload := map[string]string{
		"client_id":     cfg.ClientID,
		"client_secret": cfg.ClientSecret,
		"audience":      cfg.Audience,
		"grant_type":    "client_credentials",
	}

	body, err := json.Marshal(payload)
	if err != nil {
		return "", fmt.Errorf("marshal payload: %w", err)
	}

	req, err := http.NewRequest(http.MethodPost, url, bytes.NewReader(body))
	if err != nil {
		return "", fmt.Errorf("create request: %w", err)
	}
	req.Header.Set("Content-Type", "application/json")

	res, err := http.DefaultClient.Do(req)
	if err != nil {
		return "", fmt.Errorf("send request: %w", err)
	}
	defer res.Body.Close()

	respBody, err := io.ReadAll(res.Body)
	if err != nil {
		return "", fmt.Errorf("read response: %w", err)
	}

	if res.StatusCode < 200 || res.StatusCode >= 300 {
		return "", fmt.Errorf("auth failed: %d %s", res.StatusCode, string(respBody))
	}

	var tr TokenResponse
	if err := json.Unmarshal(respBody, &tr); err != nil {
		return "", fmt.Errorf("unmarshal response: %w", err)
	}

	return tr.AccessToken, nil
}
