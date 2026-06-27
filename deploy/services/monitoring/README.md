# Monitoring

Collects **metrics** and **logs** from the host and all containers, then ships them to
**Grafana Cloud** (Prometheus + Loki). Nothing is stored/visualized locally — this stack
is just collection + forwarding. There are no traces (yet).

## Services

| Service         | Collects                          | Exposes / Reads        |
|-----------------|-----------------------------------|------------------------|
| `node-exporter` | Host metrics (CPU, mem, disk, net)| `:9100/metrics`        |
| `cadvisor`      | Per-container metrics             | `:8080/metrics`        |
| `prometheus`    | Scrapes the two above             | `remote_write` → Grafana Cloud |
| `alloy`         | Container logs via Docker socket  | `loki.write` → Grafana Cloud   |

All except `alloy` run with `network_mode: host` so Prometheus can scrape exporters on
`localhost`.

## Metrics path

```
node-exporter (:9100) ─┐
                        ├─ prometheus (scrape every 15s) ── remote_write ──> Grafana Cloud (Prometheus)
cadvisor      (:8080) ─┘
```

Prometheus keeps a short local TSDB (`prometheus_data` volume) only as a buffer; the
source of truth is Grafana Cloud. Targets are static (`prometheus.yml.j2`), not
service-discovered.

## Logs path

```
all containers ── Docker socket ──> alloy ──> Grafana Cloud (Loki)
```

`alloy` (`config.alloy.j2`) does the work:

1. **Discover** every container via `discovery.docker` (Docker socket).
2. **Relabel** — sets `service` (from container name, overridden by the
   `logging.service` label) and `component` (`logging.component` label). These become
   Loki labels. Ephemeral containers (`spa-builder`, `function-invoker`) are **dropped**.
3. **Tail** stdout/stderr with `loki.source.docker`.
4. **Process** (`loki.process`):
   - Drops noisy cAdvisor warnings about already-gone ephemeral containers.
   - For ASP.NET services (`api`, `db-migrator`, `gateway`), parses the JSON log line,
     maps `LogLevel` → Loki `level`, and rewrites the line to
     `<level> [<category>] <message>`.
5. **Ship** to Grafana Cloud Loki (`loki.write`).

So container labels (`logging.service` / `logging.component` in each `compose.yml`) are
what drive log labeling in Loki — set them on any new service you want to query cleanly.

## Config & secrets

Both configs are Jinja2 templates rendered at deploy time. The only secrets are the
Grafana Cloud credentials:

- `monitoring/alloy/grafana_cloud_api_key` — Loki push (logs)
- `monitoring/prometheus/grafana_api_key` — Prometheus remote_write (metrics)

Grafana Cloud URLs and usernames are non-secret and hardcoded in the templates. See the
repo `CLAUDE.md` for the add/remove-a-secret checklist.
