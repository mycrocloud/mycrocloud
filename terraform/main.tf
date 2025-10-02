terraform {
  backend "s3" {
    bucket  = "mycrocloud"
    key     = "terraform.tfstate"
    region  = "ap-northeast-1"
    profile = "personal"
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
    subnet_ids = [aws_subnet.az1.id, aws_subnet.az2.id, aws_subnet.az3.id]
  }

  compute_config {
    enabled       = true
    node_pools    = ["general-purpose"]
    node_role_arn = aws_iam_role.node_role.arn
  }

  kubernetes_network_config {
    elastic_load_balancing {
      enabled = true
    }
  }

  storage_config {
    block_storage {
      enabled = true
    }
  }

  bootstrap_self_managed_addons = true

  depends_on = [
    aws_iam_role_policy_attachment.AmazonEKSBlockStoragePolicy,
    aws_iam_role_policy_attachment.AmazonEKSClusterPolicy,
    aws_iam_role_policy_attachment.AmazonEKSComputePolicy,
    aws_iam_role_policy_attachment.AmazonEKSLoadBalancingPolicy,
    aws_iam_role_policy_attachment.AmazonEKSNetworkingPolicy
  ]
}
