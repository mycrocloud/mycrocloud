module "auth0" {
  source                         = "./modules/auth0"
  web_origin                     = var.web_origin
  github_oauth_app_client_id     = var.auth0_github_oauth_app_client_id
  github_oauth_app_client_secret = var.auth0_github_oauth_app_client_secret
  google_oauth_app_client_id     = var.auth0_google_oauth_app_client_id
  google_oauth_app_client_secret = var.auth0_google_oauth_app_client_secret
}
