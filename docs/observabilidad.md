# Observabilidad

Fuente única de la observabilidad de **Camilo.OrderFlow**: cómo fluyen las señales, qué se emite, las convenciones
para lo nuevo y cómo diagnosticar con el stack local. La decisión de fondo está en
[ADR-0015](adr/0015-telemetria-otel-y-serilog-loki.md); la de logs nuevos, en
[ADR-0018](adr/0018-loggermessage-para-logs-nuevos.md). El detalle de contenedores, puertos y volúmenes del stack está
en [`deploy/observability/README.md`](../deploy/observability/README.md). Para aprender los conceptos desde cero:
[`EntenderObservabilidad.md`](EntenderObservabilidad.md) (formación; no fija convenciones).

## 1. Flujo de señales

```
API (OpenTelemetry .NET) ──OTLP gRPC :4317──► OTel Collector ──OTLP──► Tempo          (trazas)
                                                     └──► exportador Prometheus :8889 ◄── Prometheus (scrape)  (métricas)
API (Serilog) ──────────── HTTP :3100 ─────────────────────────────► Loki            (logs, si el sink está configurado)
                                                     Grafana ◄── Tempo · Prometheus · Loki
```

- Trazas y métricas: `Camilo.OrderFlow.WebApi/Observability/OpenTelemetry/OpenTelemetryExtensions.cs`.
- Logs: `UseSerilog` en `Camilo.OrderFlow.WebApi/Program.cs`; el sink de Loki se declara en la configuración
  (`Serilog:WriteTo`, ver `appsettings.Development.json.example`). Los logs **no** pasan por el Collector.
- Todo se apaga con `OpenTelemetry:Enabled=false` (valor de `appsettings.json`; el `.example` y el perfil `Local+OTel`
  lo encienden).

## 2. Atributos de recurso

| Atributo | Origen | Valor por defecto |
|---|---|---|
| `service.name` | `OpenTelemetry:ServiceName` | `Camilo.OrderFlow.WebApi` |
| `service.version` | `OpenTelemetry:ServiceVersion` | `1.0.0` |
| `deployment.environment` | `OpenTelemetry:Environment` | `dev` (código y `appsettings.json`); cada entorno compartido lo fija por variable (`OpenTelemetry__Environment`), ver [`ReleaseChecklist.md`](ReleaseChecklist.md) |

Muestreo: `ParentBased(TraceIdRatio(OpenTelemetry:SampleRatio))`, 1.0 por defecto.

## 3. Catálogo de señales

### 3.1 Instrumentación automática (lo único que se emite hoy)

| Señal | Fuente | Notas |
|---|---|---|
| Spans de servidor HTTP | `AddAspNetCoreInstrumentation` | Etiqueta `correlation_id` añadida desde `CorrelationIdMiddleware` |
| Spans de cliente HTTP | `AddHttpClientInstrumentation` | Propaga `traceparent`; `CorrelationIdHandler` añade `X-Correlation-Id` si se registra en el cliente |
| Spans de SQL | `AddSqlClientInstrumentation` | Solo con `EnableSqlClientInstrumentation=true`; el texto SQL se **elimina** del span salvo `DbTracingCaptureStatements=true` (`DbStatementRedactionProcessor`) |
| Métricas HTTP servidor/cliente | ASP.NET Core / HttpClient | `http_server_request_duration_seconds_*`, `http_client_request_duration_seconds_*` |
| Métricas de runtime | `AddRuntimeInstrumentation` | `dotnet_gc_*`, `dotnet_thread_pool_*`, … |
| Log de peticiones | `UseSerilogRequestLogging` | Plantilla con `CorrelationId` y `TraceId`; `/health`, `/swagger` y `/_info` bajan a `Verbose` |

### 3.2 Señales propias

| Tipo | Nombre | Unidad / etiquetas | Dónde se crea |
|---|---|---|---|
| `ActivitySource` | — (ninguno aún) | — | — |
| `Meter` | — (ninguno aún) | — | — |
| Logs con `[LoggerMessage]` | — (ninguno aún) | — | — |

**Nombres reservados:** el `ActivitySource` y el `Meter` de cada módulo se llaman `Camilo.OrderFlow.<Modulo>`
(p. ej. `Camilo.OrderFlow.Identity`); los del host, `Camilo.OrderFlow.WebApi`. Quien cree el primero lo registra con
`AddSource`/`AddMeter` en `OpenTelemetryExtensions.cs` y añade la fila aquí, en el mismo PR.

## 4. Convenciones para todo cambio

1. **Un `ActivitySource` y un `Meter` por módulo**, con el nombre reservado de §3.2. Ningún nombre nuevo sin fila en el
   catálogo.
2. **Convenciones semánticas de OpenTelemetry** para nombres de spans, atributos y métricas. Métricas con unidad
   declarada (`unit:`), nombres en minúsculas con puntos (`orderflow.orders.created`), sin inventar sufijos.
3. **Cardinalidad**: nunca IDs de usuario o entidad, correos ni rutas con parámetros como etiqueta de métrica. Como
   atributo de span, un ID técnico sí, si no es dato personal.
4. **Sin datos personales ni secretos** en logs, atributos de span ni etiquetas: ni contraseñas, tokens, cabecera
   `Authorization`, cookies, correos, teléfonos ni documentos. Si hace falta identificar, ID interno.
5. **Logs nuevos con `[LoggerMessage]`** sobre `ILogger<T>`, plantilla con propiedades en PascalCase, nunca
   interpolación. `Warning` = situación anómala que el sistema maneja; `Error` = la operación falló y alguien tiene que
   mirarlo. Las excepciones esperadas de dominio (404, 409, 400) no se registran como `Error`.
6. **Propagación de contexto** en toda llamada saliente: `HttpClient` del contenedor de DI (instrumentado) y, si el
   destino lo usa, `CorrelationIdHandler`.
7. **Un flujo de negocio nuevo dice qué lo observa** (span, métrica o log) en su PR.
8. **Configuración del stack como código**: lo que se cambie en Grafana (datasources, y en el futuro dashboards o
   alertas) se exporta a `deploy/observability/`; nada creado solo a mano.

## 5. Stack local

```pwsh
.\dev\observability.ps1 up        # también: status, logs, down, restart; wipe BORRA volúmenes
```

| Componente | URL / puerto |
|---|---|
| Grafana | http://localhost:3000 (acceso anónimo como Admin: solo para desarrollo local) |
| OTel Collector | OTLP gRPC 4317 · HTTP 4318 · métricas propias 8888 · exportador 8889 |
| Tempo | http://localhost:3200 |
| Loki | http://localhost:3100 |
| Prometheus | http://localhost:9090 |

Para que la API envíe: `OpenTelemetry:Enabled=true`, `OpenTelemetry:OtlpEndpoint=http://localhost:4317` y el sink
`GrafanaLoki` en `Serilog:WriteTo` (ya vienen así en `appsettings.Development.json.example`), o arrancar con
`dotnet run --project Camilo.OrderFlow.WebApi --launch-profile Local+OTel`. Desde el contenedor de
`deploy/app`, hay que unir la API a la red `observability` y usar `http://otel-collector:4317` (instrucciones
comentadas en ese compose).

Retención local: Tempo 1 h de bloques; Loki rechaza líneas de más de 7 días; Prometheus por defecto.

## 6. Correlación y consultas

| Quiero | Cómo |
|---|---|
| De un error HTTP a su traza | El `ProblemDetails` trae `traceId` → Grafana → Explore → Tempo → pegar el ID |
| De una traza a sus logs | En Tempo, «Logs for this span» (consulta `{app="camilo-order-flow-webapi"} \| json \| traceId="<id>"`) |
| De un log a su traza | En Loki, el campo derivado `traceId` enlaza a Tempo |
| Por correlación de negocio | Cabecera `X-Correlation-Id` = propiedad `CorrelationId` en logs = atributo `correlation_id` en el span |
| Peticiones por segundo | `rate(http_server_request_duration_seconds_count[1m])` |
| Latencia p99 | `histogram_quantile(0.99, rate(http_server_request_duration_seconds_bucket[5m]))` |
| Errores 5xx | `rate(http_server_request_duration_seconds_count{http_response_status_code=~"5.."}[1m])` |
| Logs de error | `{app="camilo-order-flow-webapi"} \|= "Error"` |

En Prometheus las métricas de la API llevan `exported_job="Camilo.OrderFlow.WebApi"` (las reetiqueta el exportador del
Collector). En Loki las etiquetas son las que declara el sink (`app`, `env`), no `service_name`.

## 7. Dashboards y alertas

**No hay** dashboards ni reglas de alerta como código; solo los datasources
(`deploy/observability/grafana/provisioning/datasources/datasources.yml`). Añadirlos es una propuesta abierta; si se
hace, se provisionan desde `deploy/observability/grafana/provisioning/`.

## 8. Runbooks breves

| Síntoma | Comprobar |
|---|---|
| No llegan trazas | `OpenTelemetry:Enabled` y `OtlpEndpoint`; `.\dev\observability.ps1 status`; métricas `otelcol_receiver_accepted_spans*` del job `otel-collector-internal` en Prometheus |
| No llegan logs a Loki | Bloque `Serilog:WriteTo` con `GrafanaLoki` y `uri` correcta; Loki arriba en :3100 |
| El enlace traza → logs no encuentra nada | Que el log tenga la propiedad `traceId` (enriquecedor `WithSpan`) y que la etiqueta `app` coincida con la consulta del datasource |
| Spans de SQL sin texto | Es lo esperado: `DbTracingCaptureStatements=false` |
| Trazas incompletas | `SampleRatio` < 1.0, o un servicio intermedio que no propaga `traceparent` |
| `/health/ready` 503 | SQL Server inaccesible o `ConnectionStrings:Db` vacía |
