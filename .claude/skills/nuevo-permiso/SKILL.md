---
name: nuevo-permiso
description: "Añade un permiso nuevo en Camilo.OrderFlow: constante, seed, uso en el endpoint e invalidación de caché. Úsala cuando un endpoint necesite un permiso que no existe en IdentityPermissions o en el <Modulo>Permissions del módulo."
argument-hint: "<recurso>.<accion>"
---

# Nuevo permiso

Autorización por permiso en base de datos, nunca por rol ([ADR-0009](../../../docs/adr/0009-autorizacion-por-permisos-con-hybridcache.md)).

1. **Constante.** Lee `Camilo.OrderFlow.Modules.Identity.Infrastructure/Authorization/IdentityPermissions.cs`. Añade
   `public const string XRead = "<recurso>.<accion>";` (minúsculas, punto) y súmala a la lista `All`. En otro módulo,
   crea `<Modulo>Permissions` en su `Infrastructure/Authorization/` con la misma forma.
2. **Seed.** Lee `Camilo.OrderFlow.Modules.Identity.Infrastructure/Persistence/Seed/IdentityDataSeeder.cs` y añade el
   permiso para que exista en base de datos y lo tenga el rol administrador (el seeder falla al arrancar si una
   constante declarada no está sembrada).
3. **Endpoint.** Usa siempre la constante (`[AuthorizePermission(IdentityPermissions.XRead)]` o la política
   equivalente en Minimal APIs), nunca el literal: un literal mal escrito compila y da un 403 imposible de
   diagnosticar.
4. **Caché.** Si un caso de uso cambia roles o permisos, invalida **después** del commit:
   `IPermissionService.InvalidateUserAsync(userId, ct)` o `InvalidateRoleAsync(roleId, ct)`. Ref.:
   `Camilo.OrderFlow.Modules.Identity.Infrastructure/Authorization/PermissionService.cs` y el handler
   `Application/Commands/Roles/AssignPermissions/AssignPermissionsToRoleCommandHandler.cs`.
5. **Tests.** Integración: con permiso 200/204, sin permiso 403. Ref.:
   `tests/Camilo.OrderFlow.WebApi.IntegrationTests/PermissionsTests.cs`.
6. **Verificación**: skill `verificar-cambio`.
