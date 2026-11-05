---
paths:
  - "Camilo.OrderFlow.WebApi/Observability/**"
  - "deploy/observability/**"
---

# Telemetría y stack de observabilidad

- Trazas y métricas solo por OpenTelemetry/OTLP; logs por Serilog ([ADR-0015](../../docs/adr/0015-telemetria-otel-y-serilog-loki.md)).
  No añadas otro exportador ni otro proveedor de logs sin ADR.
- Hoy no hay `ActivitySource` ni `Meter` propios. El primero de cada módulo se llama `Camilo.OrderFlow.<Modulo>`, se
  registra con `AddSource`/`AddMeter` en `OpenTelemetryExtensions.cs` y se añade al catálogo de
  `docs/observabilidad.md` §3.2 en el mismo cambio.
- Convenciones semánticas de OpenTelemetry; métricas con unidad; **sin IDs, correos ni rutas con parámetros como
  etiqueta de métrica**.
- **Sin datos personales ni secretos** en atributos, etiquetas ni logs.
- El texto SQL no sale en los spans salvo `DbTracingCaptureStatements=true`; no lo actives fuera de local.
- Atributos de recurso desde `OpenTelemetryOptions` (`service.name`, `service.version`, `deployment.environment`).
- `deploy/observability/`: es solo para desarrollo local (Grafana anónimo de solo lectura; `admin` para editar).
  Datasources en `grafana/provisioning/` y dashboards en `grafana/dashboards/` (provisionados, no editables desde la UI):
  se cambian en el repo y sus consultas se comprueban contra datos reales. `rate()` siempre con `$__rate_interval`. Mantén coherentes la etiqueta `app` de Loki, la consulta `tracesToLogsV2` y el
  `derivedFields` de `traceId`.
- Nunca `.\dev\observability.ps1 wipe` ni `docker compose … down -v` sin que te lo pidan: borra los datos.

Referencia: `Camilo.OrderFlow.WebApi/Observability/OpenTelemetry/OpenTelemetryExtensions.cs`,
`Observability/Processors/DbStatementRedactionProcessor.cs`,
`deploy/observability/grafana/provisioning/datasources/datasources.yml`.

Fuente: [docs/observabilidad.md](../../docs/observabilidad.md) §3-4 · [CLAUDE.md](../../CLAUDE.md) §4
