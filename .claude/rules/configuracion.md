---
paths:
  - "**/appsettings*.json"
  - "**/appsettings*.json.example"
  - "**/*Options.cs"
  - "**/launchSettings.json"
  - "Dockerfile"
  - "deploy/app/**"
  - "deploy/database/**"
---

# Configuración

- **Ningún secreto ni valor real de entorno** en archivos versionados: ni cadenas de conexión reales, ni claves, ni
  tokens, ni URLs de entornos. En ejemplos, marcadores (`<connection-string>`).
- `appsettings.json` lleva valores por defecto seguros y campos sensibles vacíos. `appsettings.Development.json` es
  local e ignorado: no lo crees, leas en voz alta ni lo añadas a git; su plantilla es el `.example`.
- Si añades una clave de configuración: añádela también al `.example` (sin valor real) y, si es obligatoria, valida
  con `*Options` + `ValidateDataAnnotations().ValidateOnStart()`.
- Nunca se lee `IConfiguration` desde Domain ni Application.
- `ConnectionStrings:DefaultConnection` (EF) y `ConnectionStrings:Db` (health check) deben ir siempre juntas.
- Los valores de `deploy/app` y `deploy/database` son solo de desarrollo local; no los reutilices en otro sitio.
- `Dockerfile`: la imagen final corre sin privilegios y con `HEALTHCHECK`; el target `migrator` es el que migra.

Referencia: `Camilo.OrderFlow.WebApi/appsettings.json`, `appsettings.Development.json.example`,
`Observability/OpenTelemetry/OpenTelemetryOptions.cs`.

Fuente: [ADR-0013](../../docs/adr/0013-configuracion-sin-secretos-y-opciones-validadas.md) ·
[ADR-0011](../../docs/adr/0011-migraciones-fuera-del-arranque.md) · [CLAUDE.md](../../CLAUDE.md) §8
