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
