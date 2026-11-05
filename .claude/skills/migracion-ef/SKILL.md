---
name: migracion-ef
description: "Crea, revisa y aplica en local una migración de EF Core para un módulo de Camilo.OrderFlow sin tocar las existentes. Úsala cuando cambie una entidad o su configuración EF."
argument-hint: "<Modulo> <NombreMigracion>"
---

# Migración de EF Core

Las migraciones se aplican fuera del arranque y una existente no se edita
([ADR-0011](../../../docs/adr/0011-migraciones-fuera-del-arranque.md)).

1. `dotnet tool restore` (trae `dotnet-ef` de `.config/dotnet-tools.json`).
2. Confirma que el cambio de modelo compila: `dotnet build Camilo.OrderFlow.slnx`.
3. Crea la migración (nombre en PascalCase que describe el cambio):
   ```
   dotnet ef migrations add <Nombre> --project Camilo.OrderFlow.Modules.<M>.Infrastructure --startup-project Camilo.OrderFlow.WebApi --context <M>DbContext
   ```
   Necesita `appsettings.Development.json` local (el host valida `Jwt:Key` y las cadenas de conexión al construirse).
4. Lee la migración generada junto a la última existente (ref.:
   `Camilo.OrderFlow.Modules.Identity.Infrastructure/Migrations/20260507203947_AddRefreshTokens.cs`): todo en el esquema
   del módulo, sin `DROP` ni cambios de tipo inesperados, sin tocar tablas de otro módulo.
5. Revisa el SQL:
   ```
   dotnet ef migrations script --idempotent --project Camilo.OrderFlow.Modules.<M>.Infrastructure --startup-project Camilo.OrderFlow.WebApi --context <M>DbContext
   ```
   Señala a la persona cualquier operación con pérdida de datos.
6. Aplica en local: `dotnet ef database update` con los mismos `--project/--startup-project/--context`.
7. Si te equivocaste y la migración **no** se ha commiteado: `dotnet ef migrations remove …`. Si ya está en git, crea
   otra que corrija.
8. Verifica con la skill `verificar-cambio`, incluida la suite RealDb (aplica todas las migraciones sobre un SQL Server
   real).
