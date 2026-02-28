data "http" "dashboard_node_exporter" {
  url = "https://grafana.com/api/dashboards/1860/revisions/latest/download"
}

data "grafana_data_source" "prometheus" {
  provider = grafana.stack
  name     = "grafanacloud-${var.grafana_cloud_stack_slug}-prom"
}

data "grafana_data_source" "loki" {
  provider = grafana.stack
  name     = "grafanacloud-${var.grafana_cloud_stack_slug}-logs"
}

resource "grafana_folder" "mycrocloud" {
  provider = grafana.stack
  title    = "MycroCloud"
}

resource "grafana_dashboard" "node_exporter" {
  provider = grafana.stack
  folder   = grafana_folder.mycrocloud.uid
  config_json = replace(
    replace(
      data.http.dashboard_node_exporter.response_body,
      "$${DS_PROMETHEUS}",
      data.grafana_data_source.prometheus.uid
    ),
    "$${ds_prometheus}",
    data.grafana_data_source.prometheus.uid
  )
}

resource "grafana_dashboard" "api_logs" {
  provider = grafana.stack
  folder   = grafana_folder.mycrocloud.uid
  config_json = replace(
    file("${path.module}/dashboards/api-logs.json"),
    "$${DS_LOKI}",
    data.grafana_data_source.loki.uid
  )
}
