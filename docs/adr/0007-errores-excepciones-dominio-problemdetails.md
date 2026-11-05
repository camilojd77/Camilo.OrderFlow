# ADR-0007 — Errores como excepciones de dominio traducidas a ProblemDetails

- **Estado:** Aceptada
- **Fecha:** 2026-11-05 · **Decide:** Camilo Jaramillo
- **Revisar cuando:** el coste de las excepciones en rutas calientes sea medible, o un consumidor necesite errores
  tipados por operación (patrón Result).

## Contexto

Los handlers necesitan señalar «no existe», «conflicto», «prohibido» o «datos inválidos» de forma que la API responda
con el código HTTP correcto y un cuerpo estándar. Alternativas: patrón Result (`Result<T>`/`Error`) devuelto por cada
handler y traducido en el endpoint (más explícito, más código por caso de uso); códigos de estado decididos en cada
controlador. El plan de construcción mantuvo las excepciones (D8) por coherencia con lo ya escrito.

## Decisión

- Los errores esperados se lanzan como excepciones que heredan de `DomainException` (SharedKernel):
  `NotFoundException` (404), `ConflictException` (409), `ForbiddenException` (403), `ValidationException` (400, con
  diccionario de errores por campo). `UnauthorizedAccessException` → 401.
- `GlobalExceptionHandler : IExceptionHandler` las traduce a `ProblemDetails`; cualquier otra excepción es 500 con un
  mensaje genérico. `AddProblemDetails` añade `traceId` y `correlationId` a todas las respuestas de error.
- La validación de entrada la hace `ValidationPipelineBehavior` (FluentValidation) antes del handler.
- Un error nuevo = una excepción nueva que hereda de `DomainException` + su línea de mapeo en `GlobalExceptionHandler`.
  No se capturan excepciones para silenciarlas.

## Consecuencias

- Handlers simples y un único punto de traducción a HTTP.
- `Detail` devuelve el `Message` de la excepción en errores 4xx: los mensajes **no deben llevar datos personales**
  (hoy `ConflictException` incluye el correo o el usuario; queda como propuesta revisarlo).
- Los endpoints Minimal API nuevos ([ADR-0019](0019-endpoints-nuevos-minimal-apis.md)) usan el mismo mecanismo.

## Referencias

`Camilo.OrderFlow.BuildingBlocks.SharedKernel/Exceptions/*`, `Camilo.OrderFlow.WebApi/Errors/GlobalExceptionHandler.cs`,
`Camilo.OrderFlow.WebApi/Program.cs` (`AddProblemDetails`), `Identity.Application/Behaviors/ValidationPipelineBehavior.cs`.
