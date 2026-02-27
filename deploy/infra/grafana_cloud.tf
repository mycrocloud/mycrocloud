locals {
  grafana_cloud_enabled = var.grafana_cloud_enabled
}

data "grafana_cloud_stack" "this" {
  count = local.grafana_cloud_enabled ? 1 : 0

  provider = grafana.cloud
  slug     = var.grafana_cloud_stack_slug
}

resource "grafana_cloud_access_policy" "alloy_logs_write" {
  count = local.grafana_cloud_enabled ? 1 : 0

  provider = grafana.cloud
  name     = "${local.project_name}-alloy-logs-write"
  region   = data.grafana_cloud_stack.this[0].region_slug

  scopes = ["logs:write"]

  realm {
    type       = "stack"
    identifier = data.grafana_cloud_stack.this[0].id
  }
}

resource "grafana_cloud_access_policy_token" "alloy_logs_write" {
  count = local.grafana_cloud_enabled ? 1 : 0

  provider         = grafana.cloud
  name             = "${local.project_name}-alloy-logs-write-token"
  region           = data.grafana_cloud_stack.this[0].region_slug
  access_policy_id = grafana_cloud_access_policy.alloy_logs_write[0].policy_id
}

resource "aws_secretsmanager_secret_version" "alloy_env" {
  count = local.grafana_cloud_enabled ? 1 : 0

  secret_id = aws_secretsmanager_secret.alloy_env.id
  secret_string = jsonencode({
    GRAFANA_CLOUD_LOKI_URL      = var.grafana_cloud_loki_url
    GRAFANA_CLOUD_LOKI_USERNAME = var.grafana_cloud_loki_username
    GRAFANA_CLOUD_API_KEY       = grafana_cloud_access_policy_token.alloy_logs_write[0].token
    GRAFANA_CLOUD_CLUSTER       = var.grafana_cloud_cluster
    GRAFANA_CLOUD_ENVIRONMENT   = var.grafana_cloud_environment
  })
}
