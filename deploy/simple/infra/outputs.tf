# Server
output "instance_ip" {
  value = aws_instance.server.public_ip
}

output "instance_user" {
  value = "ubuntu"
}

# DNS
output "host" {
  value = data.cloudflare_zone.zone.name
}

# Identity
output "auth0_domain" {
  value = module.auth0.domain
}

output "auth0_web_client_id" {
  value = module.auth0.web_client_id
}

output "auth0_api_identifier" {
  value = module.auth0.api_identifier
}
