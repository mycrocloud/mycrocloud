variable "cluster_name" {
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
