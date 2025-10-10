output "cluster_name" {
  description = "EKS cluster name"
  value       = aws_eks_cluster.cluster.name
}

output "cluster_endpoint" {
  description = "EKS cluster endpoint"
  value       = aws_eks_cluster.cluster.endpoint
}

output "cluster_certificate_authority" {
  description = "EKS cluster certificate authority data"
  value       = aws_eks_cluster.cluster.certificate_authority[0].data
  sensitive   = true
}

output "load_balancer_hostname" {
  description = "LoadBalancer hostname"
  value       = data.kubernetes_service.traefik.status[0].load_balancer[0].ingress[0].hostname
}
