# ADR-0019 — Endpoints nuevos con Minimal APIs y `TypedResults`

- **Estado:** Aceptada
- **Fecha:** 2026-11-05 · **Decide:** Camilo Jaramillo
- **Revisar cuando:** se haya escrito el primer grupo de endpoints Minimal API (para fijar la referencia) o se decida
  migrar los controladores existentes.

## Contexto

Toda la API actual son controladores (`[ApiController]`) que llaman a `ISender`. Las Minimal APIs son el estilo que
.NET recomienda para servicios nuevos: menos ceremonia, tipos de respuesta explícitos con `TypedResults` que alimentan
OpenAPI, y mejor encaje con una futura extracción por módulo. Alternativa: seguir con controladores para todo
(consistente, pero sigue la dirección contraria al framework).

## Decisión

- Los **endpoints nuevos** se escriben como Minimal APIs agrupadas por recurso (`MapGroup`), con handlers que devuelven
  `TypedResults` (`Results<Ok<T>, NotFound>`…), llaman a `ISender` y reciben `CancellationToken`.
- Los **controladores existentes se quedan**; no se migran al tocarlos.
- Se mantienen las decisiones transversales: versionado por cabecera ([ADR-0008](0008-versionado-por-cabecera-y-swashbuckle.md)),
  permisos ([ADR-0009](0009-autorizacion-por-permisos-con-hybridcache.md), como política con `RequireAuthorization`),
  errores por excepciones ([ADR-0007](0007-errores-excepciones-dominio-problemdetails.md)) y rate limiting.

## Consecuencias

- Conviven dos estilos de endpoint en el host.
- **El primer endpoint Minimal API necesita `Asp.Versioning.Http`**, que no está en `Directory.Packages.props`: se
  añade con la skill `actualizar-dependencias` en ese mismo cambio, y se decide cómo aplicar la política de permiso
  (hoy `[AuthorizePermission]` es un atributo para controladores).
- No hay aún ningún ejemplo en el repo: ese primer cambio establece la referencia y actualiza la skill `nuevo-endpoint`
  y la regla `api`.

## Referencias

`Camilo.OrderFlow.WebApi/Controllers/*`, `Camilo.OrderFlow.WebApi/Versioning/ApiVersioningExtensions.cs`,
`Identity.Infrastructure/Authorization/*`, skill `nuevo-endpoint`, regla `.claude/rules/api.md`.
