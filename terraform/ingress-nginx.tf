# Namespace cho ingress
resource "kubernetes_namespace" "nginx_ingress" {
  metadata {
    name = "ingress-nginx"
  }
}

# Cài đặt Helm chart
resource "helm_release" "nginx_ingress" {
  name       = "ingress-nginx"
  repository = "https://kubernetes.github.io/ingress-nginx"
  chart      = "ingress-nginx"
  namespace  = kubernetes_namespace.nginx_ingress.metadata[0].name

  values = [
    yamlencode({
      controller = {
        replicaCount = 2
        service = {
          type = "LoadBalancer"
        }
      }
    })
  ]
}

data "kubernetes_service" "nginx_ingress_lb" {
  metadata {
    name      = "ingress-nginx-controller"
    namespace = kubernetes_namespace.nginx_ingress.metadata[0].name
  }

  depends_on = [helm_release.nginx_ingress]
}