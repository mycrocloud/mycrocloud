locals {
  project_name         = "mycrocloud"
  control_plane_domain = "mycrocloud.online"
  data_plane_domain    = "mycrocloud.site"
  server_ip            = [for addr in flatten(values(conohavps_instance.server.addresses)) : addr.addr if addr.version == 4][0]
}
