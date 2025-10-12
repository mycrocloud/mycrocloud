resource "aws_eks_cluster" "cluster" {
  name    = var.cluster_name
  version = var.k8s_version

  role_arn = aws_iam_role.cluster_role.arn

  vpc_config {
    subnet_ids = [aws_subnet.private_az1.id, aws_subnet.private_az2.id]
  }

  depends_on = [
    aws_iam_role_policy_attachment.AmazonEKSClusterPolicy
  ]
}

resource "aws_eks_node_group" "node_group" {
  cluster_name = aws_eks_cluster.cluster.name
  scaling_config {
    desired_size = 1
    min_size     = 1
    max_size     = 2
  }

  node_role_arn = aws_iam_role.node_group_role.arn
  subnet_ids = [
    aws_subnet.private_az1.id,
    aws_subnet.private_az2.id,
  ]
  instance_types = ["t3.small"]

  depends_on = [
    aws_iam_role_policy_attachment.AmazonEC2ContainerRegistryReadOnly,
    aws_iam_role_policy_attachment.AmazonEKS_CNI_Policy,
    aws_iam_role_policy_attachment.AmazonEKSWorkerNodePolicy,
  ]
}
