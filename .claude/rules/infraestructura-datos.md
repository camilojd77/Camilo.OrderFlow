---
paths:
  - "Camilo.OrderFlow.Modules.*.Infrastructure/**"
---

# Infraestructura de los módulos (datos, auth, registro)

- Un `<Modulo>DbContext` por módulo; cada entidad se configura con `IEntityTypeConfiguration<T>` en
  `Persistence/Configurations` y su tabla en el esquema del módulo (`ToTable("X", "<esquema>")`).
- Nunca joins, claves foráneas ni consultas contra tablas de otro módulo.
- Repositorios en `Persistence/Repositories`: implementan los puertos de Application, **no** llaman a
  `SaveChangesAsync`; `AsNoTracking()` en lecturas; sin consultas N+1 (`Include` o proyección).
- El commit lo hace `<Modulo>ExecutionContext` (registrado como `I<Modulo>ExecutionContext`, scoped). Nunca registrar
  `IExecutionContext` a secas (lo comprueba `ModuleRegistrationTests`).
- Todo se registra en `Add<Modulo>Infrastructure(IConfiguration)`; `Program.cs` no registra servicios de un módulo.
- Opciones con `AddOptions<T>().BindConfiguration(...).ValidateDataAnnotations().ValidateOnStart()`.
- Permisos: constantes `<recurso>.<accion>` en `Authorization/<Modulo>Permissions.cs` (en Identity,
  `IdentityPermissions`) + entrada en el seed; nunca literales.
- Hashing (`IPasswordHasher` → BCrypt), JWT y refresh tokens no se debilitan sin ADR.
- **Código nuevo:** `TimeProvider` en lugar de `DateTime.UtcNow`, `[LoggerMessage]`, constructores primarios
  (ADR-0017/0018/0020); lo existente no se migra.

Referencia: `Persistence/Configurations/UserConfiguration.cs`, `Persistence/Repositories/UserRepository.cs`,
`Persistence/IdentityExecutionContext.cs`, `DependencyInjection/ServiceCollectionExtensions.cs` (todos en
`Camilo.OrderFlow.Modules.Identity.Infrastructure/`).

Fuente: [ADR-0005](../../docs/adr/0005-dbcontext-esquema-y-execution-context-por-modulo.md) ·
[ADR-0009](../../docs/adr/0009-autorizacion-por-permisos-con-hybridcache.md) ·
[ADR-0010](../../docs/adr/0010-identidad-propia-bcrypt-jwt.md) · [docs/architecture.md](../../docs/architecture.md) §7
