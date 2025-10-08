terraform {
  backend "s3" {
    bucket  = "mycrocloud"
    key     = "terraform.tfstate"
    region  = "ap-northeast-1"
  }

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.92"
    }
  }

  required_version = ">= 1.2"
}


resource "aws_eks_cluster" "cluster" {
  name    = var.project_name
  version = var.k8s_version

  role_arn = aws_iam_role.cluster_role.arn

  vpc_config {
    subnet_ids = [aws_subnet.private_az1.id, aws_subnet.private_az2.id]
  }

  depends_on = [
    aws_iam_role_policy_attachment.AmazonEKSClusterPolicy
  ]
}
