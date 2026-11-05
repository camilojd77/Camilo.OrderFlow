# ADR-0026 — Estrategia de pruebas con umbral de cobertura por proyecto

- **Estado:** Aceptada
- **Fecha:** 2026-11-05 · **Decide:** Camilo Jaramillo
- **Revisar cuando:** la suite rápida deje de ser rápida, SQLite no reproduzca un comportamiento de SQL Server que
  importe, o un umbral impida un cambio razonable (se discute en un ADR, no se baja en silencio).

## Contexto

Reemplaza a [ADR-0016](0016-estrategia-de-pruebas.md), que fijaba la estrategia de pruebas sin umbral de cobertura:
se publicaba en el CI y nadie la miraba. La herramienta del repo (`Microsoft.Testing.Extensions.CodeCoverage`) genera
un informe Cobertura por proyecto de tests, pero no aplica umbrales, y cada informe por separado da cifras engañosas
(Infrastructure aparecía al 23 % con la suite SQLite y al 92 % con RealDb).

Alternativas de umbral: uno global (un proyecto puede caer si otro compensa) o ninguno (la cobertura solo baja).
Alternativas de herramienta: ReportGenerator o Coverlet con umbral (herramientas nuevas que añadir y mantener).

## Decisión

Estrategia de pruebas (sin cambios respecto a ADR-0016):

- xUnit v3 sobre Microsoft.Testing.Platform; Shouldly; NSubstitute solo para puertos.
- Unitarios, arquitectura (NetArchTest), integración con `WebApplicationFactory` + SQLite, e integración RealDb con
  Testcontainers (`[Trait("Category", "RealDb")]`).
- Suite rápida sin Docker: `-- --filter-not-trait "Category=RealDb" --ignore-exit-code 8`. El CI ejecuta todo.

Umbral de cobertura (nuevo):

- **Cobertura de líneas por proyecto de producción**, fusionando los informes de todas las ejecuciones (una línea
  cuenta si la cubre cualquier suite) y excluyendo código generado (migraciones de EF, `obj/`).
- Mínimos en `eng/coverage-thresholds.json`: la línea base del 2026-11-05 redondeada hacia abajo a múltiplo de 5
  (BuildingBlocks.Infrastructure 100, SharedKernel 75, Identity.Application 100, Identity.Domain 95,
  Identity.Infrastructure 90, WebApi 70) y **85 % en total**.
- **Trinquete**: los mínimos solo suben. Todo proyecto de producción necesita su entrada; uno nuevo (un módulo) sin
  mínimo hace fallar la comprobación.
- La aplica `eng/coverage-gate.cs` (app C# de un archivo, `dotnet run`, sin paquetes) en el CI tras los tests y en
  local con la skill `verificar-cambio` cuando se ejecuta la suite completa con Docker.

## Consecuencias

- Un cambio que deja código sin probar en un proyecto por debajo de su mínimo rompe el CI.
- Application y BuildingBlocks.Infrastructure están al 100 %: cualquier línea nueva sin cubrir ahí falla.
- La comprobación solo es fiable con la suite completa: sin RealDb, Infrastructure y WebApi quedan por debajo.
- Al subir la cobertura de un proyecto, se puede (y conviene) subir su mínimo en el mismo cambio.

## Referencias

`eng/coverage-gate.cs`, `eng/coverage-thresholds.json`, `.github/workflows/ci.yml`, `global.json`, `tests/**`,
skill `verificar-cambio`.
