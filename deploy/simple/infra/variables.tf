variable "public_key" {
  type = string
}

variable "cloudflare_zone_id" {
  type = string
}

variable "cloudflare_api_token" {
  type      = string
  sensitive = true
}

variable "auth0_domain" {
  type = string
}

variable "auth0_client_id" {
  type = string
}

variable "auth0_client_secret" {
  type      = string
  sensitive = true
}

variable "auth0_github_oauth_app_client_id" {
  type      = string
}

variable "auth0_github_oauth_app_client_secret" {
  type      = string
  sensitive = true
}