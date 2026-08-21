# Local Observability Stack

Provides a zero-infrastructure-cost observability environment for local development:

| Component | Role | Host URL |
|---|---|---|
| **OTel Collector** | Receives OTLP from the API | grpc `localhost:4317` · http `localhost:4318` |
| **Grafana Tempo** | Distributed trace storage | `http://localhost:3200` |
| **Grafana Loki** | Log storage (Serilog writes here directly) | `http://localhost:3100` |
| **Prometheus** | Metrics storage | `http://localhost:9090` |
| **Grafana** | Dashboards (Tempo + Loki + Prometheus) | `http://localhost:3000` |

---

## Quick start

```powershell
# From the solution root
./dev/observability.ps1 up          # start all containers
./dev/observability.ps1 down        # stop all containers
./dev/observability.ps1 logs        # stream combined logs
./dev/observability.ps1 status      # show container state

# Or use docker compose directly
docker compose -f deploy/observability/docker-compose.observability.yml up -d
```

> First run pulls ~600 MB of images. Subsequent starts are instant.

---

## Wire the API

### `appsettings.json` (or `appsettings.Development.json`)

```json
"OpenTelemetry": {
  "Enabled": true,
  "OtlpEndpoint": "http://localhost:4317",
  "ExporterProtocol": "grpc"
}
```

### Launch profile `Local+OTel` (`Properties/launchSettings.json`)

The profile is pre-configured in `launchSettings.json`. Select it in Visual Studio / Rider or:

```powershell
dotnet run --launch-profile "Local+OTel"
```

It additionally sets `EnableConsoleExporter=true` so spans are printed to the terminal.

---

## Verify traces end-to-end

1. Start the stack: `./dev/observability.ps1 up`
2. Start the API with the `Local+OTel` launch profile.
3. Hit any endpoint (e.g. `GET /health/live`).
4. Open **Grafana** → **Explore** → select **Tempo** datasource.
5. Run a **Search** query – the span should appear within ~2 s.
6. Confirm the span has a `correlation_id` tag matching the `X-Correlation-Id` response header.

## Verify metrics end-to-end

1. Open **Grafana** → **Explore** → select **Prometheus** datasource.
2. Query `http_server_request_duration_seconds_count` (ASP.NET Core requests).
3. Query `dotnet_gc_collections_total` (runtime GC metrics).
4. Check `otel_collector_*` metrics to confirm the collector pipeline is healthy.

## Verify logs end-to-end

1. Open **Grafana** → **Explore** → select **Loki** datasource.
2. Query `{app="company-product-webapi"}` — Serilog pushes here via the `GrafanaLoki` sink
   configured in `appsettings.Development.json` (see the `.example` file).
3. Correlate with a trace: `{app="company-product-webapi"} | json | traceId="<id>"`.
4. From a Tempo span, the **Logs for this span** link jumps straight to the matching logs
   thanks to the `derivedFields` in the provisioned datasources.

## Verify outbound HttpClient propagation

Inspect any outbound request made by the API:
- `traceparent` header — W3C trace context injected by OTel `AddHttpClientInstrumentation`.
- `X-Correlation-Id` header — business correlation ID injected by `CorrelationIdHandler`.

Both headers should be present simultaneously.

---

## Architecture

```
┌──────────────────────┐
│    .NET Web API       │
│  OTLP gRPC :4317     │
└──────────┬───────────┘
           │ OTLP (traces + metrics)
           ▼
┌──────────────────────┐        ┌─────────────────────┐
│   OTel Collector     │─traces▶│   Grafana Tempo     │
│  :4317  :4318  :8889 │        │       :3200         │
└──────────┬───────────┘        └──────────┬──────────┘
           │ Prometheus scrape              │
           ▼                               │
┌──────────────────────┐                   │
│     Prometheus       │                   │
│       :9090          │                   │
└──────────┬───────────┘                   │
           │                               │
           ▼                               ▼
┌──────────────────────────────────────────────────────┐
│                    Grafana :3000                      │
│  Explore → Prometheus (metrics)                      │
│  Explore → Tempo     (traces + service map)          │
└──────────────────────────────────────────────────────┘
```

---

## Data persistence

Named Docker volumes keep data between `down`/`up` cycles:

| Volume | Contains |
|---|---|
| `tempo-data` | Trace blocks + WAL |
| `loki-data` | Log chunks + index |
| `prometheus-data` | TSDB metrics |
| `grafana-data` | Dashboards, user config |

To wipe everything and start fresh:

```powershell
docker compose -f deploy/observability/docker-compose.observability.yml down -v
```
