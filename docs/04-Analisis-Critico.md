# Documento 4 — Análisis crítico y plan de ajustes

> **Audiencia:** equipo técnico que decide si esta plantilla queda lista como base organizacional.
> **Objetivo:** auditar las decisiones actuales contra el estado del arte de .NET 10 (mayo 2026), señalar lo que **se debe corregir antes** de seguir con el [Plan de Implementación](03-Plan-Implementacion.md), y reescribir las fases afectadas.
> **Veredicto general:** la plantilla está **bien encaminada** en estructura, observabilidad y disciplina (Clean Arch + Modular Monolith + tests de arquitectura). Pero arrastra **3 dependencias con licencia comercial** y **un patrón redundante** que conviene resolver **antes** de implementar Commands/Queries reales — porque después tocaría reescribir cada handler.

---

## 1. Resumen ejecutivo

| # | Hallazgo | Severidad | Acción |
|---|---|---|---|
| 1 | **AutoMapper 16.1.0** es licencia comercial | Alta | Reemplazar por **Mapperly** o mapeo manual |
| 2 | **MediatR 14.1.0** es licencia comercial (mismo autor) | Alta | Reemplazar por **Mediator (M. Othamar)** o un dispatcher propio |
| 3 | **FluentAssertions 8.8.0** es licencia comercial (Xceed) | Alta | Reemplazar por **Shouldly** o **AwesomeAssertions** (fork OSS de 7.x) |
| 4 | **`IUnitOfWork` planificado** redundante con `DbContext` | Media | Sustituir por `IIdentityDbContext` mínimo o llamar `SaveChangesAsync` desde repos |
| 5 | **`MemoryCache` para permisos** no escala a multi-instancia | Media | Migrar a **HybridCache** (.NET 9+, ya disponible en .NET 10) |
| 6 | **`Microsoft.EntityFrameworkCore.InMemory`** desaconsejado por Microsoft para tests | Media | Usar **SQLite in-memory** o **Testcontainers** |
| 7 | Patrón **Repository sobre `DbSet<T>`** discutible | Baja | Mantener (mejor testabilidad) o pasar a `IDbContext` directo — decidir y ser consistente |
| 8 | **BCrypt.Net-Next 4.1.0** sin actualización reciente | Baja | Aceptable, mantener; documentar que el reemplazo natural sería ASP.NET `PasswordHasher` (PBKDF2) o Argon2 |

**Bloqueantes reales para seguir el Plan de Implementación:** los **#1, #2, #3, #4**. Los demás se pueden meter en sus fases respectivas (`#5` en fase 7, `#6` en fase 9).

---

## 2. Hallazgos críticos en detalle

### 2.1 Tres librerías ya están en licencia comercial (no solo AutoMapper)

> El usuario detectó AutoMapper. **El problema es más amplio**: Jimmy Bogard anunció en noviembre 2024 que tanto **AutoMapper** como **MediatR** pasaban a licenciamiento comercial (modelo dual: gratis para uso no comercial / pago para empresas). En enero 2025 **FluentAssertions** hizo lo mismo bajo Xceed.

**Aclaración técnica importante:** el motivo no es "vulnerabilidad". Las versiones OSS antiguas siguen funcionando y son seguras. El problema es **legal/comercial**: usar las versiones nuevas en software comercial requiere licencia paga por dev/año.

**Estado actual del repo (`Directory.Packages.props`):**

| Paquete | Versión instalada | Última versión OSS | Licencia actual |
|---|---|---|---|
| `AutoMapper` | **16.1.0** | 13.x | Comercial |
| `MediatR` | **14.1.0** | 12.x | Comercial |
| `FluentAssertions` | **8.8.0** | 7.x | Comercial (Xceed) |

**Uso real hoy (auditoría sobre el código):**

- `AutoMapper`: registrado en `src/Modules/Identity/Company.Product.Modules.Identity.Application/DependencyInjection/ServiceCollectionExtensions.cs:17`. **No hay un solo `Profile` ni un solo `IMapper` inyectado.** → Coste de migración: **trivial**, solo quitarlo.
- `MediatR`: registrado en el mismo archivo. **No hay un solo `IRequestHandler`.** Solo se usa la interfaz `IPipelineBehavior<,>` para `ValidationPipelineBehavior`. → Coste de migración: **bajo** ahora, **muy alto** si esperamos a tener 30+ handlers.
- `FluentAssertions`: solo usado en `tests/Company.Product.Identity.UnitTests`. → Coste de migración: **bajo** (un puñado de asserts).

**Recomendación:** **migrar AHORA**, antes de la Fase 1 del plan. Cada día que pasa, cada handler nuevo se escribe con `IRequest<T>` y cada DTO se piensa con `Profile` — y luego son 200 archivos a tocar.

#### 2.1.1 Reemplazo de AutoMapper

**Recomendado: [Mapperly](https://github.com/riok/mapperly)** (MIT, source generator, sin coste runtime).

```csharp
// Antes (AutoMapper)
public class IdentityMappingProfile : Profile
{
    public IdentityMappingProfile() => CreateMap<User, UserDto>();
}
// y luego: var dto = _mapper.Map<UserDto>(user);

// Después (Mapperly)
[Mapper]
public partial class UserMapper
{
    public partial UserDto ToDto(User user);
}
// y luego: var dto = _userMapper.ToDto(user);
```

Ventajas: cero reflexión, errores de mapeo en **tiempo de compilación**, performance ~10x mejor, sin coste de licencia.

**Alternativa válida:** mapeo manual con `static` extension methods (`user.ToDto()`). Para DTOs simples es **más legible** que cualquier mapper. Mapperly conviene cuando hay decenas de mapeos repetitivos.

#### 2.1.2 Reemplazo de MediatR

> Este es el cambio más estructural. Decidir bien ahorra reescrituras grandes en la Fase 4–6.

**Tres opciones reales:**

**Opción A — [Mediator de Martin Othamar](https://github.com/martinothamar/Mediator)** (MIT, source generator). API casi idéntica a MediatR, pero genera el dispatcher en compilación. Migración ≈ buscar/reemplazar de namespaces.

**Opción B — Dispatcher propio en `BuildingBlocks.SharedKernel`** (~50 líneas). Es lo que **recomiendo para una plantilla** porque:
- No introduce dependencia externa nueva.
- Total control sobre pipeline behaviors.
- Fácil de explicar a devs nuevos.

```csharp
public interface IRequest<TResponse> {}
public interface IRequestHandler<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken ct);
}
public interface ISender
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default);
}
// + un Sender que resuelve el handler vía IServiceProvider
// + un IPipelineBehavior<,> equivalente
```

**Opción C — Eliminar el dispatcher, usar handlers como clases inyectables directamente** (Vertical Slice). El controller inyecta `CreateUserHandler` y llama `handler.Handle(...)`. Es la opción **más simple y legible**, sin pipeline → la validación se mueve a un endpoint filter o a un decorador explícito.

**Recomendación final:** **Opción B** para mantener la estética CQRS que ya documenta `02-Instrucciones-IA.md` sin pagar licencia ni acoplarse a otra librería de terceros. Si el equipo prefiere menos código propio, Opción A.

#### 2.1.3 Reemplazo de FluentAssertions

**Recomendado: [Shouldly](https://github.com/shouldly/shouldly)** (MIT, mensajes de error excelentes, API muy similar):

```csharp
// Antes:  result.Should().NotBeNull();
// Después: result.ShouldNotBeNull();
```

**Alternativa:** [AwesomeAssertions](https://github.com/AwesomeAssertions/AwesomeAssertions) — fork OSS de FluentAssertions 7.x mantenido por la comunidad, **API idéntica**. Migración = solo cambiar `using FluentAssertions` por `using AwesomeAssertions`.

---

### 2.2 `IUnitOfWork` planificado es redundante con EF Core

> El usuario también detectó esto. Tiene razón.

**El argumento clásico de quienes proponen `IUnitOfWork`:** "desacoplar `Application` de EF Core".

**Por qué no aplica bien hoy:**
1. `DbContext` **ya implementa el patrón Unit of Work** (Microsoft lo dice explícitamente en su doc). Envolverlo en otro `IUnitOfWork.SaveChangesAsync()` es un envoltorio sobre un envoltorio.
2. `DbSet<T>` ya implementa `IRepository<T>`-like (`.Add`, `.Remove`, `.Update`, `IQueryable`). Un `IUserRepository` que solo redirecciona a `_db.Users.X` es ceremonia sin valor.
3. El testeo "sin EF Core" usando mocks de `IUnitOfWork` históricamente ha producido tests que pasan en mock y fallan en producción. La industria se ha movido a **integration tests con SQLite/Testcontainers**, donde EF Core real corre sobre un DB efímero.

**Lo que recomiendo en su lugar — dos patrones válidos, elegir uno:**

#### Patrón A: `IIdentityDbContext` (interfaz mínima en Application)

```csharp
// En Modules.Identity.Application/Abstractions
public interface IIdentityDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    Task<int> SaveChangesAsync(CancellationToken ct);
}

// En Infrastructure
public class IdentityDbContext : DbContext, IIdentityDbContext { ... }
```

> **Pero esto requiere que `Application` referencie `Microsoft.EntityFrameworkCore`** (por `DbSet<T>`). Eso choca con la regla 9.3 del Plan ("Application no debe referenciar EF Core"). Hay que **decidir explícitamente** si flexibilizamos esa regla.

#### Patrón B: Repositorios "gordos" sin `IUnitOfWork`

Cada repositorio expone solo lo que el dominio necesita (`GetById`, `Add`, `EmailExists`) y **llama a `SaveChangesAsync` internamente** cuando la operación es transaccional simple. Para operaciones que abarcan múltiples repos, se introduce **`ITransactionScope` o `IExecutionContext`** ad-hoc, no un `IUnitOfWork` genérico.

```csharp
// Application
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);  // <-- internamente hace SaveChanges
}

// Para una operación multi-repo (ej. crear usuario + asignar rol):
// - Opción 1: hacerlo en un solo método del UserRepository: AddWithRolesAsync(...)
// - Opción 2: el handler llama IExecutionContext.SaveAsync() al final
```

**Recomendación final:** **Patrón B** + repositorios delgados. Queda **idiomático con Clean Arch tradicional**, no requiere flexibilizar la regla "Application sin EF Core", y el `SaveChangesAsync` se invoca **explícitamente** por el handler al final de cada caso de uso (vía un `IExecutionContext.CommitAsync()` o similar — un `IUnitOfWork` mínimo, **sin** la interfaz pomposa).

**O alternativa más pragmática:** mantener el plan original con `IUnitOfWork`, pero documentar honestamente que **es solo un wrapper sobre `DbContext.SaveChangesAsync`** y **no** estamos prometiendo independencia de ORM. La verdad es que migrar de EF Core a otro ORM toca tantas otras cosas que esta abstracción no salva nada.

---

### 2.3 Otros hallazgos (severidad media-baja)

#### 2.3.1 `MemoryCache` para permisos no escala a multi-instancia

`PermissionService` cachea con `MemoryCache` (10 min). Si la API corre en **2 pods** y un admin cambia los permisos de un usuario, **solo el pod que recibió el `PUT` invalida**. El otro pod sigue sirviendo permisos viejos hasta el TTL.

El plan lo reconoce ("para microservicios futuros migrar a Redis"). Pero **incluso un monolito en producción se escala horizontalmente**. Solución moderna sin Redis:

→ **`HybridCache`** (oficial Microsoft, .NET 9+, disponible en .NET 10). Es un caché L1 in-memory + L2 distribuido opcional. Si configuras solo L1, se comporta como `MemoryCache`. Cuando metas Redis, solo cambias el registro DI. **Es la forma que recomienda hoy Microsoft** para casos como este.

→ Movilo a **Fase 7.1** del plan revisado.

#### 2.3.2 `Microsoft.EntityFrameworkCore.InMemory` está desaconsejado

Microsoft documenta explícitamente que **no se debe usar `EF.InMemory` para integration tests** (ignora constraints, comportamiento divergente vs SQL real). El paquete sigue ahí pero no recibe features nuevas y Microsoft recomienda:

- **SQLite in-memory** (`:memory:`) para tests rápidos.
- **Testcontainers** + SQL Server real para tests de alta fidelidad.

Está en `Directory.Packages.props` línea 32. Si el plan lo iba a usar para `WebApiFactory`, **conviene cambiar a SQLite in-memory antes de escribir los integration tests** (Fase 9.2).

#### 2.3.3 `BCrypt.Net-Next 4.1.0`

Funciona y es seguro, pero no recibe actualizaciones desde hace años. Para una plantilla **organizacional**, vale la pena documentar que el reemplazo natural sería:

- `Microsoft.AspNetCore.Identity.PasswordHasher<TUser>` (PBKDF2, FIPS-compliant) — ya viene con ASP.NET Core, sin paquete extra.
- `Konscious.Security.Cryptography.Argon2` para Argon2id (mejor estado del arte).

**Recomendación:** mantener BCrypt en el MVP de la plantilla **pero centralizado tras `IPasswordHasher`** (como ya lo plantea el plan en 1.5). Así el día que se cambie a Argon2 es una sola implementación nueva.

#### 2.3.4 Patrón Repository: mantener o no

El plan introduce `IUserRepository`, `IRoleRepository`, `IPermissionRepository`. Argumento moderno en contra: `DbSet<T>` ya es `IQueryable<T>`, y los repositorios terminan siendo proxy methods.

**Pero** para una plantilla organizacional con disciplina Clean Arch, **mantenerlos tiene sentido** porque:
- Permite añadir invariantes (ej. siempre `AsNoTracking()` en queries de listado).
- Centraliza queries complejas.
- Hace que `Application` no tenga que aprender LINQ avanzado.

→ **Mantener el plan original aquí**, pero recordar la regla: **un repo NO es solo un proxy a `DbSet<T>.Where()`**. Si solo proxy, mejor inyectar `IIdentityDbContext`.

---

## 3. Decisiones que necesito que confirmes antes de implementar

Estos son los puntos que **no debe decidir la IA por su cuenta**:

1. **Reemplazo de MediatR**: ¿Opción A (Mediator de Othamar), Opción B (dispatcher propio en BuildingBlocks), o Opción C (sin dispatcher, handlers directos)? R/ Opción B (dispatcher propio en BuildingBlocks).
2. **Reemplazo de AutoMapper**: ¿Mapperly o mapeo manual con extension methods? R/ Mapperly y recomendar usar mapeos manuales para los que requieran mucha lógica de negocio a futuro.
3. **Reemplazo de FluentAssertions**: ¿Shouldly (cambia API) o AwesomeAssertions (API idéntica)? R/ Shouldly
4. **`IUnitOfWork`**: ¿Patrón B con repos que llaman `SaveChanges` + `IExecutionContext` opcional, o aceptamos el envoltorio explícitamente? R/  Patrón B + repositorios delegados.
5. **`HybridCache` para permisos desde el día 1**, o aplazar a Fase 11? R/ HybridCache por defecto
Redis opcional pero documentado como requerido para multi-instancia real
MemoryCache eliminado del PermissionService
TTL de permisos configurable
Estrategia de invalidación por usuario/rol
6. **`SQLite in-memory` vs `Testcontainers`** para integration tests? R/ SQLite in-memory para velocidad, Testcontainers solo en CI nightly

> Mi recomendación combinada (lo más simple y consistente):
> **(1) Opción B**, **(2) Mapperly**, **(3) AwesomeAssertions**, **(4) Patrón B con `IExecutionContext`**, **(5) `HybridCache` desde día 1**, **(6) SQLite in-memory para velocidad, Testcontainers solo en CI nightly**.

---

## 4. Plan de cambios (revisión del [Plan de Implementación](03-Plan-Implementacion.md))

> Esta sección **reescribe** las fases afectadas. Las fases no listadas se mantienen tal como están en el documento 03.

### Fase 0 — Saneamiento de dependencias (NUEVA, **antes** de la Fase 1)

> Esfuerzo: **S** (½ día). Bloquea todo lo demás.

#### 0.1 Quitar AutoMapper

- En `Directory.Packages.props`: eliminar la línea `AutoMapper`.
- En `Modules.Identity.Application.csproj`: eliminar `<PackageReference Include="AutoMapper" />`.
- En `Modules.Identity.Application/DependencyInjection/ServiceCollectionExtensions.cs:17`: eliminar `services.AddAutoMapper(...)`.
- Actualizar `02-Instrucciones-IA.md` línea 56, 103, 145, 304: eliminar referencias a AutoMapper / `MappingProfile`.
- Si se elige **Mapperly**: agregar `Riok.Mapperly` a `Directory.Packages.props` y al `.csproj` de `Application`.
- Si se elige **mapeo manual**: añadir convención en `02-Instrucciones-IA.md`: `Modules.<X>.Application/Mapping/<Entity>Mappings.cs` con extension methods estáticos.

#### 0.2 Quitar MediatR

- En `Directory.Packages.props`: eliminar la línea `MediatR`.
- En `Modules.Identity.Application.csproj`: eliminar la `<PackageReference>`.
- Reescribir `Modules.Identity.Application/Behaviors/ValidationPipelineBehavior.cs` contra el nuevo dispatcher (si aplica).
- Reescribir el registro DI en `ServiceCollectionExtensions.cs`.
- Si **Opción B (dispatcher propio)**: crear `BuildingBlocks.SharedKernel/Messaging/IRequest.cs`, `IRequestHandler.cs`, `IPipelineBehavior.cs`, `ISender.cs`, e `Infrastructure/Messaging/Sender.cs` en `BuildingBlocks.Infrastructure`.
- Actualizar `02-Instrucciones-IA.md` secciones 4 (tabla de patrones), 5 (orden de implementación), 13 (resumen) cambiando "MediatR" por el nombre del nuevo mecanismo.

#### 0.3 Quitar FluentAssertions

- En `Directory.Packages.props`: cambiar a `Shouldly` o `AwesomeAssertions`.
- En `tests/Company.Product.Identity.UnitTests.csproj`: cambiar `<PackageReference>`.
- Buscar/reemplazar `using FluentAssertions;` y los `.Should()` (sólo unos pocos hoy).
- Actualizar `02-Instrucciones-IA.md` sección 8 (ejemplo de test).

#### 0.4 Architecture Tests: actualizar prohibiciones

- En `tests/Company.Product.ArchitectureTests/CleanArchitectureTests.cs`: reemplazar la regla "no AutoMapper en Domain" por "no `Riok.Mapperly` en Domain" y "no MediatR en Domain" por "no `*.Messaging` o el namespace del nuevo dispatcher".
- Añadir regla nueva: **`Application` no debe referenciar `AutoMapper.*` ni `MediatR.*`** (defensivo, por si alguien los re-añade).

#### 0.5 Verificación

- `dotnet restore && dotnet build && dotnet test` debe pasar verde.
- `dotnet list package | grep -E 'AutoMapper|MediatR|FluentAssertions'` debe estar vacío.

✅ **Resultado:** la plantilla queda 100% bajo licencias OSS permisivas.

---

### Fase 1 — Cimientos transversales (modificada)

Cambios respecto al plan original:

- **1.3 `IUnitOfWork`** → **renombrar a `IExecutionContext`** y dejar solo `Task CommitAsync(CancellationToken)`. Implementarla como wrapper directo sobre `IdentityDbContext.SaveChangesAsync`. Documentar en `02-Instrucciones-IA.md` que es **solo el commit**, no una abstracción de ORM.
  - Alternativa si se acepta Patrón A: crear `IIdentityDbContext` que expone `DbSet<T>` + `SaveChangesAsync`, **sin** `IExecutionContext`. Pero entonces hay que flexibilizar la regla 9.3.
- **1.4 Repositorios** → mantener, pero documentar que **un repo no es solo proxy a `DbSet<T>`**: si solo redirigiría, entonces ese caso se hace directo desde el handler con `IExecutionContext.Db.Users.X` (o equivalente).
- **1.6 AutoMapper Profile inicial** → **eliminar**. Sustituir por:
  - **Si Mapperly**: `Modules.Identity.Application/Mapping/UserMapper.cs` (`partial class` con `[Mapper]`).
  - **Si manual**: `Modules.Identity.Application/Mapping/UserMappings.cs` con `public static UserDto ToDto(this User u) => new(...);`.
- **1.7 DTOs** → sin cambios.

---

### Fase 2 — UserProfile (sin cambios)

---

### Fase 3 — Auth (sin cambios estructurales)

Solo recordar: cada `RegisterUserCommand`/`Handler` se escribe contra el **nuevo** `IRequest`/`IRequestHandler` (de Fase 0), no contra MediatR.

---

### Fase 4–6 — CRUD Roles, Permissions, Users (sin cambios estructurales)

Mismo recordatorio: dispatcher propio + Mapperly/manual.

---

### Fase 7 — Endurecimiento de autorización (modificada)

Cambios:

- **7.1 Invalidación de cache de permisos** → reemplazar todo el `MemoryCache` por **`HybridCache`** desde el día 1.
  - Configurar solo L1 en `appsettings.json` por defecto: `"HybridCache": { "MaximumPayloadBytes": 1024, "DefaultEntryOptions": { "Expiration": "00:10:00" } }`.
  - El día que el equipo deploye Redis, agregar `services.AddStackExchangeRedisCache(...)` y `HybridCache` lo usa automáticamente como L2.
  - Adiós migración futura a Redis.
- **7.2 Claims de permisos en JWT** → mantener fuera del MVP, igual que el plan original.
- **7.3 Endpoint `me`** → sin cambios.

---

### Fase 8 — BuildingBlocks (modificada)

Cambios:

- **8.3 `IUnitOfWork` genérico** → **eliminar**. Si se aceptó `IExecutionContext`, esa interfaz vive en `BuildingBlocks.SharedKernel/Persistence/IExecutionContext.cs`. Cada módulo trae su implementación.
- **NUEVO 8.5 Dispatcher propio en BuildingBlocks**: si se eligió Opción B en Fase 0, los `IRequest<T>`, `IRequestHandler<,>`, `IPipelineBehavior<,>` y el `Sender` viven en `BuildingBlocks.SharedKernel/Messaging/` (interfaces) y `BuildingBlocks.Infrastructure/Messaging/` (implementación). Documentar en `Standards.md`.

---

### Fase 9 — Tests (modificada)

Cambios:

- **9.1 Unit tests** → usar `Shouldly` o `AwesomeAssertions` (lo que se haya elegido en Fase 0).
- **9.2 Integration tests** → **NO usar `Microsoft.EntityFrameworkCore.InMemory`**.
  - Por defecto: **SQLite in-memory** (`:memory:`) en `WebApiFactory`. Quitar `Microsoft.EntityFrameworkCore.InMemory` de `Directory.Packages.props` y reemplazar por `Microsoft.EntityFrameworkCore.Sqlite`.
  - **Opcional para CI nightly**: una segunda colección de tests con `Testcontainers.MsSql` que levanta SQL Server real y corre las migraciones. Etiquetar con `[Trait("Category", "RealDb")]` para excluir del run rápido.
- **9.3 Architecture tests** → ya cubierto en Fase 0.4. Confirmar que se añadieron las reglas para los paquetes nuevos.

---

### Fase 10 — DX y docs (modificada)

Cambios:

- **10.3** Actualizar:
  - `docs/01-Arquitectura.md` § 6 (tabla de "convenciones implementadas"): reemplazar AutoMapper / MediatR / FluentAssertions por sus reemplazos.
  - `docs/02-Instrucciones-IA.md`: ya cubierto en Fase 0.
  - `README.md`: si menciona alguno de los 3 paquetes.

---

### Fase 11 — Microservicios (sin cambios)

> Solo nota: con `HybridCache` desde Fase 7, el paso "reemplazar MemoryCache por Redis" desaparece — solo se conecta Redis como L2 y listo.

---

## 5. Antes / Después comparativo

| Aspecto | Antes (Plan original) | Después (Plan revisado) |
|---|---|---|
| Mapping | AutoMapper 16.x (comercial) | Mapperly o manual (MIT) |
| CQRS dispatcher | MediatR 14.x (comercial) | Dispatcher propio o Mediator (Othamar) (MIT) |
| Aserciones de test | FluentAssertions 8.x (comercial) | Shouldly o AwesomeAssertions (MIT) |
| Persistencia en handlers | `IUserRepository` + `IUnitOfWork` (wrapper redundante) | `IUserRepository` + `IExecutionContext.CommitAsync()` (commit explícito, sin pretender independencia de ORM) |
| Cache de permisos | `MemoryCache` (no escala) | `HybridCache` (L1 hoy, L2 Redis cuando se necesite) |
| Tests de integración | `EF.InMemory` (desaconsejado) | SQLite in-memory + Testcontainers en CI nightly |
| Hashing de password | BCrypt directo | BCrypt detrás de `IPasswordHasher` (= plan original 1.5, sin cambio) |
| Coste de licencias | ~3 paquetes que requieren licencia comercial | $0 |

---

## 6. Riesgos y consideraciones

- **Migrar MediatR cuando ya hay 30+ handlers** es una refactorización no trivial (interfaces, registros, tests). Hacerlo **ahora** (con 0 handlers reales) cuesta horas; hacerlo en 6 meses cuesta semanas.
- **Mapperly genera código en build**: la primera vez que se compila requiere `dotnet restore` + `dotnet build`. Hay que documentarlo en el README para que devs nuevos no se confundan con errores de "tipo no encontrado" antes del primer build.
- **Shouldly tiene API distinta** a FluentAssertions; si el equipo viene de FA, hay curva. **AwesomeAssertions es drop-in** (mismo nombre de métodos) → menor fricción si se acepta depender de un fork comunitario.
- **`IExecutionContext.CommitAsync()`** es ergonómicamente menos elegante que `await _unitOfWork.SaveChangesAsync()`, pero **honesto**: dice "commit", no pretende ser un patrón de Domain-Driven Design.

---

## 7. Próximos pasos sugeridos

1. **Tú:** revisar este documento y responder las 6 decisiones de la sección §3.
2. **Yo (siguiente sesión):** ejecutar **Fase 0 completa** (saneamiento de dependencias) según las decisiones tomadas, dejando los Architecture Tests verdes y `dotnet build && dotnet test` en verde.
3. **Tú:** validar la Fase 0 con un PR pequeño y enfocado.
4. **Yo:** continuar con la Fase 1 ya revisada.

> Si prefieres que arranque ya con mis recomendaciones por defecto (Opción B + Mapperly + AwesomeAssertions + Patrón B + HybridCache + SQLite), dilo y arranco. Si quieres revisar primero, este doc queda como referencia.
