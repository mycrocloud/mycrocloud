# Grafana-managed alerting, provisioned as code (mirrors the dashboard provisioning
# in grafana_dashboards.tf). Rules live in the existing "MycroCloud" folder and notify
# via a dedicated Slack contact point using per-rule notification settings, so the global
# notification policy tree is left untouched.

resource "grafana_contact_point" "slack" {
  provider = grafana.stack
  name     = "mycrocloud-slack"

  slack {
    url   = var.grafana_alert_slack_webhook_url
    title = "{{ .CommonLabels.alertname }}"
    text  = <<-EOT
      {{ range .Alerts }}*{{ .Labels.service_name }}* — {{ .Annotations.summary }}
      {{ end }}
    EOT
  }
}

resource "grafana_rule_group" "http_errors" {
  provider         = grafana.stack
  name             = "mycrocloud-http-errors"
  folder_uid       = grafana_folder.mycrocloud.uid
  interval_seconds = 60

  # High 5xx error ratio on the control plane (api) or data plane (gateway).
  # Fires when >5% of requests return 5xx for a service, sustained for 5m.
  rule {
    name           = "High 5xx error rate"
    condition      = "C"
    for            = "5m"
    no_data_state  = "OK"
    exec_err_state = "Error"

    data {
      ref_id         = "A"
      datasource_uid = data.grafana_data_source.prometheus.uid
      relative_time_range {
        from = 600
        to   = 0
      }
      model = jsonencode({
        refId   = "A"
        instant = true
        range   = false
        expr    = "sum by (service_name) (rate(http_server_request_duration_seconds_count{http_response_status_code=~\"5..\"}[5m])) / sum by (service_name) (rate(http_server_request_duration_seconds_count[5m]))"
      })
    }

    data {
      ref_id         = "C"
      datasource_uid = "__expr__"
      relative_time_range {
        from = 0
        to   = 0
      }
      model = jsonencode({
        refId      = "C"
        type       = "threshold"
        expression = "A"
        conditions = [{
          evaluator = {
            type   = "gt"
            params = [0.05]
          }
        }]
      })
    }

    annotations = {
      summary = "{{ $labels.service_name }} 5xx error ratio is {{ $values.A.Value | humanizePercentage }} (>5% for 5m)."
    }

    labels = {
      severity = "warning"
    }

    notification_settings {
      contact_point = grafana_contact_point.slack.name
      group_by      = ["alertname", "service_name"]
    }
  }
}
