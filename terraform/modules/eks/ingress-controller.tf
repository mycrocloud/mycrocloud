resource "kubernetes_namespace" "traefik" {
  metadata {
    name = "traefik"
  }
}

resource "helm_release" "traefik" {
  repository = "https://traefik.github.io/charts"
  chart      = "traefik"
  name       = "traefik"

  namespace = kubernetes_namespace.traefik.metadata[0].name

  set = [{
    name  = "service.type"
    value = "LoadBalancer"
    }, {
    name  = "ingressClass.enabled"
    value = "true"
    }, {
    name  = "ingressClass.isDefaultClass"
    value = "true"
  }]
}

data "kubernetes_service" "traefik" {
  metadata {
    name      = "traefik"
    namespace = kubernetes_namespace.traefik.metadata[0].name
  }

  depends_on = [helm_release.traefik]
}