terraform {
  backend "s3" {
    bucket         = "mycrocloud"
    key            = "terraform.tfstate"
    region         = "ap-northeast-1"
    profile        = "personal"
  }

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.92"
    }
  }

  required_version = ">= 1.2"
}

provider "aws" {
  region                   = "ap-northeast-1"
  shared_credentials_files = ["~/.aws/credentials"]
  profile                  = "personal"
}
