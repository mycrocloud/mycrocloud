locals {
  api_secrets = [
    "ConnectionStrings__DefaultConnection",
    "ConnectionStrings__PubSub",
    "ExternalIntegrations__GitHub__WebhookSecret",
    "ExternalIntegrations__Slack__ClientSecret",
    "ExternalIntegrations__Slack__SigningSecret",
    "ExternalIntegrations__Slack__LinkSecret",
    "Storage__S3__AccessKey",
    "Storage__S3__SecretKey",
    "gha-mycrocloud.private-key.pem",
  ]
}

resource "bitwarden-secrets_secret" "api_secrets" {
  for_each   = toset(local.api_secrets)
  key        = "api/${each.value}"
  project_id = "9dd69c49-ce80-477e-a92c-b4060039719a"
}

locals {
  dbmigrator_secrets = [
    "ConnectionStrings__DefaultConnection"
  ]
}

resource "bitwarden-secrets_secret" "dbmigrator_secrets" {
  for_each   = toset(local.dbmigrator_secrets)
  key        = "dbmigrator/${each.value}"
  project_id = "9dd69c49-ce80-477e-a92c-b4060039719a"
}