//go:build integration
// +build integration

package api_client

import (
	"os"
	"testing"
)

func TestGetAccessToken_Auth0Real(t *testing.T) {
	domain := os.Getenv("AUTH0_DOMAIN")
	clientID := os.Getenv("AUTH0_CLIENT_ID")
	clientSecret := os.Getenv("AUTH0_SECRET")
	audience := os.Getenv("AUTH0_AUDIENCE")

	if domain == "" || clientID == "" || clientSecret == "" || audience == "" {
		t.Skip("Missing Auth0 credentials in environment variables")
	}

	cfg := Config{
		Domain:       domain,
		ClientID:     clientID,
		ClientSecret: clientSecret,
		Audience:     audience,
	}

	token, err := GetAccessToken(cfg)
	if err != nil {
		t.Fatalf("Auth0 token request failed: %v", err)
	}

	if len(token) < 10 {
		t.Fatalf("Token returned from Auth0 looks invalid: %s", token)
	}

	t.Logf("Successfully received Auth0 token.")
}
