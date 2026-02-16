variable "public_key" {
  type    = string
  default = "ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIF+PV+eGYi2awncbY+nyqdaKcev4MV5KTcdcgZyZM4NC nam@mycrocloud.info"
}

variable "cloudflare_control_plane_zone_id" {
  type        = string
  description = "Cloudflare zone ID for the control plane domain (e.g. mycrocloud.online)"
  default     = "058b5763ec271908e7b2dcc26417253c"
}

variable "cloudflare_data_plane_zone_id" {
  type        = string
  description = "Cloudflare zone ID for the data plane domain (e.g. mycrocloud.site)"
  default     = "15409f5c7e9ffa42220f0ebb0a91cfeb"
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
  default = ["https://mycrocloud.online"]
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
