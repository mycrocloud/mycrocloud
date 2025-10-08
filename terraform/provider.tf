locals {
  kube_config_path = "~/.kube/config"
}
provider "aws" {
  region  = var.region
  profile = var.profile
}

provider "kubernetes" {
  config_path = local.kube_config_path
}

provider "helm" {
  kubernetes = {
    config_path = local.kube_config_path
  }
}
