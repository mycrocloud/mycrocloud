resource "aws_secretsmanager_secret" "grafana_cloud" {
  name        = "mycrocloud/monitoring/grafana-cloud"
  description = "Grafana Cloud credentials for Prometheus remote write"
}

resource "aws_secretsmanager_secret_version" "grafana_cloud" {
  secret_id = aws_secretsmanager_secret.grafana_cloud.id
  secret_string = jsonencode({
    GRAFANA_CLOUD_USER_ID = var.grafana_cloud_user_id
    GRAFANA_CLOUD_API_KEY = var.grafana_cloud_api_key
  })
}

variable "grafana_cloud_user_id" {
  description = "Grafana Cloud User ID for Prometheus remote write"
  type        = string
  sensitive   = true
}

variable "grafana_cloud_api_key" {
  description = "Grafana Cloud API Key for Prometheus remote write"
  type        = string
  sensitive   = true
}