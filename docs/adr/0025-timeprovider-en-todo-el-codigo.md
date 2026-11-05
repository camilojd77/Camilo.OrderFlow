# ADR-0025 — `TimeProvider` en todo el código de producción; sin `IDateTimeProvider`

- **Estado:** Aceptada
- **Fecha:** 2026-11-05 · **Decide:** Camilo Jaramillo
- **Revisar cuando:** haga falta un reloj con capacidades que `TimeProvider` no tiene, o una excepción justificada a
  la regla (que se registraría como entrada en `SystemClockUsageTests.KnownUsages` con su ADR).

## Contexto

Reemplaza a [ADR-0017](0017-timeprovider-en-codigo-nuevo.md). Este pedía `TimeProvider` solo en código nuevo y dejaba
20 lecturas de `DateTime.UtcNow` en 8 archivos y la abstracción propia `IDateTimeProvider` como obsoleta. Convivían
dos estilos, y las expiraciones, el bloqueo por intentos y los refresh tokens no se podían probar sin depender del
reloj real. Se migró todo el código existente.

Alternativas: mantener ADR-0017 (dos estilos indefinidamente) o conservar `IDateTimeProvider` como fachada sobre
`TimeProvider` (una abstracción propia que no aporta nada sobre la del framework).

## Decisión

- **Todo** el código de producción obtiene la hora de `TimeProvider` (registrado como `TimeProvider.System` en
  `AddBuildingBlocks`). Nunca `DateTime.UtcNow`, `DateTime.Now` ni `DateTimeOffset.UtcNow`.
- Las **entidades de dominio no inyectan servicios**: la hora llega como parámetro (`DateTime utcNow`) desde el handler
  o el servicio. Las comprobaciones que dependen de la hora son métodos (`User.IsLockedOut(utcNow)`,
  `RefreshToken.IsExpired(utcNow)`, `RefreshToken.IsActive(utcNow)`), no propiedades.
- Para medir duraciones (p. ej. el uptime de `/_info`) se usa `Stopwatch`, que no depende del reloj de pared.
- `IDateTimeProvider` y `SystemDateTimeProvider` se eliminan.
- Los tests usan un reloj fijo (`TestTime` / `FixedTimeProvider` en `Identity.UnitTests`), sin paquetes adicionales.
- `SystemClockUsageTests` lo comprueba con una lista de excepciones **vacía**.

## Consecuencias

- Toda lógica temporal es determinista en las pruebas.
- Las firmas del dominio llevan el instante explícito (`new User(…, createdAtUtc)`, `user.Deactivate(utcNow)`…).
- Una lectura directa del reloj en producción rompe `dotnet test`.

## Referencias

`Camilo.OrderFlow.BuildingBlocks.Infrastructure/DependencyInjection/BuildingBlocksServiceCollectionExtensions.cs`,
`Camilo.OrderFlow.Modules.Identity.Domain/Entities/User.cs`, `RefreshToken.cs`,
`tests/Camilo.OrderFlow.ArchitectureTests/SystemClockUsageTests.cs`, `tests/Camilo.OrderFlow.Identity.UnitTests/TestTime.cs`.
