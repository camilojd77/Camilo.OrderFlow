# ADR-0016 — Pruebas con xUnit v3 sobre Microsoft.Testing.Platform, Shouldly, NSubstitute, SQLite y Testcontainers

- **Estado:** Reemplazada por [ADR-0026](0026-umbral-de-cobertura-por-proyecto.md)
- **Fecha:** 2026-11-05 · **Decide:** Camilo Jaramillo
- **Revisar cuando:** la suite rápida deje de ser rápida o SQLite no reproduzca un comportamiento de SQL Server que
  importe.

## Contexto

Hacían falta pruebas rápidas sin Docker y, a la vez, fidelidad con SQL Server real. EF Core InMemory está
desaconsejado por Microsoft y se retiró (D6 del plan de construcción). La suite migró de xUnit v2/VSTest a xUnit v3.

## Decisión

- **xUnit v3** sobre **Microsoft.Testing.Platform** (runner declarado en `global.json`); cada proyecto de test es un
  ejecutable. Argumentos del runner después de `--`.
- **Shouldly** para aserciones; **NSubstitute** para dobles de los puertos en tests unitarios.
- Cuatro proyectos: unitarios del módulo (Domain + handlers), arquitectura (NetArchTest), integración con
  `WebApplicationFactory` + **SQLite** en memoria, e integración **RealDb** con **Testcontainers** (SQL Server) marcada
  `[Trait("Category", "RealDb")]`.
- Suite rápida sin Docker: `-- --filter-not-trait "Category=RealDb" --ignore-exit-code 8`. El CI ejecuta todo.
- Cobertura con `Microsoft.Testing.Extensions.CodeCoverage`; **sin umbral** por ahora (se propone uno a partir de la
  línea base del `CHANGELOG.md`).

## Consecuencias

- La suite rápida no valida SQL Server; un cambio de persistencia se verifica también con RealDb (necesita Docker).
- Métodos con `CancellationToken` reciben `TestContext.Current.CancellationToken` (analizador `xUnit1051`, avisos como
  errores en CI).

## Referencias

`global.json`, `tests/**`, `Directory.Packages.props` (grupo Testing), `.github/workflows/ci.yml`.
