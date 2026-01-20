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
    bucket = ""
    key    = ""
  }
}

provider "aws" {}

provider "cloudflare" {
  api_token = var.cloudflare_api_token
}

data "aws_caller_identity" "current" {}
data "aws_region" "current" {}


module "auth0" {
  source                         = "./modules/auth0"
  domain                         = var.auth0_domain
  client_id                      = var.auth0_client_id
  client_secret                  = var.auth0_client_secret
  web_origin                     = var.web_origin
  github_oauth_app_client_id     = var.auth0_github_oauth_app_client_id
  github_oauth_app_client_secret = var.auth0_github_oauth_app_client_secret
  google_oauth_app_client_id     = var.auth0_google_oauth_app_client_id
  google_oauth_app_client_secret = var.auth0_google_oauth_app_client_secret
}

locals {
  project_name = "mycrocloud"
}

locals {
  domain = "mycrocloud.info"
}

data "aws_ami" "ubuntu" {
  most_recent = true
  owners      = ["099720109477"] // Canonical
  filter {
    name   = "name"
    values = ["ubuntu/images/hvm-ssd/ubuntu-focal-20.04-amd64-server-*"]
  }
  filter {
    name   = "virtualization-type"
    values = ["hvm"]
  }
}

resource "aws_key_pair" "ssh_key" {
  key_name   = "${local.project_name}-key"
  public_key = var.public_key

  tags = {
    Name = "${local.project_name}-key"
  }
}

resource "aws_vpc" "vpc" {
  cidr_block           = "10.0.0.0/16"
  enable_dns_hostnames = true
  enable_dns_support   = true

  tags = {
    Name = "${local.project_name}-vpc"
  }
}

resource "aws_internet_gateway" "ig" {
  vpc_id = aws_vpc.vpc.id

  tags = {
    Name = "${local.project_name}-igw"
  }
}

resource "aws_subnet" "subnet" {
  vpc_id     = aws_vpc.vpc.id
  cidr_block = "10.0.1.0/24"

  tags = {
    Name = "${local.project_name}-subnet"
  }
}

resource "aws_route_table" "public" {
  vpc_id = aws_vpc.vpc.id

  route {
    cidr_block = "0.0.0.0/0"
    gateway_id = aws_internet_gateway.ig.id
  }

  tags = {
    Name = "${local.project_name}-public-rt"
  }
}

resource "aws_route_table_association" "public_assoc" {
  route_table_id = aws_route_table.public.id
  subnet_id      = aws_subnet.subnet.id
}

resource "aws_security_group" "sg" {
  name        = "${local.project_name}-server-sg"
  description = "Security group for server instance"
  vpc_id      = aws_vpc.vpc.id

  ingress {
    description = "SSH from anywhere"
    from_port   = 22
    to_port     = 22
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  ingress {
    description = "HTTP from anywhere"
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  ingress {
    description = "HTTPS from anywhere"
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  ingress {
    description = "SEQ"
    from_port   = 5341
    to_port     = 5341
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    description = "Allow all outbound traffic"
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name = "${local.project_name}-server-sg"
  }
}

resource "aws_instance" "server" {
  ami           = data.aws_ami.ubuntu.id
  instance_type = "t3.small"
  root_block_device {
    volume_size = 20
  }

  key_name                    = aws_key_pair.ssh_key.key_name
  associate_public_ip_address = true
  subnet_id                   = aws_subnet.subnet.id
  vpc_security_group_ids      = [aws_security_group.sg.id]

  tags = {
    Name = "${local.project_name}-server"
  }
}

data "cloudflare_zone" "zone" {
  zone_id = var.cloudflare_zone_id
}

resource "cloudflare_dns_record" "apex" {
  zone_id = data.cloudflare_zone.zone.zone_id
  name    = "@"
  type    = "A"
  ttl     = 1
  content = aws_instance.server.public_ip
  proxied = true
}

resource "cloudflare_dns_record" "editor" {
  zone_id = data.cloudflare_zone.zone.zone_id
  name    = "editor"
  type    = "CNAME"
  ttl     = 1
  proxied = true
  content = local.domain
}

resource "cloudflare_dns_record" "wildcard" {
  zone_id = data.cloudflare_zone.zone.zone_id
  name    = "*"
  type    = "CNAME"
  ttl     = 1
  proxied = true
  content = local.domain
}

resource "cloudflare_dns_record" "api" {
  zone_id = data.cloudflare_zone.zone.zone_id
  name    = "api"
  type    = "CNAME"
  ttl     = 1
  proxied = true
  content = "mycrocloud.info"
}

resource "cloudflare_dns_record" "slack_integration_api" {
  zone_id = data.cloudflare_zone.zone.zone_id
  name    = "slack-integration-api"
  type    = "CNAME"
  ttl     = 1
  proxied = true
  content = local.domain
}