variable "domain" {
  type    = string
  default = "mycrocloud.info"
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

variable "ingress_hostname" {
  type        = string
  description = "LoadBalancer hostname from nginx ingress controller"
}
