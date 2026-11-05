# ADR-0024 — Versiones fijadas: paquetes (CPM), SDK, imágenes y acciones del CI

- **Estado:** Aceptada
- **Fecha:** 2026-11-05 · **Decide:** Camilo Jaramillo
- **Revisar cuando:** se adopten archivos de bloqueo de NuGet (`packages.lock.json`) o un bot de actualizaciones
  (Dependabot, Renovate) que mantenga estas versiones.

## Contexto

Reemplaza a [ADR-0012](0012-versiones-centralizadas-y-pines-transitivos.md). Este centralizaba las versiones de
NuGet, pero dejaba flotar el SDK (`10.0.x` en CI, sin `sdk` en `global.json`), las imágenes base (`sdk:10.0`,
`aspnet:10.0`, `mssql/server:2022-latest`) y las acciones del CI (`@v4`, `@v1`). Un mismo commit podía compilar y
probarse con herramientas distintas según el día y la máquina, y una etiqueta movible de una acción puede cambiar de
código sin aviso.

Alternativas: seguir flotando (reproducibilidad baja) o fijar también por *digest* las imágenes (máxima
reproducibilidad, pero ilegible y sin parches de seguridad sin intervención).

## Decisión

- **NuGet**: Central Package Management; versiones solo en `Directory.Packages.props`, nunca `Version=` en un
  `.csproj`; `CentralPackageTransitivePinningEnabled` solo para cerrar vulnerabilidades, con el comentario «Llega por /
  Motivo / Revisar»; solo nuget.org (`nuget.config` con `<clear/>`).
- **SDK**: `global.json` fija `10.0.403` con `rollForward: latestPatch` (se aceptan parches de la banda 10.0.4xx). El CI
  lo instala leyendo `global.json`.
- **Imágenes**: etiquetas de versión exacta, nunca `latest` ni etiquetas de familia. `Dockerfile`: `sdk:10.0.403` (igual
  que `global.json`) y `aspnet:10.0.14` (el runtime de ese SDK). SQL Server: `2022-CU27-GDR1-ubuntu-22.04` en los compose
  y en el fixture de Testcontainers. Las de observabilidad ya estaban fijadas.
- **Acciones de GitHub**: por SHA completo de commit, con la versión en un comentario (`@<sha> # v4.4.0`).
- Cambiar cualquiera de estas versiones solo cuando se pide, con la skill `actualizar-dependencias`, y en un único
  cambio que actualice todos los sitios donde aparece.

## Consecuencias

- Mismo SDK, imágenes y acciones en todas las máquinas y en el CI; los cambios de versión quedan en el historial.
- Los parches de seguridad no llegan solos: hay que revisarlos (las asesorías de NuGetAudit siguen rompiendo el build,
  [ADR-0023](0023-avisos-como-errores-en-local-y-ci.md)).
- Quien no tenga un SDK 10.0.4xx ≥ 10.0.403 tiene que instalarlo.

## Referencias

`Directory.Packages.props`, `nuget.config`, `global.json`, `Dockerfile`, `deploy/app/docker-compose.yml`,
`deploy/database/docker-compose.sqlserver.yml`, `tests/Camilo.OrderFlow.WebApi.IntegrationTests.RealDb/SqlServerFixture.cs`,
`.github/workflows/ci.yml`, skill `actualizar-dependencias`.
