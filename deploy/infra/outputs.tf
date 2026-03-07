# Server
output "instance_ip" {
  value = local.server_ip
}

# DNS
output "control_plane_host" {
  value = data.cloudflare_zone.control_plane_zone.name
}

output "data_plane_host" {
  value = local.data_plane_domain
}

# Identity
output "auth0_domain" {
  value = var.auth0_domain
}

output "auth0_web_client_id" {
  value = module.auth0.web_client_id
}

output "auth0_api_identifier" {
  value = module.auth0.api_identifier
}

output "auth0_build_worker_client_id" {
  value = module.auth0.build_worker_client_id
}

