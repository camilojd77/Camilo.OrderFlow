# ADR-0009 — Autorizar por permisos en base de datos con caché HybridCache

- **Estado:** Reemplazada por [ADR-0030](0030-cache-de-permisos-con-ventana-acotada.md)
- **Fecha:** 2026-11-05 · **Decide:** Camilo Jaramillo
- **Revisar cuando:** se despliegue con varias réplicas y haya que garantizar un plazo de revocación, o se adopte un
  proveedor de identidad externo.

## Contexto

Los permisos deben cambiar sin redesplegar ni reemitir tokens, y con granularidad mayor que el rol. Alternativas:
`[Authorize(Roles = "...")]` (rígido, roles en código), permisos como *claims* dentro del JWT (cambian solo al
reemitir el token) o consultar la base en cada petición sin caché (coste por petición).

## Decisión

- Los endpoints se protegen con `[AuthorizePermission(IdentityPermissions.X)]` (o la política equivalente en Minimal
  APIs). Los códigos de permiso son constantes en `IdentityPermissions` (`users.read`, `users.write`, …) y se siembran
  en base de datos.
- `PermissionService` resuelve los permisos del usuario desde sus roles y los guarda en **HybridCache**: L1 en memoria
  siempre, L2 Redis si `Cache:Redis` tiene valor. Clave `perms:user:<id>`, etiqueta `role:<id>`; TTL configurable en
  `Permissions:*`.
- Al cambiar roles o permisos se invalida por usuario (`RemoveAsync`) o por rol (`RemoveByTagAsync`).

## Consecuencias

- Los cambios de permisos se aplican sin reemitir el JWT, dentro de la ventana de invalidación/TTL.
- **No hay garantía de revocación inmediata entre réplicas**: está probado con una sola instancia. Medirlo es deuda
  abierta. Desactivar un usuario no invalida los access tokens ya emitidos.
- No se usa `[Authorize(Roles=...)]`.

## Referencias

`Camilo.OrderFlow.Modules.Identity.Infrastructure/Authorization/*`, `Persistence/Seed/IdentityDataSeeder.cs`,
`Identity.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`, `Camilo.OrderFlow.WebApi/Program.cs` (Redis
opcional), `tests/Camilo.OrderFlow.WebApi.IntegrationTests/CacheInvalidationTests.cs`.
