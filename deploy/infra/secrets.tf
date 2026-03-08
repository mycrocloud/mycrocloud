locals {
  project_id = "9dd69c49-ce80-477e-a92c-b4060039719a"
}

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
}

resource "bitwarden-secrets_secret" "api_secrets" {
  for_each   = toset(local.api_secrets)
  key        = "api/${each.value}"
  project_id = local.project_id
}


resource "bitwarden-secrets_secret" "dbmigrator_secrets" {
  for_each   = toset(local.dbmigrator_secrets)
  key        = "db-migrator/${each.value}"
  project_id = local.project_id
}

resource "bitwarden-secrets_secret" "lb_secrets" {
  for_each   = toset(local.lb_secrets)
  key        = "lb/${each.value}"
  project_id = local.project_id
}

resource "bitwarden-secrets_secret" "monitoring_alloy_secrets" {
  for_each   = toset(local.monitoring_alloy_secrets)
  key        = "alloy/${each.value}"
  project_id = local.project_id
}

resource "bitwarden-secrets_secret" "monitoring_prometheus_secrets" {
  for_each   = toset(local.monitoring_prometheus_secrets)
  key        = "prometheus/${each.value}"
  project_id = local.project_id
}

resource "bitwarden-secrets_secret" "webapp_gateway_secrets" {
  for_each   = toset(local.webapp_gateway_secrets)
  key        = "webapp-gateway/${each.value}"
  project_id = local.project_id
}

resource "bitwarden-secrets_secret" "webapp_spa_build_worker_secrets" {
  for_each   = toset(local.webapp_spa_build_worker_secrets)
  key        = "webapp-spa-build-worker/${each.value}"
  project_id = local.project_id
}
