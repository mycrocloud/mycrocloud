output "domain" {
  value = var.domain
}

output "web_client_id" {
  value = auth0_client.web.client_id
}

output "api_identifier" {
  value = auth0_resource_server.api.identifier
}