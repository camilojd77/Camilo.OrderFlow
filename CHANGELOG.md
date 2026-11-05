# Changelog

Historial de cambios relevantes de **Camilo.OrderFlow**, del más reciente al más antiguo. Empieza en el commit
`b3ad03a` (2026-11-05); lo anterior está en `git log`. Cada commit explica el porqué en su cuerpo. El estado actual del
proyecto vive en el [README](README.md), no aquí.

Formato de cada entrada: **Qué** cambió, qué es **Provisional** y las **Cifras** del día.

## 2026-11-05 — Seguimiento de la revisión del plan 01

- **Qué:**
  - [ADR-0021](docs/adr/0021-una-sola-rama-develop.md): una sola rama de larga vida, `develop` (se borró `main` del
    remoto). [ADR-0022](docs/adr/0022-clientes-http-de-modulo-en-infrastructure.md): los clientes HTTP de un módulo
    van en su `Infrastructure/Http`, detrás de un puerto.
  - `SystemClockUsageTests`: test de arquitectura que impide lecturas nuevas de `DateTime.UtcNow`/`Now` en producción
    ([ADR-0017](docs/adr/0017-timeprovider-en-codigo-nuevo.md)); hoy hay 20 permitidas en 8 archivos.
  - `OpenTelemetry:Environment` vale `dev` por defecto en `appsettings.json` (antes `production`); el
    ReleaseChecklist pide fijarlo en cada entorno.
  - `.gitattributes` con LF en el repositorio y CRLF/LF en disco según `.editorconfig`.
  - Comentarios corregidos en `CleanArchitectureTests.cs` (adaptador en el host) y `dev/rename.ps1` (ruta archivada).
- **Provisional:** el disparador de `main` sigue en `.github/workflows/ci.yml` (sin efecto); quitarlo es un cambio de
  pipeline pendiente.
- **Cifras:**

  | Comprobación | Resultado |
  |---|---|
  | Tests | 208/208: ArchitectureTests 22 (antes 20) · Identity.UnitTests 121 · WebApi.IntegrationTests 59 · WebApi.IntegrationTests.RealDb 6 |
  | Avisos (`-c Release -p:TreatWarningsAsErrors=true`) | 0 |
  | Paquetes vulnerables | 0 |
  | `dotnet format --verify-no-changes` | OK |
  | Cobertura | No se volvió a medir (sin cambios de código de producción salvo `appsettings.json`) |

## 2026-11-05 — Estructura para trabajar con Claude (plan 01)

- **Qué:**
  - 20 ADR en [`docs/adr/`](docs/adr/README.md): 14 decisiones que ya estaban en el código, la integración entre
    módulos (adaptador en el host) y cuatro reglas solo para código nuevo (`TimeProvider`, `[LoggerMessage]`, Minimal
    APIs con `TypedResults`, constructores primarios).
  - [`docs/architecture.md`](docs/architecture.md) y [`docs/observabilidad.md`](docs/observabilidad.md) como fuentes
    únicas; README reorganizado; [`CLAUDE.md`](CLAUDE.md) como manual operativo.
  - `.claude/`: 11 reglas por ruta, 12 skills, subagente `revisor-arquitectura` con hook de solo lectura
    (`dotnet run`), `settings.json` sin atribución y con denegaciones; `.mcp.json` con Microsoft Learn.
  - `.editorconfig`: `csharp_style_prefer_primary_constructors = true:suggestion`. `.gitignore`: `.claude/` versionado
    salvo `settings.local.json` y `CLAUDE.local.md`.
  - Guías antiguas (`01-Arquitectura`, `02-Instrucciones-IA`, `03-Guia-Nuevo-Equipo`, `04-Guia-Renombrado…`,
    `05-Plan-Definitivo`, `Standards`, `Observability`) movidas a `docs/archive/` con nota de reemplazo.
  - Sin cambios en código, proyectos, paquetes ni CI.
- **Provisional:**
  - Integración entre módulos y Minimal APIs: decididas, sin ejemplo en el código todavía. El primer endpoint Minimal
    API añadirá `Asp.Versioning.Http`.
  - Cobertura sin umbral (se propone uno con estas cifras). Sin hooks de git.
  - Ruta a microservicios: pendiente, sin criterios decididos.
- **Cifras** (`b3ad03a`, SDK 10.0.403, sin cambios de código en este plan):

  | Comprobación | Resultado |
  |---|---|
  | Tests | 206/206: ArchitectureTests 20 · Identity.UnitTests 121 · WebApi.IntegrationTests 59 · WebApi.IntegrationTests.RealDb 6 |
  | Avisos (`-c Release -p:TreatWarningsAsErrors=true`) | 0 en los 10 proyectos |
  | Paquetes vulnerables (`--include-transitive`) | 0 |
  | `dotnet format --verify-no-changes` | OK |
  | Cobertura de líneas, tests unitarios | Identity.Application 76,6 % · Identity.Domain 92,6 % · SharedKernel 70,8 % |
  | Cobertura de líneas, integración SQLite | WebApi 56,3 % · Identity.Infrastructure 23,3 % (incluye migraciones) · Identity.Application 98,8 % · Identity.Domain 87,4 % |
  | Cobertura de líneas, RealDb | Identity.Infrastructure 92,2 % · WebApi 45,1 % |

  La cobertura es por ejecución (no fusionada).
