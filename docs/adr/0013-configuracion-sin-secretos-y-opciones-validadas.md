# ADR-0013 — Configuración sin secretos en el repo y opciones validadas al arrancar

- **Estado:** Aceptada
- **Fecha:** 2026-11-05 · **Decide:** Camilo Jaramillo
- **Revisar cuando:** se adopte un almacén de secretos (Key Vault u otro) o user-secrets en local.

## Contexto

La API necesita cadenas de conexión, clave JWT y destinos de telemetría distintos por entorno. Un secreto en un
archivo versionado queda en el historial para siempre. Alternativas para local: user-secrets (fuera del árbol del
repo) o variables de entorno; para entornos compartidos: variables de entorno o un almacén de secretos.

## Decisión

- `appsettings.json` versionado **sin secretos**: cadenas de conexión y `Jwt:Key` vacías.
- En local, `appsettings.Development.json` creado a partir de `appsettings.Development.json.example` y **ignorado** por
  git (también `appsettings.Local/Staging/Production.json` y `.env*`).
- En contenedores y entornos, variables de entorno (`ConnectionStrings__DefaultConnection`, `Jwt__Key`, …).
- Las secciones con requisitos se enlazan a clases `*Options` con `ValidateDataAnnotations().ValidateOnStart()`: la
  aplicación no arranca con configuración inválida.

## Consecuencias

- Ningún secreto real en git; cada persona crea su archivo local.
- Los valores de los compose de `deploy/` son de desarrollo local y no sirven fuera de él.
- Todavía hay lecturas directas de `IConfiguration` en el host y en `AddIdentityInfrastructure`; las nuevas secciones
  usan `IOptions` validadas. Nunca se lee configuración desde Domain o Application.

## Referencias

`Camilo.OrderFlow.WebApi/appsettings.json`, `appsettings.Development.json.example`, `.gitignore`,
`**/*Options.cs` (p. ej. `Observability/OpenTelemetry/OpenTelemetryOptions.cs`, `Identity.Infrastructure/Auth/JwtOptions.cs`),
`deploy/app/docker-compose.yml`.
