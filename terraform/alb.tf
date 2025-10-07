
resource "aws_iam_openid_connect_provider" "default" {
  client_id_list = ["sts.amazonaws.com"]
  url = aws_eks_cluster.cluster.identity[0].oidc[0].issuer
}

resource "aws_iam_policy" "AWSLoadBalancerControllerIAMPolicy" {
  policy = file("iam_policy.json")
}

resource "kubernetes_service_account" "alb" {
  metadata {
    name = ""
    namespace = "kube-system"
  }
}