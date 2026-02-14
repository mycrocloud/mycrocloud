variable "public_key" {
  type    = string
  default = "ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIF+PV+eGYi2awncbY+nyqdaKcev4MV5KTcdcgZyZM4NC nam@mycrocloud.info"
}

variable "cloudflare_zone_id" {
  type    = string
  default = "c719907867c6f32aa82a8258adbf337a"
}

variable "cloudflare_api_token" {
  type      = string
  sensitive = true
}

variable "auth0_domain" {
  type    = string
  default = "dev-vzxphouz.us.auth0.com" # custom domain won't work
}

variable "auth0_client_id" {
  type    = string
  default = "KN9Y1imMXFv7CO5jjdY2LTBEavFlP4fU"
}

variable "auth0_client_secret" {
  type      = string
  sensitive = true
}

variable "web_origin" {
  type    = list(string)
  default = ["https://mycrocloud.info"]
}

variable "auth0_github_oauth_app_client_id" {
  type    = string
  default = "0b52124cb3636aa4e203"
}

variable "auth0_github_oauth_app_client_secret" {
  type      = string
  sensitive = true
}

variable "auth0_google_oauth_app_client_id" {
  type    = string
  default = "895232793314-iuf6v44ihi4ar16ei5ssmm6j69tl1m48.apps.googleusercontent.com"
}

variable "auth0_google_oauth_app_client_secret" {
  type      = string
  sensitive = true
}

variable "neon_api_key" {
  type      = string
  sensitive = true
}