---
name: nuevo-endpoint
description: "Añade un endpoint HTTP nuevo en Camilo.OrderFlow como Minimal API con TypedResults, versionado por cabecera, permiso como política, OpenAPI y test de integración. Úsala al exponer un command o query nuevo por HTTP."
argument-hint: "<Recurso> <verbo y ruta>"
---

# Nuevo endpoint (Minimal API)

Los endpoints nuevos son Minimal APIs ([ADR-0019](../../../docs/adr/0019-endpoints-nuevos-minimal-apis.md)); los
controladores existentes se quedan.

> **Aún no existe ningún endpoint Minimal API en el repo.** El primero establece la referencia: tras escribirlo,
> actualiza esta skill y `.claude/rules/api.md` con su ruta y quita este aviso. Para la API de Minimal APIs, consulta la
> documentación oficial (skill `dotnet-referencia`, Microsoft Learn) en vez de escribir de memoria.

1. **Contrato de referencia.** Lee `Camilo.OrderFlow.WebApi/Controllers/UsersController.cs`: rutas, permisos, códigos
   de respuesta y llamada a `ISender`. El endpoint nuevo expone el mismo tipo de contrato.
2. **Primer endpoint Minimal API del repo (solo esa vez):**
   - Añade `Asp.Versioning.Http` con la skill `actualizar-dependencias` (la persona tiene que aprobarlo).
   - Decide con la persona cómo se aplica el permiso: política por permiso con `RequireAuthorization(...)` reutilizando
     `PermissionPolicyProvider` (`Camilo.OrderFlow.Modules.Identity.Infrastructure/Authorization/`). Si la decisión
     tiene alternativas reales, skill `nuevo-adr`.
   - Crea `Camilo.OrderFlow.WebApi/Endpoints/` y una extensión `Map<Recurso>Endpoints(this IEndpointRouteBuilder)`
     llamada desde `Program.cs`, junto a `MapControllers()`.
3. **Grupo**: `MapGroup("api/<recurso>")` con versión 1.0 (cabecera `api-version`,
   [ADR-0008](../../../docs/adr/0008-versionado-por-cabecera-y-swashbuckle.md)), rate limiting `default` y
   autorización.
4. **Handler**: lambda o método estático que recibe la request, `ISender` y `CancellationToken`; devuelve
   `TypedResults` con unión `Results<…>` de todos los resultados posibles. Sin lógica de negocio. Errores: lanza la
   excepción de dominio (lo traduce `GlobalExceptionHandler`).
5. **OpenAPI**: nombre, resumen y respuestas (las `TypedResults` las documentan); comprueba que aparece en Swagger con
   su versión.
6. **Test de integración** con `WebApiFactory`: caso feliz + 401 sin token + 403 sin permiso + 400/404 si aplica.
   Ref.: `tests/Camilo.OrderFlow.WebApi.IntegrationTests/UsersTests.cs`.
7. **Verificación**: skill `verificar-cambio`.
