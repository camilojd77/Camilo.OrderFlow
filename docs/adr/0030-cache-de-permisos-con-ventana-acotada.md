# ADR-0030 — Autorizar por permisos en base de datos, con caché de ventana acotada entre réplicas

- **Estado:** Aceptada
- **Fecha:** 2026-11-05 · **Decide:** Camilo Jaramillo
- **Revisar cuando:** se despliegue con varias réplicas y un minuto de permisos obsoletos no sea aceptable (habría que
  añadir un mecanismo de invalidación entre instancias), o se adopte un proveedor de identidad externo.

## Contexto

Reemplaza a [ADR-0009](0009-autorizacion-por-permisos-con-hybridcache.md), que dejaba sin acotar la coherencia de la
caché de permisos con varias réplicas. La documentación de HybridCache (ASP.NET Core 10) lo aclara: al invalidar por
clave o por etiqueta, el cambio llega a la instancia actual y a la caché distribuida (L2, Redis), pero **no a la
memoria local (L1) de las demás instancias**. Con `LocalCacheTtlMinutes = 5`, otra réplica podía seguir autorizando
con permisos retirados hasta 5 minutos.

Alternativas: un backplane de invalidación (p. ej. Redis pub/sub) para vaciar la L1 de todas las instancias
(infraestructura y código nuevos sin réplicas reales hoy); sin L1 (una consulta a Redis o a la base en cada petición);
o dejar 5 minutos.

## Decisión

Se mantiene lo de ADR-0009:

- Autorización por permisos (`[AuthorizePermission(IdentityPermissions.X)]` o la política equivalente), nunca por
  roles fijos; los códigos de permiso son constantes y se siembran.
- `PermissionService` cachea con HybridCache: L1 en memoria y L2 Redis si `Cache:Redis` tiene valor; clave
  `perms:user:<id>` y etiqueta `role:<id>`.
- Cambiar roles o permisos invalida la caché por usuario o por rol.

Y se acota:

- `Permissions:LocalCacheTtlMinutes` pasa a **1** por defecto (código, `appsettings.json` y el `.example`).
- **Ventana conocida:** tras un cambio de permisos, la instancia que lo procesa y Redis quedan al día al momento; otra
  réplica puede usar permisos obsoletos como mucho `LocalCacheTtlMinutes` (1 min). Sin Redis, cada réplica tarda hasta
  `CacheTtlMinutes` (10 min) en ver el cambio: con varias réplicas, Redis es obligatorio.
- Desactivar a un usuario no depende de esta caché: sus sesiones se cortan al momento
  ([ADR-0029](0029-revocacion-de-sesiones.md)).

## Consecuencias

- Con varias réplicas, la L1 se rellena más a menudo: un poco más de tráfico a Redis o a la base.
- Un despliegue con varias réplicas sin Redis no cumple esta decisión; se comprueba en el ReleaseChecklist.
- Si un minuto no basta, se reemplaza este ADR con un mecanismo de invalidación entre instancias.

## Referencias

`Camilo.OrderFlow.Modules.Identity.Infrastructure/Authorization/PermissionService.cs`,
`Identity.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`, `appsettings.json` (`Permissions`,
`Cache:Redis`), `docs/ReleaseChecklist.md`, documentación de HybridCache en ASP.NET Core 10 («Cache storage»).
