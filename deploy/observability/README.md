# Stack local de observabilidad

Entorno de desarrollo para **Camilo.OrderFlow**. Configuración disponible no significa telemetría verificada: comprobar datos recibidos con el stack y la API en ejecución.

| Componente | Función | URL / puerto del host |
|---|---|---|
| OTel Collector | Recibe trazas y métricas OTLP | gRPC 4317, HTTP 4318; métricas internas 8888 y exportador 8889 |
| Tempo | Almacena trazas | http://localhost:3200 |
| Loki | Recibe logs directamente de Serilog si el sink está configurado | http://localhost:3100 |
| Prometheus | Consulta métricas del Collector | http://localhost:9090 |
| Grafana | Consulta Tempo, Loki y Prometheus | http://localhost:3000 |

## Arranque

Comprobar primero el daemon con `docker info`. Desde la raíz:

```powershell
./dev/observability.ps1 up
./dev/observability.ps1 status
./dev/observability.ps1 logs
./dev/observability.ps1 down
```

Alternativa: `docker compose -f deploy/observability/docker-compose.observability.yml up -d`. La primera ejecución descarga imágenes; registrar tiempos reales sin prometer arranque instantáneo.

La API exporta trazas/métricas cuando OpenTelemetry está habilitado con OtlpEndpoint http://localhost:4317 y protocolo grpc. Logs usan Serilog:WriteTo/GrafanaLoki del archivo local; revisar su plantilla `.example` sin imprimir secretos. El Collector actual tiene pipelines de trazas y métricas, no de logs.

El perfil Local+OTel de launchSettings.json habilita además spans en consola:

```powershell
dotnet run --project Camilo.OrderFlow.WebApi --launch-profile Local+OTel
```

## Comprobaciones de extremo a extremo

1. Levantar stack y API, generar solicitudes y abrir Grafana → Explore → Tempo.
2. Buscar el servicio Camilo.OrderFlow.WebApi o el traceId de una respuesta. Registrar recepción y demora observada; comprobar correlation_id frente al header X-Correlation-Id.
3. En Prometheus consultar `http_server_request_duration_seconds_count`, `dotnet_gc_collections_total` y métricas `otel_collector_*`.
4. En Loki consultar `{app="camilo-order-flow-webapi"}` y correlacionar con `{app="camilo-order-flow-webapi"} | json | traceId="<id>"`. El enlace de Tempo a logs depende de los derivedFields provisionados y de que el sink esté activo.
5. En una solicitud saliente verificar traceparent (OpenTelemetry) y X-Correlation-Id (CorrelationIdHandler). No almacenar tokens ni headers sensibles.

Recorrido real: API/OpenTelemetry → Collector → Tempo (trazas) y exportador Prometheus (métricas); API/Serilog → Loki (logs). Grafana consulta los almacenes. Ver [guía de observabilidad](../../docs/Observability.md).

## Persistencia y convivencia

| Volumen lógico | Contenido |
|---|---|
| tempo-data | Bloques de trazas y WAL |
| loki-data | Logs e índices |
| prometheus-data | Métricas TSDB |
| grafana-data | Dashboards y configuración |

El proyecto Compose es camilo-order-flow-observability: cambia el prefijo de esos volúmenes frente al proyecto de plantilla. Los volúmenes anteriores permanecen conservados; no se migran automáticamente. down/up conserva datos. No usar wipe ni down -v para validar esta fase.

La red observability y los nombres de contenedores tempo, loki, prometheus, grafana y otel-collector siguen compartidos; antes de otro stack comprobar colisiones sin interrumpir procesos del usuario.
