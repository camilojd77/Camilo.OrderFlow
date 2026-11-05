# ADR-0018 — Logs nuevos con `[LoggerMessage]` generado

- **Estado:** Aceptada
- **Fecha:** 2026-11-05 · **Decide:** Camilo Jaramillo
- **Revisar cuando:** se cambie de proveedor de logging o el generador deje de cubrir un caso necesario.

## Contexto

Hoy el código de la aplicación no escribe ningún log propio: no hay un solo `ILogger<T>`; solo existe el log de
peticiones de Serilog. Los logs que se añadan tienen que ser estructurados (plantilla con propiedades), baratos cuando
el nivel está desactivado y coherentes. Alternativas: llamadas directas `logger.LogInformation("…{Id}", id)`
(estructuradas, pero sin comprobación en compilación y con *boxing*), o la API estática `Log.*` de Serilog (acopla el
código al proveedor).

## Decisión

- Todo log nuevo se escribe con métodos `partial` anotados con `[LoggerMessage]` (generador de
  `Microsoft.Extensions.Logging`), sobre `ILogger<T>` inyectado. Nunca `Serilog.Log` estático en código de aplicación.
- Plantillas con nombres de propiedad en PascalCase; **nunca** interpolación ni concatenación.
- Sin datos personales ni secretos en los argumentos (ver `docs/observabilidad.md`).
- `EventId` estable por mensaje dentro de cada clase.

## Consecuencias

- Comprobación en compilación de plantilla y argumentos; mejor rendimiento.
- Un poco más de código (clase o métodos `partial`).
- Serilog sigue siendo el proveedor ([ADR-0015](0015-telemetria-otel-y-serilog-loki.md)): recibe estos logs por
  `Microsoft.Extensions.Logging`.

## Referencias

`Camilo.OrderFlow.WebApi/Program.cs` (`UseSerilog`), `docs/observabilidad.md`, skill `instrumentar`.
