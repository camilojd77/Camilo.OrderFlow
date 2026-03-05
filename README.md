# Company.Product — Backend Template

Plantilla enterprise para APIs REST en **.NET 10** con Clean Architecture, lista para clonar y arrancar.

## Características incluidas

| Área | Detalle |
|---|---|
| Arquitectura | Clean Architecture (Domain / Application / Infrastructure / WebApi) con módulos |
| Auth | JWT Bearer + autorización basada en permisos |
| Observabilidad | OpenTelemetry → OTel Collector → Tempo + Loki + Prometheus + Grafana |
| Logging | Serilog estructurado con enrichment de traceId / spanId / correlationId |
| Resiliencia | `Microsoft.Extensions.Http.Resilience` — timeout + retry + circuit breaker |
| Rate limiting | Políticas `default` y `auth` configurables por appsettings |
| API versioning | Por header `api-version`, versión por defecto `1.0` |
| Health checks | `/health/live` y `/health/ready` (SQL Server) |
| Seguridad | Security headers configurables, CORS, HTTPS |
| Tests | Unit tests, Integration tests, **Architecture tests** (Clean Architecture no violable) |
| CI | GitHub Actions — restore → build → test → coverage |

## Arranque rápido

```bash
# 1. Clonar
git clone <url> && cd <repo>

# 2. Copiar configuración de desarrollo
cp Company.Product.WebApi/appsettings.Development.json.example \
   Company.Product.WebApi/appsettings.Development.json
# Editar la copia con tus credenciales locales

# 3. Stack de observabilidad (opcional)
docker compose -f docker/observability/docker-compose.yml up -d

# 4. Aplicar migraciones
dotnet ef database update \
  --project Company.Product.Modules.Identity.Infrastructure \
  --startup-project Company.Product.WebApi

# 5. Ejecutar
dotnet run --project Company.Product.WebApi
```

Swagger → `https://localhost:{port}/swagger`  
Info de servicio → `https://localhost:{port}/_info`

## Tests

```bash
dotnet test                                          # todos
dotnet test tests/Company.Product.ArchitectureTests  # solo arquitectura
dotnet test --collect:"XPlat Code Coverage"          # con coverage
```

## Documentación

| Documento | Contenido |
|---|---|
| [`docs/README.md`](docs/README.md) | Guía de arranque local detallada |
| [`docs/Standards.md`](docs/Standards.md) | Convenciones, capas, cómo agregar módulos |
| [`docs/Observability.md`](docs/Observability.md) | Grafana: trazas, métricas, logs |
| [`docs/ReleaseChecklist.md`](docs/ReleaseChecklist.md) | Checklist antes de deploy |

## Licencia

Uso interno — © Company.
