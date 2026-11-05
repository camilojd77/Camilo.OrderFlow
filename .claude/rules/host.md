---
paths:
  - "Camilo.OrderFlow.WebApi/Program.cs"
  - "Camilo.OrderFlow.WebApi/Extensions/**"
  - "Camilo.OrderFlow.WebApi/Middleware/**"
  - "Camilo.OrderFlow.WebApi/Security/**"
  - "Camilo.OrderFlow.WebApi/Errors/**"
  - "Camilo.OrderFlow.WebApi/Versioning/**"
  - "Camilo.OrderFlow.WebApi/Swagger/**"
  - "Camilo.OrderFlow.WebApi/Infrastructure/**"
---

# Host (composición y middleware)

- `Program.cs` **compone**: registra cada módulo con dos líneas (`Add<Modulo>Application()`,
  `Add<Modulo>Infrastructure(builder.Configuration)`) y configura lo transversal. Los servicios de un módulo se
  registran en su propio `Add<Modulo>…`, no aquí.
- Los **adaptadores entre módulos** viven en el host: implementan el puerto del consumidor llamando a la fachada del
  proveedor ([ADR-0006](../../docs/adr/0006-integracion-modulos-adaptador-en-host.md)).
- Lo transversal se registra con extensiones `Add<Algo>`/`Use<Algo>` en `Extensions/`, con su sección de
  configuración y opciones validadas al arrancar.
- Orden del pipeline actual: excepciones → security headers → correlation id → log de peticiones → HTTPS → CORS →
  rate limiting → autenticación → autorización → endpoints. No lo reordenes sin motivo escrito.
- Un tipo de error nuevo = excepción que hereda de `DomainException` + una línea de mapeo en
  `Errors/GlobalExceptionHandler.cs`. No reescribas el handler.
- No debilites security headers, CORS (`*` con credenciales), `ClockSkew`, ni la validación de opciones.
- `HttpClient` saliente: con `Microsoft.Extensions.Http.Resilience` siguiendo el patrón `ErpClient` y, si aplica,
  `CorrelationIdHandler`. Ojo: los reintentos solo para operaciones idempotentes.

Referencia: `Camilo.OrderFlow.WebApi/Program.cs`, `Errors/GlobalExceptionHandler.cs`,
`Extensions/ServiceCollectionExtensions.Resilience.cs`, `Middleware/CorrelationIdMiddleware.cs`.

Fuente: [docs/architecture.md](../../docs/architecture.md) §8-9 ·
[ADR-0007](../../docs/adr/0007-errores-excepciones-dominio-problemdetails.md) ·
[ADR-0013](../../docs/adr/0013-configuracion-sin-secretos-y-opciones-validadas.md)
