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
- **`internal` por defecto** ([ADR-0028](../../docs/adr/0028-superficie-publica-de-los-modulos.md)): `DbContext`, configuraciones, repositorios,
  implementaciones de puertos, servicios y seeders. Público solo lo que el host necesita para componer.
- Opciones con `AddOptions<T>().BindConfiguration(...).ValidateDataAnnotations().ValidateOnStart()`.
- Permisos: constantes `<recurso>.<accion>` en `Authorization/<Modulo>Permissions.cs` (en Identity,
  `IdentityPermissions`) + entrada en el seed; nunca literales.
- Clientes HTTP que usa solo este módulo: en `Http/`, detrás de un puerto de Application, registrados con
  `AddHttpClient` en `Add<Modulo>Infrastructure`; resiliencia como `ErpClient` y reintentos solo si la operación es
  idempotente ([ADR-0022](../../docs/adr/0022-clientes-http-de-modulo-en-infrastructure.md)).
- Hashing (`IPasswordHasher` → BCrypt), JWT y refresh tokens no se debilitan sin ADR.
- `TimeProvider` inyectado para la hora, nunca `DateTime.UtcNow` (ADR-0025). **Código nuevo:** `[LoggerMessage]` y
  constructores primarios (ADR-0018/0020); lo existente no se migra.

Referencia: `Persistence/Configurations/UserConfiguration.cs`, `Persistence/Repositories/UserRepository.cs`,
`Persistence/IdentityExecutionContext.cs`, `DependencyInjection/ServiceCollectionExtensions.cs` (todos en
`Camilo.OrderFlow.Modules.Identity.Infrastructure/`).

Fuente: [ADR-0005](../../docs/adr/0005-dbcontext-esquema-y-execution-context-por-modulo.md) ·
[ADR-0030](../../docs/adr/0030-cache-de-permisos-con-ventana-acotada.md) ·
[ADR-0029](../../docs/adr/0029-revocacion-de-sesiones.md) · [docs/architecture.md](../../docs/architecture.md) §7
