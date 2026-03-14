locals {
  api_secrets = [
    "db_connection_string",
    "db_pubsub_connection_string",
    "github_webhook_secret",
    "slack_client_secret",
    "slack_signing_secret",
    "slack_link_secret",
    "s3_access_key",
    "s3_secret_key",
    "gha_mycrocloud_private_key",
  ]

  dbmigrator_secrets = [
    "db_connection_string",
  ]

  lb_secrets = [
    "mycrocloud_online_key",
    "mycrocloud_site_key",
  ]

  monitoring_alloy_secrets = [
    "grafana_cloud_api_key",
  ]

  monitoring_prometheus_secrets = [
    "grafana_api_key",
  ]

  webapp_gateway_secrets = [
    "db_connection_string",
    "s3_access_key",
    "s3_secret_key",
  ]

  webapp_spa_build_worker_secrets = [
    "database_url",
    "auth0_secret",
  ]

  ssm_prefix = "/mycrocloud"
}

resource "aws_ssm_parameter" "api_secrets" {
  for_each = toset(local.api_secrets)
  name     = "${local.ssm_prefix}/api/${each.value}"
  type     = "SecureString"
  value    = "PLACEHOLDER"

  lifecycle {
    ignore_changes = [value]
  }
}

resource "aws_ssm_parameter" "dbmigrator_secrets" {
  for_each = toset(local.dbmigrator_secrets)
  name     = "${local.ssm_prefix}/db-migrator/${each.value}"
  type     = "SecureString"
  value    = "PLACEHOLDER"

  lifecycle {
    ignore_changes = [value]
  }
}

resource "aws_ssm_parameter" "lb_secrets" {
  for_each = toset(local.lb_secrets)
  name     = "${local.ssm_prefix}/lb/${each.value}"
  type     = "SecureString"
  value    = "PLACEHOLDER"

  lifecycle {
    ignore_changes = [value]
  }
}

resource "aws_ssm_parameter" "monitoring_alloy_secrets" {
  for_each = toset(local.monitoring_alloy_secrets)
  name     = "${local.ssm_prefix}/monitoring/alloy/${each.value}"
  type     = "SecureString"
  value    = "PLACEHOLDER"

  lifecycle {
    ignore_changes = [value]
  }
}

resource "aws_ssm_parameter" "monitoring_prometheus_secrets" {
  for_each = toset(local.monitoring_prometheus_secrets)
  name     = "${local.ssm_prefix}/monitoring/prometheus/${each.value}"
  type     = "SecureString"
  value    = "PLACEHOLDER"

  lifecycle {
    ignore_changes = [value]
  }
}

resource "aws_ssm_parameter" "webapp_gateway_secrets" {
  for_each = toset(local.webapp_gateway_secrets)
  name     = "${local.ssm_prefix}/webapp/gateway/${each.value}"
  type     = "SecureString"
  value    = "PLACEHOLDER"

  lifecycle {
    ignore_changes = [value]
  }
}

resource "aws_ssm_parameter" "webapp_spa_build_worker_secrets" {
  for_each = toset(local.webapp_spa_build_worker_secrets)
  name     = "${local.ssm_prefix}/webapp/spa/build-worker/${each.value}"
  type     = "SecureString"
  value    = "PLACEHOLDER"

  lifecycle {
    ignore_changes = [value]
  }
}
