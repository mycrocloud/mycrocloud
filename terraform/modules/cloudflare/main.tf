resource "cloudflare_zone" "zone" {
  count = var.cloudflare_zone_id == "" ? 1 : 0
  account = {
    id = var.cloudflare_account_id
  }
  name = var.domain
}

locals {
  cloudflare_zone_id = var.cloudflare_zone_id != "" ? var.cloudflare_zone_id : cloudflare_zone.zone[0].id
}

resource "cloudflare_dns_record" "root" {
  zone_id = local.cloudflare_zone_id
  name    = "@"
  type    = "CNAME"
  ttl     = 1
  content = var.ingress_hostname

  proxied = true
}