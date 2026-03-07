resource "grafana_cloud_stack" "this" {
  provider          = grafana.cloud
  name              = local.project_name
  slug              = var.grafana_cloud_stack_slug
  region_slug       = var.grafana_cloud_region_slug
  delete_protection = true
}

data "grafana_cloud_stack" "this" {
  provider = grafana.cloud
  slug     = grafana_cloud_stack.this.slug

  depends_on = [grafana_cloud_stack.this]
}

resource "grafana_cloud_stack_service_account" "terraform" {
  provider    = grafana.cloud
  stack_slug  = grafana_cloud_stack.this.slug
  name        = "terraform"
  role        = "Admin"
  is_disabled = false
}

resource "grafana_cloud_stack_service_account_token" "terraform" {
  provider           = grafana.cloud
  stack_slug         = grafana_cloud_stack.this.slug
  name               = "terraform"
  service_account_id = grafana_cloud_stack_service_account.terraform.id
}

resource "grafana_cloud_access_policy" "alloy_logs_write" {
  provider = grafana.cloud
  name     = "${local.project_name}-alloy-logs-write"
  region   = grafana_cloud_stack.this.region_slug

  scopes = ["logs:write"]

  realm {
    type       = "stack"
    identifier = grafana_cloud_stack.this.id
  }
}

resource "grafana_cloud_access_policy_token" "alloy_logs_write" {
  provider         = grafana.cloud
  name             = "${local.project_name}-alloy-logs-write-token"
  region           = grafana_cloud_stack.this.region_slug
  access_policy_id = grafana_cloud_access_policy.alloy_logs_write.policy_id
}

resource "grafana_cloud_access_policy" "prometheus_metrics_write" {
  provider = grafana.cloud
  name     = "${local.project_name}-prometheus-metrics-write"
  region   = grafana_cloud_stack.this.region_slug

  scopes = ["metrics:write"]

  realm {
    type       = "stack"
    identifier = grafana_cloud_stack.this.id
  }
}

resource "grafana_cloud_access_policy_token" "prometheus_metrics_write" {
  provider         = grafana.cloud
  name             = "${local.project_name}-prometheus-metrics-write-token"
  region           = grafana_cloud_stack.this.region_slug
  access_policy_id = grafana_cloud_access_policy.prometheus_metrics_write.policy_id
}
