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
    values = ["ubuntu/images/hvm-ssd-gp3/ubuntu-noble-24.04-amd64-server-*"]
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

resource "aws_iam_role" "ec2_ssm" {
  name = "${local.project_name}-ec2-ssm"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Principal = {
          Service = "ec2.amazonaws.com"
        }
        Action = "sts:AssumeRole"
      }
    ]
  })
}

resource "aws_iam_role_policy_attachment" "ec2_ssm" {
  role       = aws_iam_role.ec2_ssm.name
  policy_arn = "arn:aws:iam::aws:policy/AmazonSSMManagedInstanceCore"
}

resource "aws_iam_instance_profile" "ec2_ssm" {
  name = "${local.project_name}-ec2-ssm"
  role = aws_iam_role.ec2_ssm.name
}

resource "aws_instance" "server" {
  ami           = data.aws_ami.ubuntu.id
  instance_type = "t3.small"
  root_block_device {
    volume_size = 20
  }

  key_name                    = aws_key_pair.ssh_key.key_name
  iam_instance_profile        = aws_iam_instance_profile.ec2_ssm.name
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

resource "aws_iam_openid_connect_provider" "github" {
  url = "https://token.actions.githubusercontent.com"

  client_id_list = [
    "sts.amazonaws.com"
  ]

  thumbprint_list = [
    "6938fd4d98bab03faadb97b34396831e3780aea1"
  ]
}

resource "aws_iam_role" "github_actions" {
  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Principal = {
          Federated = aws_iam_openid_connect_provider.github.arn
        }
        Action = "sts:AssumeRoleWithWebIdentity"
        Condition = {
          StringEquals = {
            "token.actions.githubusercontent.com:aud" = "sts.amazonaws.com"
            "token.actions.githubusercontent.com:sub" = "repo:mycrocloud/mycrocloud:ref:refs/heads/main"
          }
        }
      }
    ]
  })
}
resource "aws_iam_policy" "secrets_read" {
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "secretsmanager:GetSecretValue"
        ]
        # TODO: Temporarily allow all secrets - revert to aws_secretsmanager_secret.openvpn.arn
        Resource = "*"
      }
    ]
  })
}

resource "aws_iam_policy" "ssm_session" {
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "ssm:StartSession",
          "ssm:TerminateSession",
          "ssm:ResumeSession",
          "ssm:DescribeSessions"
        ]
        Resource = [
          "arn:aws:ec2:${data.aws_region.current.name}:${data.aws_caller_identity.current.account_id}:instance/*",
          "arn:aws:ssm:${data.aws_region.current.name}:${data.aws_caller_identity.current.account_id}:session/*"
        ]
      }
    ]
  })
}

resource "aws_iam_role_policy_attachment" "attach" {
  role       = aws_iam_role.github_actions.name
  policy_arn = aws_iam_policy.secrets_read.arn
}

resource "aws_iam_role_policy_attachment" "attach_ssm_session" {
  role       = aws_iam_role.github_actions.name
  policy_arn = aws_iam_policy.ssm_session.arn
}