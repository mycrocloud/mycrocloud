terraform {
  backend "s3" {
    bucket = "mycrocloud"
    key    = "terraform.tfstate"
    region = "ap-northeast-1"
  }

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.92"
    }

    cloudflare = {
      source  = "cloudflare/cloudflare"
      version = "~> 5"
    }

    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = "~> 2.38"
    }

    helm = {
      source  = "hashicorp/helm"
      version = "~> 3.0"
    }
  }

  required_version = ">= 1.2"
}

module "eks" {
  source       = "./modules/eks"
  cluster_name = var.project_name
  aws_region   = var.aws_region
  k8s_version  = var.k8s_version
}

module "cloudflare" {
  source = "./modules/cloudflare"

  cloudflare_api_token  = var.cloudflare_api_token
  cloudflare_account_id = var.cloudflare_account_id
  cloudflare_zone_id    = var.cloudflare_zone_id
  hostname              = var.load_balancer_hostname
}
