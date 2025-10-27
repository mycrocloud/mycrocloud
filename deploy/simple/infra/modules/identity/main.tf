locals {
  domain = "mycrocloud.info"
}

resource "auth0_client" "web" {
  name = "mycrocloud"
  app_type = "spa"
  callbacks = [ "https://${local.domain}" ]
  allowed_logout_urls = [ "https://${local.domain}" ]
  web_origins = [ "https://${local.domain}" ]
  
  //TODO: confirm what these do
  cross_origin_auth = true
  organization_require_behavior = "no_prompt"
}

resource "auth0_connection" "github" {
  name = "github"
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
  name = "google-oauth2"
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
  name = "mycrocloud"
}