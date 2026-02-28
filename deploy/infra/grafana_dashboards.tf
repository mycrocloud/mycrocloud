data "http" "dashboard_node_exporter" {
  url = "https://grafana.com/api/dashboards/1860/revisions/latest/download"
}

data "http" "dashboard_nginx" {
  url = "https://grafana.com/api/dashboards/9614/revisions/latest/download"
}

data "http" "dashboard_docker" {
  url = "https://grafana.com/api/dashboards/193/revisions/latest/download"
}

resource "grafana_folder" "mycrocloud" {
  provider = grafana.stack
  title    = "MycroCloud"
}

resource "grafana_dashboard" "node_exporter" {
  provider    = grafana.stack
  folder      = grafana_folder.mycrocloud.uid
  config_json = data.http.dashboard_node_exporter.response_body
}

resource "grafana_dashboard" "nginx" {
  provider    = grafana.stack
  folder      = grafana_folder.mycrocloud.uid
  config_json = data.http.dashboard_nginx.response_body
}

resource "grafana_dashboard" "docker" {
  provider    = grafana.stack
  folder      = grafana_folder.mycrocloud.uid
  config_json = data.http.dashboard_docker.response_body
}
