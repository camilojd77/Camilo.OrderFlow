# Observabilidad — Guía rápida

La plantilla exporta **trazas, métricas y logs** a un OTel Collector local, que los reenvía a Tempo, Prometheus y Loki respectivamente. Todo se visualiza desde Grafana.

---

## Levantar el stack local

```pwsh
.\dev\observability.ps1 up       # camino recomendado
```

O directamente con compose:

```pwsh
docker compose -f deploy/observability/docker-compose.observability.yml up -d
```

| Componente | Rol | URL / puerto |
|---|---|---|
| Grafana | Dashboards y Explore | `http://localhost:3000` |
| OTel Collector | Recibe OTLP de la API | gRPC `4317` · HTTP `4318` |
| Tempo | Almacena trazas | `http://localhost:3200` |
| Loki | Almacena logs | `http://localhost:3100` |
| Prometheus | Almacena métricas | `http://localhost:9090` |

> Otros comandos del wrapper: `down`, `restart`, `logs`, `status`, `wipe` (este último borra los volúmenes).
> Detalle de cada componente en [`../deploy/observability/README.md`](../deploy/observability/README.md).

---

## Ver una traza en Grafana

1. Abrir `http://localhost:3000` → **Explore** → datasource **Tempo**.
2. Pegar el `traceId` que aparece en cualquier respuesta de error (campo `traceId` del ProblemDetails)  
   o buscarlo en los logs de la API.
3. Tempo muestra el span completo: latencia, DB queries redactadas, errores.

> **Trace → Logs**: en el panel de Tempo, el link "Logs for this span" abre Loki filtrado por `traceId` automáticamente gracias a los `derivedFields` configurados.

---

## Ver métricas en Grafana

1. **Explore** → datasource **Prometheus**.
2. Queries útiles de arranque:

| Qué | Query |
|---|---|
| Requests/s | `rate(http_server_request_duration_seconds_count[1m])` |
| Latencia p99 | `histogram_quantile(0.99, rate(http_server_request_duration_seconds_bucket[5m]))` |
| Errores 5xx | `rate(http_server_request_duration_seconds_count{http_response_status_code=~"5.."}[1m])` |
| GC Gen0 | `dotnet_gc_collections_total{generation="gen0"}` |
| Thread pool queue | `dotnet_thread_pool_queue_length` |

---

## Ver logs en Grafana

Los logs **no pasan por el OTel Collector**: Serilog los envía directamente a Loki con el sink
`GrafanaLoki` configurado en `appsettings.Development.json`. Las etiquetas disponibles son las que
declara ese sink — `app` y `env` — no `service_name`.

1. **Explore** → datasource **Loki**.
2. Query básica: `{app="company-product-webapi"}`.
3. Filtrar errores: `{app="company-product-webapi"} |= "Error"`.
4. Correlacionar con traza: `{app="company-product-webapi"} | json | traceId="<id>"`.
5. Filtrar por entorno: `{app="company-product-webapi", env="dev"}`.

> Si renombraste la plantilla, la etiqueta `app` cambió con ella: revisa el bloque `Serilog:WriteTo`
> de tu `appsettings.Development.json`.

---

## Configuración clave en `appsettings.json`

```json
"OpenTelemetry": {
  "Enabled": true,
  "OtlpEndpoint": "http://localhost:4317",
  "ExporterProtocol": "grpc",
  "EnableSqlClientInstrumentation": true,
  "DbTracingCaptureStatements": false   // true solo en Development — captura SQL
}
```

> `DbTracingCaptureStatements: true` activa la captura de `db.statement`. El `DbStatementRedactionProcessor` redacta valores literales antes de exportar.
