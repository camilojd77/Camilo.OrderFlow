---
name: diagnosticar
description: "Investiga un fallo o un comportamiento raro de Camilo.OrderFlow con las señales del stack local: trazas en Tempo, métricas en Prometheus y logs en Loki, saltando de un traceId o correlationId a sus logs. Solo lectura y solo contra el stack local salvo que se indique otro."
argument-hint: "<síntoma, traceId o correlationId>"
---

# Diagnosticar con las señales

**Solo lectura.** No cambies configuración de Grafana, no borres datos (`wipe`, `down -v`) y no consultes entornos
que no sean el stack local salvo que la persona lo diga explícitamente.

1. **¿Está el stack?** `.\dev\observability.ps1 status` (o `docker ps`). Si no, dilo y para.
2. **Punto de partida**:
   - Un error HTTP: el `ProblemDetails` trae `traceId` y `correlationId`.
   - Un `X-Correlation-Id`: búscalo en Loki (`{app="camilo-order-flow-webapi"} |= "<id>"`).
3. **Traza (Tempo)** en http://localhost:3200 o Grafana → Explore → Tempo: por `traceId`, o TraceQL
   `{ resource.service.name = "Camilo.OrderFlow.WebApi" && status = error }`. Mira qué span falla y su duración.
4. **Logs (Loki)**: `{app="camilo-order-flow-webapi"} | json | traceId="<traceId>"`. Si no hay nada, comprueba que el
   sink `GrafanaLoki` está configurado (runbook en `docs/observabilidad.md` §8).
5. **Métricas (Prometheus)** en http://localhost:9090: tasa de peticiones y errores
   (`http_server_request_duration_seconds_count`, filtrando `http_response_status_code`), latencia p99, GC y thread
   pool (`dotnet_*`). Las de la API llevan `exported_job="Camilo.OrderFlow.WebApi"`.
6. **Hipótesis y evidencia**: resume qué viste (consulta + resultado), la causa probable y qué cambio la confirmaría.
   No apliques el arreglo sin que te lo pidan.

Las consultas útiles y los runbooks están en [`docs/observabilidad.md`](../../../docs/observabilidad.md) §6 y §8.
