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

variable "grafana_cloud_enabled" {
  type        = bool
  description = "Enable Grafana Cloud Terraform resources and Alloy secret provisioning."
  default     = false
}

variable "grafana_cloud_stack_slug" {
  type        = string
  description = "Grafana Cloud stack slug (for example: my-stack)."
  default     = ""
}

variable "grafana_cloud_access_policy_token" {
  type        = string
  description = "Grafana Cloud access policy token with permission to manage stack access policies/tokens."
  sensitive   = true
  default     = ""
}

variable "grafana_cloud_loki_url" {
  type        = string
  description = "Grafana Cloud Loki push URL (for example: https://logs-prod-xxx.grafana.net/loki/api/v1/push)."
  default     = ""
}

variable "grafana_cloud_loki_username" {
  type        = string
  description = "Grafana Cloud Loki username (stack numeric user ID for logs)."
  default     = ""
}

variable "grafana_cloud_cluster" {
  type        = string
  description = "Cluster label attached to logs sent by Alloy."
  default     = "mycrocloud-prod"
}

variable "grafana_cloud_environment" {
  type        = string
  description = "Environment label attached to logs sent by Alloy."
  default     = "production"
}
