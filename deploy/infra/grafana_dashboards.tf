data "http" "dashboard_node_exporter" {
  url = "https://grafana.com/api/dashboards/1860/revisions/latest/download"
}

data "grafana_data_source" "prometheus" {
  provider = grafana.stack
  name     = "grafanacloud-${var.grafana_cloud_stack_slug}-prom"
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
