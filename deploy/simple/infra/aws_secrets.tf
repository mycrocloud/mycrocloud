# =============================================================================
# AWS Secrets Manager - Secret resources only (values managed via AWS Console)
# =============================================================================

resource "aws_secretsmanager_secret" "env" {
  name        = "prod/mycrocloud/.env"
  description = "Main environment variables for mycrocloud"
}

resource "aws_secretsmanager_secret" "lb_certs_key" {
  name        = "prod/mycrocloud/lb/certs/mycrocloud.info.key"
  description = "SSL private key for mycrocloud.info"
}

resource "aws_secretsmanager_secret" "webapp_deployment_env" {
  name        = "prod/mycrocloud/Services/WebApp/deployment/.env"
  description = "Environment variables for WebApp deployment"
}

resource "aws_secretsmanager_secret" "webapp_api_gha_pem" {
  name        = "prod/mycrocloud/Services/WebApp/WebApp.Api/gha-mycrocloud.pem"
  description = "GitHub App private key for WebApp API"
}

resource "aws_secretsmanager_secret" "grafana_cloud" {
  name        = "mycrocloud/monitoring/grafana-cloud"
  description = "Grafana Cloud credentials for Prometheus remote write"
}