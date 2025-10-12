variable "cloudflare_api_token" {
  type        = string
  sensitive   = true
  description = "Cloudflare API token for DNS management"
}

variable "cloudflare_account_id" {
  type        = string
  description = "Cloudflare account ID"
}

variable "cloudflare_zone_id" {
  type        = string
  description = "Cloudflare zone ID for the domain"
}

variable "domain" {
  type = string
}

variable "project_name" {
  type    = string
  default = "mycrocloud"
}

variable "aws_region" {
  type    = string
  default = "ap-northeast-1"
}

variable "k8s_version" {
  type    = string
  default = "1.33"
}

variable "load_balancer_hostname" {
  type = string
}