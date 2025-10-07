
resource "aws_iam_openid_connect_provider" "oidc_provider" {
  client_id_list = ["sts.amazonaws.com"]
  url = aws_eks_cluster.cluster.identity[0].oidc[0].issuer
}

resource "aws_iam_role" "alb_role" {
  assume_role_policy = templatefile("alb_trust_policy.json.tftpl", {
    oidc_provider_arn = aws_iam_openid_connect_provider.oidc_provider.arn,
    oidc_provider_url = replace(aws_eks_cluster.cluster.identity[0].oidc[0].issuer, "https://", "")
  })
}

resource "aws_iam_policy" "AWSLoadBalancerControllerIAMPolicy" {
  name = "AWSLoadBalancerControllerIAMPolicy"
  policy = file("iam_policy.json")
}

resource "aws_iam_role_policy_attachment" "alb_role_attachment" {
  role       = aws_iam_role.alb_role.name
  policy_arn = aws_iam_policy.AWSLoadBalancerControllerIAMPolicy.arn
}

resource "kubernetes_service_account" "alb" {
  metadata {
    name = "aws-load-balancer-controller"
    namespace = "kube-system"
    annotations = {
      "eks.amazonaws.com/role-arn" = aws_iam_role.alb_role.arn
    }
  }
}