resource "aws_eks_node_group" "monitoring" {
  cluster_name    = aws_eks_cluster.cluster.name
  node_group_name = "monitoring-ng"
  node_role_arn   = aws_iam_role.node_group_role.arn
  subnet_ids = [
    aws_subnet.private_az1.id,
    aws_subnet.private_az2.id,
  ]

  instance_types = ["t3.large"]

  scaling_config {
    desired_size = 1
    max_size     = 2
    min_size     = 1
  }

  labels = {
    "node-type" = "monitoring"
  }
}