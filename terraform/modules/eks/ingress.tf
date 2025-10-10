resource "kubernetes_namespace" "traefik" {
  metadata {
    name = "traefik"
  }
}

resource "helm_release" "traefik" {
  repository = "https://traefik.github.io/charts"
  chart      = "traefik"
  name       = "traefik"
  namespace  = kubernetes_namespace.traefik.metadata[0].name
}

data "kubernetes_service" "traefik" {
  metadata {
    name      = helm_release.traefik.name
    namespace = kubernetes_namespace.traefik.metadata[0].name
  }
}
