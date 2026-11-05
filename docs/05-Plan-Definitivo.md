# Documento 5 — Plan Definitivo de Implementación

> **Audiencia:** desarrollador (humano o IA) responsable de terminar la plantilla `Camilo.OrderFlow`.
> **Estado:** **EJECUTADO — fases 0..10 completas** (2026-05-08). Documento **histórico**: se conserva como evidencia del proceso. La conciliación siguiente reemplaza las afirmaciones indicadas; las instrucciones actuales del entorno y usuario prevalecen. Este documento no autoriza ejecutar de nuevo sus fases ni su prompt histórico.
> **Documentos de soporte:**
> - [`01-Arquitectura.md`](01-Arquitectura.md) — qué es la plantilla y qué reglas estructurales no se rompen.
> - [`02-Instrucciones-IA.md`](02-Instrucciones-IA.md) — convenciones de código, nomenclatura, capas.
>
> Los documentos `03-Plan-Implementacion.md` y `04-Analisis-Critico.md` que este plan citaba fueron
> eliminados al cerrar el hand-off; su contenido sigue en la historia de git. Los números 03 y 04
> los ocupan hoy las guías de puesta en marcha y de renombrado.

---

## 1. Decisiones tomadas (no negociables)

### Conciliación vigente — 5 de octubre de 2026

- **D4 reemplazada:** repositorios sin `SaveChangesAsync`; el handler confirma mediante `IIdentityExecutionContext.CommitAsync` (o el contexto específico del módulo). `IdentityExecutionContext` delega a EF. La excepción single-entity de § 3 es histórica y queda reemplazada por esta regla y `Standards.md`.
- **D6 / fase 9 / checklist reemplazados:** `.github/workflows/ci.yml` dispara en push/PR a main/develop y ejecuta tests sin filtro, incluidos RealDb con Docker; no existe schedule nightly.
- **D5 / fase 7:** Redis configura L2 compartida, pero no acredita revocación inmediata multiinstancia. Las pruebas de invalidación actuales usan una sola WebApiFactory; verificar propagación por clave/tag y medir ventana pendiente.
- **D1–D3 y fase 0:** cifras de líneas, velocidad (~10x), ahorro, licencias y objetivo “100% OSS” son motivación histórica no auditada en esta fase, no resultados actuales ni certificación de licencias. No se cambian paquetes ni versiones.
- Comandos de tests históricos `--filter` de VSTest quedan reemplazados por `dotnet test --no-build --no-restore -- --filter-not-trait "Category=RealDb" --ignore-exit-code 8` y, para SQL, `--filter-trait`. Un proyecto filtrado sin casos no valida SQL. Consultar la guía de arranque vigente.
- Los permisos usan constantes existentes como `IdentityPermissions.RolesRead`; los literales del plan son antecedentes. Nuevos permisos hipotéticos requieren su propia clase/seed.
- `global.json` selecciona Microsoft Testing Platform sin fijar SDK; CI usa 10.0.x y publica TRX/cobertura sin umbral ni auditoría dedicada. Paquetes centrales no garantizan por sí solos resolución bloqueada.
- Las rutas de documentos eliminados, duraciones y checklist de construcción siguientes pertenecen al snapshot histórico. No describen aceptación actual del producto.

| # | Decisión | Justificación corta |
|---|---|---|
| **D1** | **Dispatcher propio** en `BuildingBlocks.SharedKernel/Messaging/` (≈50 LoC) reemplaza a MediatR | Quita licencia comercial; control total; no añade dependencia externa |
| **D2** | **Mapperly** (source-generator, MIT) reemplaza a AutoMapper. Mapeos con lógica compleja → **manuales** (extension methods) | Compile-time, ~10x más rápido, sin licencia |
| **D3** | **Shouldly** (MIT) reemplaza a FluentAssertions en todos los proyectos de test | Sin licencia; mensajes de error claros |
| **D4** | **Patrón B**: repositorios delgados que llaman `SaveChangesAsync` cuando aplique + `IExecutionContext.CommitAsync()` para operaciones multi-repo. **No** se introduce `IUnitOfWork` | `DbContext` ya es UoW; abstracción honesta |
| **D5** | **`HybridCache` desde el día 1** para permisos. L1 in-memory por defecto, L2 (Redis) opcional documentado. **`MemoryCache` queda eliminado** del `PermissionService`. TTL configurable. Invalidación por `userId` y por `roleId` | Escala a multi-instancia el día 1; transición a Redis = 1 línea de DI |
| **D6** | Tests de integración con **SQLite in-memory** por defecto (rápido). **Testcontainers + SQL Server** en una colección aparte etiquetada `[Trait("Category","RealDb")]` para CI nightly | EF.InMemory está desaconsejado por Microsoft; SQLite mantiene velocidad; Testcontainers da fidelidad cuando hace falta |
| **D7** *(heredada del plan original)* | CQRS por convención (Commands/Queries + Handlers + Validators) | Ya documentado en `02-Instrucciones-IA.md` |
| **D8** *(heredada)* | Excepciones de dominio (no Result pattern) | Consistencia con lo ya implementado |
| **D9** *(heredada)* | Autorización por permiso (`[AuthorizePermission]`), no por rol | Política de la organización |
| **D10** *(heredada)* | `BCrypt.Net-Next 4.1.0` detrás de `IPasswordHasher` | OK para MVP; reemplazo natural a Argon2 sin tocar handlers |

> **Cualquier desviación de estas decisiones requiere revisar este documento explícitamente, no se decide en code review.**

---

## 2. Estado del repositorio al iniciar (snapshot)

- Estructura Clean Arch + Modular Monolith ✅
- Identity con `User`, `Role`, `Permission`, `UserRole`, `RolePermission` (Domain) ✅
- Migración inicial + seeder de admin ✅
- JWT funcionando (`AuthService.LoginAsync`) ✅
- `[AuthorizePermission]` con cache `MemoryCache` (10 min) — **será reemplazado por HybridCache**
- `GlobalExceptionHandler` → `ProblemDetails` ✅
- Observabilidad (OTel + Serilog), correlation, health, rate limiting, security headers, resiliencia HTTP ✅
- Tests de arquitectura + 1 unit test + 1 integration test ✅
- **Pendiente:** Commands/Queries, repos, DTOs, Mapping, casi todos los endpoints (register, refresh, CRUD users/roles/permissions, me)

Paquetes instalados que **se eliminan en Fase 0**:
- `AutoMapper 16.1.0` (comercial)
- `MediatR 14.1.0` (comercial)
- `FluentAssertions 8.8.0` (comercial)
- `Microsoft.EntityFrameworkCore.InMemory 10.0.3` (desaconsejado)

Paquetes que **se agregan en Fase 0**:
- `Riok.Mapperly` (última estable)
- `Shouldly` (última estable)
- `Microsoft.EntityFrameworkCore.Sqlite 10.0.3`
- `Microsoft.Extensions.Caching.Hybrid` (última estable, bundled con .NET 10)
- `Testcontainers.MsSql` (en proyecto de tests CI nightly — Fase 9)

---

## 3. Plan ordenado de fases

### Reglas que aplican durante todo el plan

1. Después de **cada fase** debe pasar `dotnet build && dotnet test` en verde (incluyendo Architecture Tests).
2. Cada permiso nuevo se agrega a `IdentityDataSeeder` y se documenta aquí.
3. Cada setting nuevo va a `appsettings.json` con default seguro y, si es secreto, a `appsettings.Development.json.example`.
4. **Las versiones de NuGet solo se tocan en `Directory.Packages.props`.**
5. **Fase 0 es bloqueante.** Ningún Command/Query nuevo se escribe contra MediatR/AutoMapper.
6. **Si surge una decisión arquitectónica no cubierta, parar y preguntar al usuario.**

---

### Fase 0 — Saneamiento de dependencias **(BLOQUEANTE)**

> **Objetivo:** dejar la plantilla 100% bajo licencias OSS permisivas y con un dispatcher CQRS propio antes de implementar lógica nueva.

#### 0.1 Crear el dispatcher propio en BuildingBlocks

Archivos a crear:

- `src/BuildingBlocks/Camilo.OrderFlow.BuildingBlocks.SharedKernel/Messaging/IRequest.cs`
  ```csharp
  public interface IRequest<TResponse> { }
  public interface IRequest : IRequest<Unit> { }
  public readonly record struct Unit { public static readonly Unit Value = new(); }
  ```
- `src/BuildingBlocks/Camilo.OrderFlow.BuildingBlocks.SharedKernel/Messaging/IRequestHandler.cs`
  ```csharp
  public interface IRequestHandler<in TRequest, TResponse>
      where TRequest : IRequest<TResponse>
  {
      Task<TResponse> Handle(TRequest request, CancellationToken ct);
  }
  ```
- `src/BuildingBlocks/Camilo.OrderFlow.BuildingBlocks.SharedKernel/Messaging/IPipelineBehavior.cs`
  ```csharp
  public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
  public interface IPipelineBehavior<in TRequest, TResponse>
      where TRequest : IRequest<TResponse>
  {
      Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct);
  }
  ```
- `src/BuildingBlocks/Camilo.OrderFlow.BuildingBlocks.SharedKernel/Messaging/ISender.cs`
  ```csharp
  public interface ISender
  {
      Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default);
  }
  ```
- `src/BuildingBlocks/Camilo.OrderFlow.BuildingBlocks.Infrastructure/Messaging/Sender.cs`
  - Resuelve `IRequestHandler<TRequest, TResponse>` vía `IServiceProvider`.
  - Compone los `IPipelineBehavior<,>` registrados, en orden de registro, alrededor del handler.
  - Cachea el delegate compilado por `(TRequest, TResponse)` para evitar reflection en cada call.
- `src/BuildingBlocks/Camilo.OrderFlow.BuildingBlocks.Infrastructure/Messaging/MessagingServiceCollectionExtensions.cs`
  ```csharp
  public static IServiceCollection AddMessaging(this IServiceCollection services, params Assembly[] assembliesToScan);
  // Registra ISender + escanea handlers (IRequestHandler<,>) y los registra como Scoped.
  ```

#### 0.2 Eliminar AutoMapper

- `Directory.Packages.props`: borrar `AutoMapper 16.1.0`.
- `src/Modules/Identity/Camilo.OrderFlow.Modules.Identity.Application/Camilo.OrderFlow.Modules.Identity.Application.csproj`: borrar el `<PackageReference>`.
- `Modules.Identity.Application/DependencyInjection/ServiceCollectionExtensions.cs`: quitar `services.AddAutoMapper(...)`.
- Agregar `Riok.Mapperly` a `Directory.Packages.props` y referenciarlo en `Modules.Identity.Application.csproj`.

#### 0.3 Eliminar MediatR

- `Directory.Packages.props`: borrar `MediatR 14.1.0`.
- `Modules.Identity.Application.csproj`: borrar el `<PackageReference>` y agregar referencia a `BuildingBlocks.SharedKernel`.
- `Modules.Identity.Application/Behaviors/ValidationPipelineBehavior.cs`: reescribir contra `IPipelineBehavior<,>` propio. Mantiene la misma semántica (FluentValidation + lanzar `ValidationException`).
- `Modules.Identity.Application/DependencyInjection/ServiceCollectionExtensions.cs`: quitar `AddMediatR(...)`. Reemplazar por `services.AddMessaging(typeof(...).Assembly)` y registrar el behavior con `services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>))`.
- En `Program.cs`, asegurar que `BuildingBlocks` se registra (`services.AddBuildingBlocks()` si aplica para timing/messaging compartido).

#### 0.4 Eliminar FluentAssertions

- `Directory.Packages.props`: reemplazar `FluentAssertions 8.8.0` por `Shouldly` (última estable).
- `tests/Camilo.OrderFlow.Identity.UnitTests/Camilo.OrderFlow.Identity.UnitTests.csproj`: cambiar el `<PackageReference>`.
- `tests/Camilo.OrderFlow.WebApi.IntegrationTests/Camilo.OrderFlow.WebApi.IntegrationTests.csproj`: idem si lo usaba.
- Reescribir los asserts de `Should().X()` a `ShouldX()` (son pocos hoy).

#### 0.5 Reemplazar `EF.InMemory` por SQLite

- `Directory.Packages.props`: borrar `Microsoft.EntityFrameworkCore.InMemory`.
- Agregar `Microsoft.EntityFrameworkCore.Sqlite 10.0.3`.
- En `tests/Camilo.OrderFlow.WebApi.IntegrationTests`: si hay un `WebApiFactory`, sustituir el `UseInMemoryDatabase(...)` por SQLite con conexión `:memory:` mantenida abierta durante toda la fixture (necesario para que el schema sobreviva).

#### 0.6 Architecture Tests

`tests/Camilo.OrderFlow.ArchitectureTests/CleanArchitectureTests.cs`:

- Eliminar reglas que mencionen `AutoMapper` o `MediatR`.
- Añadir:
  - **`Application` no debe referenciar `AutoMapper.*`** (defensivo).
  - **`Application` no debe referenciar `MediatR.*`** (defensivo).
  - **`Domain` no debe referenciar `Riok.Mapperly`** ni el namespace `*.Messaging`.

#### 0.7 Documentación

Actualizar (búsqueda y reemplazo cuidadoso):

- `docs/01-Arquitectura.md` § 6 (tabla de convenciones): cambiar AutoMapper / MediatR por sus reemplazos.
- `docs/02-Instrucciones-IA.md`:
  - § 3.3 (tabla de archivos): mantener `<Module>MappingProfile.cs` como **opcional** y agregar fila `Mapper de Mapperly: <Entity>Mapper.cs (partial)`.
  - § 4 (tabla de patrones): reemplazar fila MediatR por "Dispatcher propio (`ISender` en `BuildingBlocks.SharedKernel.Messaging`)".
  - § 8 (tests): cambiar ejemplo de `.Should()` a `.ShouldX()`.
  - § 13 (resumen): cambiar "MediatR / AutoMapper / FluentAssertions" por "dispatcher propio / Mapperly o manual / Shouldly".
- `docs/Standards.md`: si menciona alguno de los 3 paquetes, actualizar.
- `README.md`: idem.

#### 0.8 Verificación de Fase 0

```pwsh
dotnet restore
dotnet build /warnaserror
dotnet test
```

- `dotnet list package` no debe mostrar `AutoMapper`, `MediatR` ni `FluentAssertions` en ningún proyecto.
- Architecture Tests verdes.
- Solución compila y todos los tests existentes pasan.

✅ **Salida de Fase 0:** la plantilla queda 100% OSS y con el dispatcher propio listo para escribir Commands/Queries reales.

---

### Fase 1 — Cimientos transversales

#### 1.1 `ICurrentUser`

- `Modules.Identity.Application/Abstractions/ICurrentUser.cs`: `Guid? UserId`, `string? UserName`, `bool IsAuthenticated`, `IReadOnlyCollection<string> Permissions` (opcional).
- `WebApi/Infrastructure/CurrentUser.cs`: implementación con `IHttpContextAccessor`.
- Registrar en `Program.cs` como `Scoped`.

#### 1.2 `IDateTimeProvider`

- `BuildingBlocks.SharedKernel/Time/IDateTimeProvider.cs` con `DateTime UtcNow`.
- `BuildingBlocks.Infrastructure/Time/SystemDateTimeProvider.cs`.
- Registrar en `BuildingBlocks.Infrastructure` con `services.AddBuildingBlocks()` si no existe; si existe, agregar ahí.

#### 1.3 `IExecutionContext` (reemplaza la idea de IUnitOfWork)

- `BuildingBlocks.SharedKernel/Persistence/IExecutionContext.cs`:
  ```csharp
  public interface IExecutionContext
  {
      Task<int> CommitAsync(CancellationToken ct = default);
  }
  ```
- Cada módulo provee su implementación. Para Identity:
  - `Modules.Identity.Infrastructure/Persistence/IdentityExecutionContext.cs`: wrapper sobre `IdentityDbContext.SaveChangesAsync`.
  - Registrado en `AddIdentityInfrastructure` como `Scoped`.

> **Convención:** los repositorios **NO** llaman `SaveChangesAsync`. Lo llama el handler al final, vía `IExecutionContext.CommitAsync()`. Esto deja claro el límite transaccional del caso de uso. Excepción: para casos de uso triviales single-entity, el repositorio puede tener un método `AddAsync(entity, ct)` que sí commitee — documentarlo explícitamente.
>
> **Decisión:** **no** mezclar ambos estilos. Para esta plantilla, repos delgados + commit explícito por handler. Single-style.

#### 1.4 Repositorios

Interfaces en `Modules.Identity.Application/Abstractions/`:

- `IUserRepository`: `GetByIdAsync`, `GetByEmailAsync`, `GetByUserNameAsync`, `EmailExistsAsync`, `Add`, `Update`, `Remove`, `Query` (devuelve `IQueryable<User>` con `AsNoTracking` por defecto).
- `IRoleRepository`: análogo.
- `IPermissionRepository`: análogo.
- `IRefreshTokenRepository`: para Fase 3.3.

Implementaciones en `Modules.Identity.Infrastructure/Persistence/Repositories/`. Registrar en `AddIdentityInfrastructure`.

#### 1.5 `IPasswordHasher`

- `Modules.Identity.Application/Abstractions/IPasswordHasher.cs` (`Hash(string)`, `Verify(string, string)`).
- `Modules.Identity.Infrastructure/Auth/BcryptPasswordHasher.cs`.
- Refactorizar `AuthService` y `IdentityDataSeeder` para usarlo en lugar de `BC.HashPassword`/`BC.Verify`.

#### 1.6 Mapping con Mapperly

- `Modules.Identity.Application/Mapping/UserMapper.cs`:
  ```csharp
  [Mapper]
  public partial class UserMapper
  {
      public partial UserDto ToDto(User user);
  }
  ```
- `RoleMapper`, `PermissionMapper`: análogos.
- Convención: registrarlos como `Singleton` (no tienen estado).

#### 1.7 DTOs

- `Modules.Identity.Application/Dtos/UserDto.cs` (incluye `ProfileDto` cuando exista en Fase 2).
- `Modules.Identity.Application/Dtos/RoleDto.cs`.
- `Modules.Identity.Application/Dtos/PermissionDto.cs`.
- `Modules.Identity.Application/Dtos/PagedResult.cs` se mueve a Fase 8 — por ahora no.

#### 1.8 Verificación de Fase 1

`dotnet build && dotnet test` verdes. La capa Application tiene los building blocks listos pero **no** se ha tocado lógica de negocio. Se debe poder reemplazar la implementación de cualquier repo sin tocar handlers (porque aún no hay handlers).

---

### Fase 2 — UserProfile (VO embebido)

> Sin cambios respecto al `03-Plan-Implementacion.md` § 2. Resumen: `UserProfile` value object con `FirstName`, `LastName`, `PhoneNumber?`, `AvatarUrl?`. `User.UpdateProfile(...)`. EF Core `OwnsOne`. Migración `AddUserProfile`. Seeder asigna profile al admin. Tests de dominio.

---

### Fase 3 — Auth completo

#### 3.1 Register

- Command + Validator + Handler.
- `RegisterUserCommandValidator`: email válido, password ≥ 8 chars con mayúscula y número, `UserName` 3-50 chars.
- Handler: `IUserRepository.EmailExistsAsync` → `ConflictException`; crea `User`; opcionalmente asigna rol "User" por defecto (si existe; si no, ignora).
- Endpoint `POST /api/auth/register`, `[EnableRateLimiting("auth")]`, sin permiso.

#### 3.2 Change Password

- `ChangePasswordCommand` (`CurrentPassword`, `NewPassword`).
- Handler: usa `ICurrentUser.UserId`, valida con `IPasswordHasher.Verify`, llama `User.UpdatePasswordHash`.
- Endpoint `POST /api/auth/change-password` autenticado.

#### 3.3 Refresh Tokens

- Entidad `RefreshToken` en Domain (`Id`, `UserId`, `TokenHash`, `ExpiresAtUtc`, `RevokedAtUtc?`, `CreatedByIp`, `ReplacedByTokenId?`).
- Configuration EF + migración `AddRefreshTokens`.
- `IRefreshTokenRepository`.
- `AuthService.LoginAsync`: emite también un refresh token. **Decisión:** retornar refresh token en cookie HttpOnly + Secure + SameSite=Strict. El access token (JWT) sí va en el body.
- Endpoints `POST /api/auth/refresh` (rota: revoca el viejo, emite uno nuevo) y `POST /api/auth/logout` (revoca el actual).

#### 3.4 Lockout

- Campos en `User`: `FailedLoginAttempts`, `LockoutEndUtc?`.
- En `AuthService.LoginAsync`: incrementa contador; tras N intentos (configurable, default 5), bloquea por X minutos (default 15).
- Migración `AddUserLockout`.

#### 3.5 Tests

- Unit tests: handlers Register / ChangePassword (camino feliz, ConflictException, ValidationException).
- Integration tests: `/api/auth/register`, `/api/auth/login` (válido + inválido + lockout), `/api/auth/change-password`, `/api/auth/refresh`, `/api/auth/logout`.

---

### Fase 4 — CRUD de Roles

> Sin cambios estructurales respecto al `03` § 4. Recordatorio: usar el dispatcher propio (`IRequest<T>` + `IRequestHandler<,>`), Mapperly para DTOs, repos delgados + `IExecutionContext.CommitAsync()`.

Endpoints en `RolesController`:

- `GET /api/roles` — `[AuthorizePermission("roles.read")]`
- `GET /api/roles/{id}` — `roles.read`
- `POST /api/roles` — `roles.write`
- `PUT /api/roles/{id}` — `roles.write`
- `DELETE /api/roles/{id}` — `roles.write` (no permitir borrar rol "Admin")
- `PUT /api/roles/{id}/permissions` — `roles.write` (asignación masiva, reemplaza el set)
  - **Tras la asignación, invalidar cache de permisos** de todos los usuarios con ese rol (Fase 7).

---

### Fase 5 — CRUD de Permisos

> Sin cambios estructurales respecto al `03` § 5.

Endpoints en `PermissionsController`:

- `GET /api/permissions` — `permissions.read`
- `POST /api/permissions` — `permissions.write` (validar formato `<recurso>.<accion>`)
- `PUT /api/permissions/{id}` — `permissions.write` (solo `Name`, no `Code`)
- `DELETE /api/permissions/{id}` — `permissions.write` (solo si no asignado a ningún rol)

Permisos nuevos al seed: `permissions.read`, `permissions.write` → asignados al rol Admin.

---

### Fase 6 — Gestión de Usuarios

> Sin cambios estructurales respecto al `03` § 6. **Borrar** los stubs `TestNotFound` y `TestConflict` de `UsersController`.

Endpoints en `UsersController`:

- `GET /api/users` — `users.read`, paginado (`?page=1&pageSize=20&search=...`).
- `GET /api/users/{id}` — `users.read`.
- `POST /api/users` — `users.write` (admin crea usuario).
- `PUT /api/users/{id}/profile` — auto-edición (validando `id == ICurrentUser.UserId`) **o** `users.write`.
- `POST /api/users/{id}/deactivate` — `users.write` (regla: no desactivar último Admin).
- `POST /api/users/{id}/activate` — `users.write`.
- `PUT /api/users/{id}/roles` — `users.write` (invalida cache de permisos para `id`).
- `GET /api/users/me` — autenticado, sin permiso.
- `GET /api/users/me/permissions` — autenticado, devuelve permisos efectivos (cache hit).

---

### Fase 7 — HybridCache + invalidación

#### 7.1 Migración del PermissionService a HybridCache

- Quitar `IMemoryCache` del `PermissionService`.
- Agregar `Microsoft.Extensions.Caching.Hybrid` a `Directory.Packages.props`.
- En `Program.cs` (o donde se registre Identity Infrastructure):
  ```csharp
  services.AddHybridCache(options =>
  {
      options.DefaultEntryOptions = new HybridCacheEntryOptions
      {
          Expiration = TimeSpan.FromMinutes(config.GetValue<int?>("Permissions:CacheTtlMinutes") ?? 10),
          LocalCacheExpiration = TimeSpan.FromMinutes(config.GetValue<int?>("Permissions:LocalCacheTtlMinutes") ?? 5)
      };
  });
  ```
- `appsettings.json` añadir sección:
  ```json
  "Permissions": {
    "CacheTtlMinutes": 10,
    "LocalCacheTtlMinutes": 5
  }
  ```

#### 7.2 Invalidación

- `PermissionService` expone:
  - `Task InvalidateUserAsync(Guid userId, CancellationToken ct)` → `HybridCache.RemoveAsync($"perms:user:{userId}")`.
  - `Task InvalidateRoleAsync(Guid roleId, CancellationToken ct)` → invalida tags `role:{roleId}` (cuando se asignen tags por rol al cachear).
  - Las claves de cache deben usar tags: `[$"role:{roleId}"]` para que `RemoveByTagAsync` invalide a todos los usuarios afectados al cambiar permisos de un rol.
- Llamar invalidación en handlers que mutan: `AssignRolesToUserCommand`, `AssignPermissionsToRoleCommand`, `DeleteRoleCommand`, `DeletePermissionCommand`, `UpdatePermissionCommand` (y cualquier otro que afecte el grafo `User → Role → Permission`).

#### 7.3 Redis L2 (documentado, NO activado por defecto)

- En `appsettings.json` agregar key vacío `"Cache:Redis": null`.
- En `appsettings.Development.json.example` documentar:
  ```json
  "Cache": { "Redis": "localhost:6379" }
  ```
- En `Program.cs`, si `Cache:Redis` está presente:
  ```csharp
  services.AddStackExchangeRedisCache(o => o.Configuration = config["Cache:Redis"]);
  ```
  → `HybridCache` lo detecta y lo usa como L2 automáticamente.
- Documentar en `README.md` y `Standards.md` que **producción multi-instancia REQUIERE Redis configurado**, de lo contrario los caches de cada réplica se desincronizan.

#### 7.4 Tests

- Integration test: `PUT /api/users/{id}/roles` → siguiente request del usuario afectado refleja los nuevos permisos sin esperar al TTL.
- Test que verifica: TTL configurado en `appsettings` se respeta.

---

### Fase 8 — BuildingBlocks reutilizables

#### 8.1 PagedResult

- `BuildingBlocks.SharedKernel/Pagination/PagedResult.cs`: `Items`, `TotalCount`, `Page`, `PageSize`, `HasNext`, `TotalPages`.
- `BuildingBlocks.SharedKernel/Pagination/PagedRequest.cs`: base con `Page`, `PageSize` y validación (page ≥ 1, pageSize 1..100).

#### 8.2 ~~`IUnitOfWork` genérico~~ — eliminado por D4

`IExecutionContext` ya está en `BuildingBlocks.SharedKernel/Persistence/`.

#### 8.3 Documentar el patrón completo de módulo en `Standards.md`

Cubrir:

- Registración DI de un módulo.
- Cómo añadir una entidad, un command, un query, un endpoint.
- Cómo invalidar cache de permisos cuando se mutan datos relevantes.
- Cuándo crear un repo vs cuándo inyectar `DbContext` directo (NUNCA, en esta plantilla).
- Cómo escribir un test de handler (con repos fakeados) y un integration test (con `WebApiFactory` + SQLite).

---

### Fase 9 — Cobertura de tests

#### 9.1 Unit tests

- Cada entidad y VO (`User`, `Role`, `Permission`, `UserRole`, `RolePermission`, `UserProfile`, `RefreshToken`): factory, validaciones, transiciones.
- Cada handler de Command/Query: camino feliz + 1-2 errores. Mockear repos con **NSubstitute** (decisión: NSubstitute, no Moq, para mantener un único framework de mock — añadir a `Directory.Packages.props`).
- Asserts con **Shouldly**.

#### 9.2 Integration tests

- `WebApiFactory` con SQLite `:memory:` (mantener una conexión abierta durante toda la fixture; reusar para todos los tests del fixture).
- Por cada endpoint: 1 caso feliz + 1 caso de error (400/401/403/404/409 según aplique).
- Suite separada `IntegrationTests.RealDb` con `Testcontainers.MsSql`:
  - Levanta SQL Server real, corre migraciones reales.
  - Etiquetada `[Trait("Category", "RealDb")]`.
  - Excluida del run rápido: `dotnet test --filter "Category!=RealDb"`.
  - CI nightly la ejecuta: `dotnet test --filter "Category=RealDb"`.

#### 9.3 Architecture tests (consolidar)

- `Domain` no referencia: `Microsoft.AspNetCore.*`, `Microsoft.EntityFrameworkCore.*`, `Riok.Mapperly.*`, `FluentValidation.*`, namespaces `*.Messaging`.
- `Application` no referencia: EF Core, ASP.NET Core, AutoMapper, MediatR (defensivo).
- `Modules.<X>` no referencia `Modules.<Y>` (aislamiento).

---

### Fase 10 — DX y documentación

#### 10.1 `appsettings.Development.json.example`

Asegurar que incluye placeholder para todos los settings: `ConnectionStrings:DefaultConnection`, `Jwt:*`, `OpenTelemetry:OtlpEndpoint`, `Cors:AllowedOrigins`, `Seed:Enabled`, `Permissions:CacheTtlMinutes`, `Cache:Redis`, `Lockout:*`.

#### 10.2 `Camilo.OrderFlow.WebApi.http`

Ejemplos para cada endpoint nuevo: register, login, refresh, logout, change-password, CRUD users/roles/permissions, asignación de roles, me, me/permissions.

#### 10.3 README + Standards + ReleaseChecklist

- `README.md`: actualizar con nuevos endpoints clave + pre-requisitos (`Mapperly` requiere build antes de IntelliSense; SQLite para tests rápidos; Redis para multi-instancia).
- `docs/Standards.md`: patrón completo de módulo (ver § 8.3).
- `docs/ReleaseChecklist.md`: añadir checks de "al menos un Admin", "permisos seed-ados", "Redis configurado si > 1 réplica".

#### 10.4 Renombrado de plantilla

Sección en `README.md` con instrucciones paso a paso (ver `03-Plan-Implementacion.md` § 10.4 — sin cambios).

---

### Fase 11 — Microservicios (FUTURO, no MVP)

Sin cambios respecto al `03`. Solo nota: con HybridCache desde Fase 7, la migración a Redis es un setting de config, no un refactor.

---

## 4. Reglas de oro durante toda la implementación

1. **Nunca empezar por el controller.** Orden: Domain → Application → Infrastructure → WebApi → Tests.
2. **No introducir librerías nuevas sin permiso explícito del usuario.** El stack está congelado en este documento.
3. **Versiones de NuGet solo en `Directory.Packages.props`.**
4. **Cada permiso nuevo va al seeder**, sin excepción.
5. **Las entidades de Domain NO se exponen en respuestas HTTP.** Mapping con Mapperly o manual.
6. **Las excepciones son la forma de manejar errores de negocio.** No introducir `Result<T>`.
7. **El handler llama `IExecutionContext.CommitAsync()` al final** (no el repo).
8. **`[AuthorizePermission("...")]`**, no `[Authorize(Roles="...")]`.
9. **Si los Architecture Tests fallan, el código está mal — no los tests.**
10. **Antes de cualquier cambio destructivo, estructural, o de stack, preguntar al usuario.**

---

## 5. Definición de "plantilla terminada"

- [ ] Fase 0 cierra: `dotnet list package` no muestra AutoMapper, MediatR, FluentAssertions, `EF.InMemory`.
- [ ] Endpoints implementados, autorizados, validados y testeados:
  - Auth: `register`, `login`, `change-password`, `refresh`, `logout`, lockout funcional.
  - Users: CRUD + `me`, `me/permissions`, asignar roles, activar/desactivar.
  - Roles: CRUD + asignar permisos.
  - Permissions: CRUD.
- [ ] Al menos 1 unit test por handler y 1 integration test por endpoint.
- [ ] `dotnet test --filter "Category!=RealDb"` pasa en verde.
- [ ] CI nightly con `Category=RealDb` configurado y verde.
- [ ] Architecture Tests cubren las reglas de la § 9.3.
- [ ] Swagger documenta cada endpoint con `[ProducesResponseType]` y bearer auth.
- [ ] HybridCache configurado, invalidación funcional, Redis documentado.
- [ ] El admin seed-ado puede: login → crear permiso, rol y usuario → asignar rol → el nuevo usuario logea y solo ve lo que su rol permite.
- [ ] `appsettings.Development.json.example` permite a un dev nuevo arrancar en < 15 min.
- [ ] `docs/01-05` consistentes con el código.

---

## 6. Prompt para Sonnet 4.6 (asistente de implementación)

> **Cómo usar este prompt:** pégalo como mensaje de sistema o como primer mensaje en una nueva conversación con Sonnet 4.6 dentro de este repositorio. El asistente leerá los documentos y empezará por la Fase 0.

```
Eres un asistente de implementación senior trabajando sobre el repositorio `Camilo.OrderFlow`,
una plantilla backend en .NET 10 con Clean Architecture + Modular Monolith. Tu único trabajo
en esta sesión es ejecutar el plan definido en `docs/05-Plan-Definitivo.md`, fase por fase,
sin desviaciones.

## Antes de tocar código (obligatorio)

Lee, en este orden, y trata su contenido como contrato:

1. `docs/05-Plan-Definitivo.md` — el plan que ejecutarás (este documento prevalece sobre el 03).
2. `docs/02-Instrucciones-IA.md` — convenciones de código, nomenclatura, capas, qué NO hacer.
3. `docs/01-Arquitectura.md` — visión estructural y reglas de dependencias.
4. `docs/04-Analisis-Critico.md` — solo como contexto histórico de por qué el plan cambió.

Si algo en otros archivos del repo (incluyendo `03-Plan-Implementacion.md`) contradice
`05-Plan-Definitivo.md`, **prevalece el `05`**.

## Cómo trabajar

- Las **decisiones D1–D10** de `05-Plan-Definitivo.md` § 1 son **inamovibles**. No las re-discutas
  ni propongas alternativas. Si crees que una decisión está mal, repórtalo al usuario en
  un mensaje y para — no toques código hasta tener su confirmación.
- Ejecuta **una fase a la vez**, en el orden listado (0 → 1 → 2 → … → 10). No empieces la
  fase N+1 sin que la N esté verificada.
- **Fase 0 es bloqueante.** No escribas un solo `IRequest` ni un solo `[Mapper]` antes de
  completarla.
- Después de cada fase ejecuta:
    dotnet restore
    dotnet build /warnaserror
    dotnet test --filter "Category!=RealDb"
  Y reporta el resultado al usuario antes de continuar.
- Las versiones de NuGet **solo** se modifican en `Directory.Packages.props`.
- Nunca introduzcas paquetes nuevos que no estén en `05-Plan-Definitivo.md`. Si necesitas
  uno (incluida cualquier dependencia transitiva inusual), pide permiso explícito.
- Sigue el orden de implementación obligatorio: Domain → Application → Infrastructure →
  WebApi → Tests. **Nunca empieces por el controller.**
- Para cada entidad/handler/endpoint nuevo, agrega también su test (unit y/o integration
  según aplique) en la misma fase. No "después".
- Cuando un permiso nuevo aparezca, agrégalo al `IdentityDataSeeder` en la misma PR.
- Las entidades de Domain NUNCA se exponen en respuestas HTTP. Usa Mapperly o mapeo
  manual con extension methods (D2).
- Errores de negocio = excepciones (`NotFoundException`, `ConflictException`, etc.). No
  introduzcas `Result<T>`.
- Logs estructurados con `ILogger<T>` y templates (`"User {UserId} created"`). Nunca
  `string.Format` ni interpolación.
- Nunca loguees passwords, tokens, claims sensibles ni headers `Authorization`/`Cookie`.

## Formato de tu output

- En cada turno: una breve descripción de lo que vas a hacer en esa fase/sub-fase, los
  archivos que crearás o editarás (con ruta relativa), y los resultados de `dotnet build`
  y `dotnet test` al cerrar la sub-fase.
- Si encuentras una decisión arquitectónica no cubierta por los docs (por ejemplo: cómo
  numerar versiones de migraciones cuando coexisten dos módulos, o cómo dividir un
  archivo `ServiceCollectionExtensions.cs` muy grande), **para y pregunta al usuario**.
  No improvises.
- Si los Architecture Tests fallan, el código nuevo está mal. Corrige el código, no los
  tests. Si crees que el test está obsoleto, repórtalo y espera confirmación.
- Mantén las respuestas concisas. El usuario no quiere prosa, quiere progreso verificable.

## Empezar

Confirma que has leído los 4 documentos listados arriba listando una idea clave de cada
uno (1 frase por doc). Después, propón el primer commit de la **Fase 0.1 (crear el
dispatcher propio en BuildingBlocks)** indicando exactamente qué archivos vas a crear y
con qué contenido. Espera confirmación antes de escribir.
```

---

## 7. Próximos pasos

1. **Tú:** abrir nueva conversación con Sonnet 4.6 en este repositorio, pegar el prompt de § 6.
2. **Sonnet 4.6:** ejecutar Fase 0 → reportar resultado de tests → esperar OK → Fase 1 → … → Fase 10.
3. **Tú (revisor):** validar cada fase con un PR pequeño y enfocado antes de aprobar la siguiente.
4. **Cuando los 11 checks de § 5 estén ✅,** la plantilla queda lista para usarse como base organizacional.
