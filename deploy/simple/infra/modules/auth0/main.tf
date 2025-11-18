locals {
  domain = "mycrocloud.info"
}

resource "auth0_client" "web" {
  name = "Web"
  app_type = "spa"
  callbacks = [ var.web_origin ]
  allowed_logout_urls = [ var.web_origin ]
  web_origins = [ var.web_origin ]
  
  //TODO: confirm what these do
  cross_origin_auth = true
  organization_require_behavior = "no_prompt"
}

resource "auth0_connection" "github" {
  name = "GitHub"
  strategy = "github"

  options {
    client_id = var.github_oauth_app_client_id
    client_secret = var.github_oauth_app_client_secret
  }
}

resource "auth0_connection_client" "web_github" {
  client_id = auth0_client.web.client_id
  connection_id = auth0_connection.github.id
}

resource "auth0_connection" "google" {
  name = "Google"
  strategy = "google-oauth2"

  options {
    client_id = var.google_oauth_app_client_id
    client_secret = var.google_oauth_app_client_secret
  }
}

resource "auth0_connection_client" "web_google" {
  client_id = auth0_client.web.client_id
  connection_id = auth0_connection.google.id
}

resource "auth0_resource_server" "api" {
  identifier = "mycrocloud-api"
  name = "API"
}

resource "auth0_client" "build_worker" {
  name = "Build Worker"
  app_type = "non_interactive"
}

resource "auth0_client_grant" "build_worker_api_grant" {
  client_id = auth0_client.build_worker.client_id
  audience = auth0_resource_server.api.identifier
  scopes = []
}