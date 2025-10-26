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

variable "github_oauth_app_client_id" {
  type      = string
}

variable "github_oauth_app_client_secret" {
  type      = string
  sensitive = true
}