terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 6.16.0"
    }

    cloudflare = {
      source = "cloudflare/cloudflare"
    }

    auth0 = {
      source = "auth0/auth0"
    }
  }

  backend "s3" {
    bucket = "075313985331-terraform"
    key    = "mycrocloud/infra.tfstate"
  }
}

provider "aws" {
  default_tags {
    tags = {
      environment = "prod"
    }
  }
}

provider "cloudflare" {
  api_token = var.cloudflare_api_token
}
