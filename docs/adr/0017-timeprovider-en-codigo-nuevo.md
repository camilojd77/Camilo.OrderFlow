# ADR-0017 — `TimeProvider` en código nuevo; `IDateTimeProvider` obsoleto

- **Estado:** Aceptada
- **Fecha:** 2026-11-05 · **Decide:** Camilo Jaramillo
- **Revisar cuando:** se retire `IDateTimeProvider` y se migre el código existente.

## Contexto

El repo tiene una abstracción propia, `IDateTimeProvider` (SharedKernel, registrada como singleton), pero casi nadie
la usa: entidades de Domain, `AuthService`, `JwtTokenService`, `RefreshTokenRepository` e `InfoController` leen
`DateTime.UtcNow` directamente. Eso hace las expiraciones y bloqueos difíciles de probar. .NET trae `TimeProvider`
(con `FakeTimeProvider` para pruebas), que es el estándar del framework.

Alternativas: imponer `IDateTimeProvider` (abstracción propia que nadie fuera del repo conoce) o seguir con
`DateTime.UtcNow`.

## Decisión

- **Código nuevo**: obtiene la hora de un `TimeProvider` inyectado (`GetUtcNow()`); en Domain, el valor llega como
  parámetro del método (la entidad no inyecta servicios).
- **No** se usa `DateTime.UtcNow`/`DateTime.Now` en código nuevo de producción.
- `IDateTimeProvider` queda **obsoleto**: no se usa en código nuevo. Su retirada y la migración del código existente
  son una propuesta aparte; **no** se migra lo existente al tocarlo de paso.
- El primer uso registra `TimeProvider.System` como singleton en la composición (si aún no lo está).

## Consecuencias

- Conviven dos estilos hasta que se migre lo existente.
- Las pruebas de código nuevo pueden controlar el reloj.
- Propuesta abierta: test de arquitectura que impida `DateTime.UtcNow` en código nuevo.

## Referencias

`Camilo.OrderFlow.BuildingBlocks.SharedKernel/Time/IDateTimeProvider.cs`,
`Camilo.OrderFlow.BuildingBlocks.Infrastructure/Time/SystemDateTimeProvider.cs`, inventario
`docs-develop/analisis/01-inventario.md` (C-10, D-16).
