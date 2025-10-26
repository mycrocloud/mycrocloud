locals {
  domain = "mycrocloud.info"
}

resource "auth0_client" "web" {
  name = "mycrocloud"
  app_type = "spa"
  callbacks = [ "https://${local.domain}" ]
  allowed_logout_urls = [ "https://${local.domain}" ]
  web_origins = [ "https://${local.domain}" ]
  
  //TODO: confirm what these do
  cross_origin_auth = true
  organization_require_behavior = "no_prompt"
}