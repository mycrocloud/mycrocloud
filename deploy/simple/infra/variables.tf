variable "project_name" {
  type        = string
  default     = "mycrocloud"
}

variable "public_key" {
  type = string
}

variable "cloudflare_zone_id" {
  type = string
}

variable "cloudflare_api_token" {
  type = string
  sensitive = true
}