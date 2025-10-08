variable "region" {
  type    = string
  default = "ap-northeast-1"
}

variable "project_name" {
  type    = string
  default = "mycrocloud"
}

variable "domain" {
  type    = string
  default = "mycrocloud.info"
}

variable "k8s_version" {
  type    = string
  default = "1.33"
}

variable "ghcr_pat" {
  type      = string
  sensitive = true
}

variable "ghcr_username" {
  type = string
}

variable "cloudflare_api_token" {
  type      = string
  sensitive = true
}

variable "cloudflare_account_id" {
  type = string
}

variable "cloudflare_zone_id" {
  type = string
}