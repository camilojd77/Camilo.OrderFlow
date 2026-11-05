# ADR-0015 — Trazas y métricas por OpenTelemetry/OTLP; logs por Serilog a Loki

- **Estado:** Aceptada
- **Fecha:** 2026-11-05 · **Decide:** Camilo Jaramillo
- **Revisar cuando:** se quiera un único camino OTLP también para logs (exportador de logs de OpenTelemetry o Grafana
  Alloy), o cambie el backend de observabilidad.

## Contexto

La plantilla se observa con Grafana, Tempo, Prometheus y Loki. Ya estaba montada así: OpenTelemetry para trazas y
métricas y Serilog para logs. Alternativas: todo por OTLP (logs incluidos) hacia el Collector; Serilog con sink OTLP;
solo logs, sin trazas.

## Decisión

- **Trazas y métricas**: OpenTelemetry .NET con instrumentación de ASP.NET Core, `HttpClient`, runtime y (opcional)
  SqlClient; exportador OTLP (gRPC por defecto) → OpenTelemetry Collector → Tempo (trazas) y exportador Prometheus que
  Prometheus scrapea (métricas). Recurso: `service.name`, `service.version`, `deployment.environment`.
- **Logs**: Serilog (`UseSerilog`, configuración en `Serilog`) a consola y archivo, y a **Loki directo** con
  `Serilog.Sinks.Grafana.Loki` cuando la configuración lo declara. Enriquecidos con `traceId`/`spanId` y
  `CorrelationId` para saltar entre logs y trazas en Grafana.
- El SQL de los spans se elimina salvo que `DbTracingCaptureStatements=true`.
- Todo desactivable por configuración (`OpenTelemetry:Enabled`).

## Consecuencias

- Dos caminos de telemetría (OTLP y Loki) que hay que configurar por separado.
- La correlación log↔traza depende de las propiedades `traceId`/`spanId` y de los datasources provisionados.
- Las convenciones de nombres y el catálogo de señales están en `docs/observabilidad.md`.

## Referencias

`Camilo.OrderFlow.WebApi/Observability/**`, `Camilo.OrderFlow.WebApi/Program.cs` (`UseSerilog`),
`appsettings.Development.json.example`, `deploy/observability/**`.
