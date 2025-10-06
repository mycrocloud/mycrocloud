output "region" {
  value = var.region
}

output "profile" {
  value = var.profile
}

output "cluster_name" {
  value = aws_eks_cluster.cluster.name
}

output "kubeconfi_command" {
  value = "eksctl utils write-kubeconfig --region ${var.region} --cluster ${aws_eks_cluster.cluster.name} --profile ${var.profile}"
}
