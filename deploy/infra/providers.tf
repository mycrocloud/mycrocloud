terraform {
  required_providers {
    conohavps = {
      source = "gmo-internet/conohavps"
    }

    aws = {
      source  = "hashicorp/aws"
      version = "~> 6.16.0"
    }

    cloudflare = {
      source  = "cloudflare/cloudflare"
      version = "~> 5.17"
    }

    auth0 = {
      source  = "auth0/auth0"
      version = "~> 1.40"
    }

    grafana = {
      source  = "grafana/grafana"
      version = "~> 4.0"
    }

    http = {
      source  = "hashicorp/http"
      version = "~> 3.4"
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
provider "conohavps" {
  password  = var.conohavps_password
  tenant_id = "fc13fd9d02d9410cb8230bc190acd48f"
  user_id   = "f2293c5c0f684dfca401fa56d35e9017"
}

provider "cloudflare" {
  api_token = var.cloudflare_api_token
}

provider "auth0" {
  domain        = var.auth0_domain
  client_id     = var.auth0_client_id
  client_secret = var.auth0_client_secret
}

provider "grafana" {
  alias = "cloud"

  cloud_access_policy_token = var.grafana_cloud_access_policy_token
}

provider "grafana" {
  alias = "stack"
  url   = grafana_cloud_stack.this.url
  auth  = grafana_cloud_stack_service_account_token.terraform.key
}
