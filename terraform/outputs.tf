output "region" {
  value = var.region
}

output "cluster_name" {
  value = aws_eks_cluster.cluster.name
}

output "kubeconfig_command" {
  value = "eksctl utils write-kubeconfig --region ${var.region} --cluster ${aws_eks_cluster.cluster.name}"
}
