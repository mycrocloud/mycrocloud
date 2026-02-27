data "grafana_cloud_stack" "this" {
  provider = grafana.cloud
  slug     = var.grafana_cloud_stack_slug
}

resource "grafana_cloud_access_policy" "alloy_logs_write" {
  provider = grafana.cloud
  name     = "${local.project_name}-alloy-logs-write"
  region   = data.grafana_cloud_stack.this.region_slug

  scopes = ["logs:write"]

  realm {
    type       = "stack"
    identifier = data.grafana_cloud_stack.this.id
  }
}

resource "grafana_cloud_access_policy_token" "alloy_logs_write" {
  provider         = grafana.cloud
  name             = "${local.project_name}-alloy-logs-write-token"
  region           = data.grafana_cloud_stack.this.region_slug
  access_policy_id = grafana_cloud_access_policy.alloy_logs_write.policy_id
}

resource "aws_secretsmanager_secret_version" "alloy_env" {
  secret_id = aws_secretsmanager_secret.alloy_env.id
  secret_string = jsonencode({
    GRAFANA_CLOUD_LOKI_URL      = data.grafana_cloud_stack.this.logs_url
    GRAFANA_CLOUD_LOKI_USERNAME = tostring(data.grafana_cloud_stack.this.logs_user_id)
    GRAFANA_CLOUD_API_KEY       = grafana_cloud_access_policy_token.alloy_logs_write.token
  })
}
