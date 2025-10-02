output "region" {
  value = var.region
}

output "profile" {
  value = var.profile
}

output "cluster_name" {
  value = aws_eks_cluster.cluster.name
}
