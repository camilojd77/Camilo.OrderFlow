---
paths:
  - "Camilo.OrderFlow.WebApi/Controllers/**"
  - "Camilo.OrderFlow.WebApi/Endpoints/**"
---

# Endpoints HTTP

- **Endpoints nuevos: Minimal APIs** con `MapGroup` por recurso y handlers que devuelven `TypedResults`
  (`Results<Ok<T>, NotFound>`…) ([ADR-0019](../../docs/adr/0019-endpoints-nuevos-minimal-apis.md)). Los controladores
  existentes se quedan; no se migran al tocarlos.
- Todavía no hay ningún endpoint Minimal API: el primero añade `Asp.Versioning.Http` con la skill
  `actualizar-dependencias`, decide cómo aplicar el permiso como política y actualiza esta regla y la skill
  `nuevo-endpoint`.
- El endpoint solo traduce HTTP ↔ request: llama a `ISender.Send(…, cancellationToken)`. Sin lógica de negocio, sin
  `DbContext`, sin entidades en la respuesta (DTOs).
- Autorización por permiso con la constante (`IdentityPermissions.UsersRead`), nunca un literal ni
  `[Authorize(Roles = …)]`. `[Authorize]` solo para «cualquier usuario autenticado».
- Versión por cabecera `api-version`; un cambio incompatible va en una versión nueva, no rompe la 1.0.
- Documenta cada código de respuesta posible (`[ProducesResponseType]` en controladores; `TypedResults`/`Produces`
  en Minimal APIs).
- Errores: lanzar la excepción de dominio; `GlobalExceptionHandler` la convierte en `ProblemDetails`. No construyas
  respuestas de error a mano.
- Rutas en `api/<recurso-en-plural-kebab-case>`; acciones como sub-recursos (`/{id}/deactivate`).
- Códigos: 200 GET con cuerpo · 201 POST con `Location` · 204 sin cuerpo · 400 validación · 401 sin token ·
  403 sin permiso · 404 no existe · 409 conflicto.
- Endpoints de autenticación o sensibles: política de rate limiting `auth` (`[EnableRateLimiting("auth")]`).

Referencia: `Camilo.OrderFlow.WebApi/Controllers/UsersController.cs`, `Controllers/AuthController.cs`.

Fuente: [ADR-0007](../../docs/adr/0007-errores-excepciones-dominio-problemdetails.md) ·
[ADR-0008](../../docs/adr/0008-versionado-por-cabecera-y-swashbuckle.md) ·
[ADR-0030](../../docs/adr/0030-cache-de-permisos-con-ventana-acotada.md) · [docs/architecture.md](../../docs/architecture.md) §8
