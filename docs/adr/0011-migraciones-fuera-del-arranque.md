# ADR-0011 — Aplicar migraciones fuera del arranque

- **Estado:** Aceptada
- **Fecha:** 2026-11-05 · **Decide:** Camilo Jaramillo
- **Revisar cuando:** el despliegue use un pipeline con paso de migración propio (bundle de EF o scripts SQL
  revisados).

## Contexto

Aplicar migraciones dentro de la API al arrancar (`Database.Migrate()`) es cómodo, pero con varias réplicas compiten
entre sí, la cuenta de la aplicación necesita permisos de DDL y un fallo de migración tumba el arranque.

## Decisión

- La API **no** migra al arrancar.
- En local: `dotnet tool restore` y `dotnet ef database update --project <Modulo>.Infrastructure --startup-project
  Camilo.OrderFlow.WebApi --context <Modulo>DbContext`.
- En contenedores: el target `migrator` del `Dockerfile` ejecuta `dotnet ef database update` y termina; en
  `deploy/app/docker-compose.yml` la API espera a que acabe con éxito.
- Las migraciones viven en `<Modulo>.Infrastructure/Migrations`; una migración aplicada no se edita: se crea otra.

## Consecuencias

- Migrar es un paso explícito y separado del despliegue de la API.
- Hay que acordarse de migrar en local tras cambiar de rama.
- `dotnet-ef` es herramienta local del repo (`.config/dotnet-tools.json`); su versión no coincide con la de EF Core del
  proyecto (10.0.11 frente a 10.0.3), sin incompatibilidad observada.

## Referencias

`Dockerfile` (target `migrator`), `deploy/app/docker-compose.yml`, `.config/dotnet-tools.json`,
`Camilo.OrderFlow.Modules.Identity.Infrastructure/Migrations/*`, skill `migracion-ef`.
