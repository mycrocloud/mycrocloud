data "cloudflare_zone" "control_plane_zone" {
  zone_id = var.cloudflare_control_plane_zone_id
}

data "cloudflare_zone" "data_plane_zone" {
  count   = var.cloudflare_data_plane_zone_id != "" ? 1 : 0
  zone_id = var.cloudflare_data_plane_zone_id
}

# Control plane DNS (mycrocloud.online)
resource "cloudflare_dns_record" "apex" {
  zone_id = data.cloudflare_zone.control_plane_zone.zone_id
  name    = "@"
  type    = "A"
  ttl     = 1
  content = local.server_ip
  proxied = true
}

resource "cloudflare_dns_record" "wildcard" {
  zone_id = data.cloudflare_zone.control_plane_zone.zone_id
  name    = "*"
  type    = "CNAME"
  ttl     = 1
  proxied = true
  content = local.control_plane_domain
}

resource "cloudflare_dns_record" "api" {
  zone_id = data.cloudflare_zone.control_plane_zone.zone_id
  name    = "api"
  type    = "CNAME"
  ttl     = 1
  proxied = true
  content = local.control_plane_domain
}

resource "cloudflare_dns_record" "slack_integration_api" {
  zone_id = data.cloudflare_zone.control_plane_zone.zone_id
  name    = "slack-integration-api"
  type    = "CNAME"
  ttl     = 1
  proxied = true
  content = local.control_plane_domain
}

# Data plane DNS (mycrocloud.site)
resource "cloudflare_dns_record" "data_plane_apex" {
  count   = var.cloudflare_data_plane_zone_id != "" ? 1 : 0
  zone_id = data.cloudflare_zone.data_plane_zone[0].zone_id
  name    = "@"
  type    = "A"
  ttl     = 1
  proxied = true
  content = local.server_ip
}

resource "cloudflare_dns_record" "data_plane_wildcard" {
  count   = var.cloudflare_data_plane_zone_id != "" ? 1 : 0
  zone_id = data.cloudflare_zone.data_plane_zone[0].zone_id
  name    = "*"
  type    = "CNAME"
  ttl     = 1
  proxied = true
  content = local.data_plane_domain
}
