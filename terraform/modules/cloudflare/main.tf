resource "cloudflare_zone" "domain" {
  account = {
    id = var.cloudflare_account_id
  }
  name = var.domain
}

resource "cloudflare_dns_record" "root" {
  zone_id = var.cloudflare_zone_id
  name    = "@"
  type    = "CNAME"
  ttl     = 1
  content = var.ingress_hostname

  proxied = true
}