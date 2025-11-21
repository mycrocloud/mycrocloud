variable "domain" {
  type = string
}

variable "client_id" {
  type = string
}

variable "client_secret" {
  type      = string
  sensitive = true
}

variable "web_origin" {
  type = list(string)
}

variable "github_oauth_app_client_id" {
  type      = string
}

variable "github_oauth_app_client_secret" {
  type      = string
  sensitive = true
}

variable "google_oauth_app_client_id" {
  type      = string
}

variable "google_oauth_app_client_secret" {
  type      = string
  sensitive = true
}