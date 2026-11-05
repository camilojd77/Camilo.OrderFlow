---
paths:
  - "**/Migrations/**"
---

# Migraciones de EF Core

- **No edites una migración existente** (ni su `.Designer.cs`): todas las del repo se consideran aplicadas. Un cambio
  de esquema = migración nueva.
- Créala con la skill `migracion-ef`:
  `dotnet ef migrations add <Nombre> --project Camilo.OrderFlow.Modules.<Modulo>.Infrastructure --startup-project Camilo.OrderFlow.WebApi --context <Modulo>DbContext`.
- Nombre en PascalCase que describe el cambio (`AddRefreshTokens`, `AddUserLockout`).
- Revisa el SQL antes de aplicarla: `dotnet ef migrations script --idempotent …`. Busca pérdidas de datos (`DROP`,
  cambios de tipo) y que todo vaya al esquema del módulo.
- Son código generado: `.editorconfig` las excluye del estilo; no las reformatees.
- No se aplican al arrancar la API; se aplican con `dotnet ef database update` o el contenedor `migrator`.

Referencia: `Camilo.OrderFlow.Modules.Identity.Infrastructure/Migrations/20260507203947_AddRefreshTokens.cs`.

Fuente: [ADR-0011](../../docs/adr/0011-migraciones-fuera-del-arranque.md) ·
[ADR-0005](../../docs/adr/0005-dbcontext-esquema-y-execution-context-por-modulo.md)
