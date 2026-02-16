# =============================================================================
# AWS Secrets Manager - Secret resources only (values managed via AWS Console)
# =============================================================================

locals {
  secret_name_prefix = "prod/${local.project_name}"
}

# env_files (referenced in deploy.yml)
resource "aws_secretsmanager_secret" "api_env" {
  name = "${local.secret_name_prefix}/api/.env"
}

resource "aws_secretsmanager_secret" "dbmigrator_env" {
  name = "${local.secret_name_prefix}/dbmigrator/.env"
}

resource "aws_secretsmanager_secret" "gateway_env" {
  name = "${local.secret_name_prefix}/webapp/gateway/.env"
}

resource "aws_secretsmanager_secret" "spa_worker_env" {
  name = "${local.secret_name_prefix}/webapp/spa/worker/.env"
}

resource "aws_secretsmanager_secret" "seq_env" {
  name = "${local.secret_name_prefix}/monitoring/seq/.env"
}

# secret_files (referenced in deploy.yml)
resource "aws_secretsmanager_secret" "lb_certs_key" {
  name = "${local.secret_name_prefix}/lb/certs/mycrocloud.online.key"
}

resource "aws_secretsmanager_secret" "api_gha_pem" {
  name = "${local.secret_name_prefix}/api/gha-mycrocloud.private-key.pem"
}

resource "aws_secretsmanager_secret" "monitoring_prometheus_prometheus_yml" {
  name = "${local.secret_name_prefix}/monitoring/prometheus/prometheus.yml"
}
