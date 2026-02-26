output "domain" {
  value = var.domain
}

output "web_client_id" {
  value = auth0_client.web.client_id
}

output "api_identifier" {
  value = auth0_resource_server.api.identifier
}

output "build_worker_client_id" {
  value = auth0_client.build_worker.client_id
}