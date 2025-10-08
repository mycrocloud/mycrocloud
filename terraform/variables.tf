variable "region" {
  type    = string
  default = "ap-northeast-1"
}

variable "project_name" {
  type    = string
  default = "mycrocloud"
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
