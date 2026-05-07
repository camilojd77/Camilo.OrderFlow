# Documento 3 — Plan de implementación para completar la plantilla

> **Audiencia:** desarrolladores (humanos o asistidos por IA) que van a terminar `Company.Product` para que pueda usarse como **plantilla base** en cualquier proyecto backend de la organización que requiera login.
> **Objetivo:** lista paso a paso de lo que falta, ordenada por dependencias, para llegar a una plantilla "lista para clonar y arrancar".

---

## 0. Estado actual (resumen)

✅ **Ya está**:

- Estructura Clean Architecture + Modular Monolith.
- Entidades `User`, `Role`, `Permission`, `UserRole`, `RolePermission` en Domain.
- `IdentityDbContext` + migración inicial + seeder con admin.
- JWT (`AuthService`, `JwtTokenService`, `LoginRequest/Response`).
- Autorización por permisos (`[AuthorizePermission]`) con cache.
- `GlobalExceptionHandler` → `ProblemDetails`.
- MediatR + FluentValidation + AutoMapper registrados, con `ValidationPipelineBehavior`.
- Observabilidad (OTel, Serilog), correlation, health, rate limiting, CORS, security headers, resiliencia HTTP.
- Versionado de API + Swagger con bearer.
- Tests de arquitectura, unit (sample), integration (sample).
- CI básica en `.github/workflows/ci.yml`.

❌ **Falta** (es lo que cubre este plan):

- No hay Commands/Queries reales en `Application` — solo el pipeline behavior.
- No hay abstracciones (`IUserRepository`, `IUnitOfWork`, `ICurrentUser`, etc.).
- Solo existe `Login`. Falta **register, change password, refresh token, logout, profile, listado/CRUD de usuarios, CRUD de roles, asignación de roles, CRUD de permisos**.
- `User` no tiene perfil (nombre, apellido, etc.). El usuario pidió "perfiles".
- No hay AutoMapper Profile real ni DTOs de Identity.
- No hay seguridad adicional: lockout, refresh tokens, revocación.
- Tests cubren muy poco (un test de dominio, uno de health).

---

## 1. Convenciones que se aplican durante todo el plan

- Cada paso indica **qué archivo crear/editar** y la **carpeta exacta** según la convención del documento [`02-Instrucciones-IA.md`](02-Instrucciones-IA.md).
- Después de cada fase **debe pasar `dotnet test`** (incluyendo Architecture Tests).
- Cualquier permiso nuevo se **agrega al `IdentityDataSeeder`** y se documenta en este archivo.
- Cualquier setting nuevo se **agrega a `appsettings.json` con valor por defecto** y, si es secreto, a `appsettings.Development.json.example`.

---

## Fase 1 — Cimientos transversales (preparar el terreno)

> Estos son los building blocks que **toda** la siguiente lógica usará. Sin esto, los Commands/Queries quedarían acoplados al `DbContext`.

### 1.1 `ICurrentUser` (saber quién está autenticado)

**Por qué:** los handlers necesitan el `UserId` actual para audit, ownership checks, etc., sin depender de `HttpContextAccessor`.

- `Modules.Identity.Application/Abstractions/ICurrentUser.cs` — interfaz: `Guid? UserId { get; }`, `string? UserName { get; }`, `bool IsAuthenticated { get; }`.
- `WebApi/Infrastructure/CurrentUser.cs` — implementación basada en `IHttpContextAccessor` y los claims del JWT (`ClaimTypes.NameIdentifier`).
- Registrar en `Program.cs`: `services.AddScoped<ICurrentUser, CurrentUser>();`.

### 1.2 `IDateTimeProvider`

**Por qué:** evita acoplar el dominio a `DateTime.UtcNow` y permite testear con tiempos congelados.

- `BuildingBlocks.SharedKernel/Time/IDateTimeProvider.cs` — interfaz `DateTime UtcNow { get; }`.
- `BuildingBlocks.Infrastructure/Time/SystemDateTimeProvider.cs` — `=> DateTime.UtcNow`.
- Registrar en `BuildingBlocks.Infrastructure` con un `AddBuildingBlocks()` extension.

### 1.3 `IUnitOfWork` y abstracción de persistencia por módulo

- `Modules.Identity.Application/Abstractions/IUnitOfWork.cs` — `Task<int> SaveChangesAsync(CancellationToken)`.
- `Modules.Identity.Infrastructure/Persistence/Context/IdentityDbContext.cs` — implementa `IUnitOfWork` (o se hace un wrapper).
- Registrar en `AddIdentityInfrastructure`.

### 1.4 Repositorios de Identity

- `Modules.Identity.Application/Abstractions/IUserRepository.cs` — `GetByIdAsync`, `GetByEmailAsync`, `GetByUserNameAsync`, `EmailExistsAsync`, `Add`, `Update`, `Remove`, `Query`.
- `Modules.Identity.Application/Abstractions/IRoleRepository.cs` — análogo.
- `Modules.Identity.Application/Abstractions/IPermissionRepository.cs` — análogo.
- Implementaciones en `Modules.Identity.Infrastructure/Persistence/Repositories/`.
- Registrar en `AddIdentityInfrastructure`.

### 1.5 `IPasswordHasher`

**Por qué:** hoy `AuthService` y `IdentityDataSeeder` llaman directo a `BCrypt`. Hay que centralizar para poder cambiar el algoritmo sin tocar handlers.

- `Modules.Identity.Application/Abstractions/IPasswordHasher.cs` — `string Hash(string)`, `bool Verify(string password, string hash)`.
- `Modules.Identity.Infrastructure/Auth/BcryptPasswordHasher.cs` — implementación.
- Reemplazar uso directo de `BCrypt` en `AuthService` y `IdentityDataSeeder` por la interfaz.

### 1.6 AutoMapper Profile inicial

- `Modules.Identity.Application/Mapping/IdentityMappingProfile.cs` — mapeo `User → UserDto`, `Role → RoleDto`, `Permission → PermissionDto`.

### 1.7 DTOs de Identity

- `Modules.Identity.Application/Dtos/UserDto.cs`
- `Modules.Identity.Application/Dtos/RoleDto.cs`
- `Modules.Identity.Application/Dtos/PermissionDto.cs`

✅ Al final de la fase 1, `dotnet build` y `dotnet test` deben pasar (no se ha tocado lógica todavía).

---

## Fase 2 — Perfil de usuario y refactor de la entidad `User`

> El usuario pidió "Usuarios, roles, perfiles, permisos". Hoy `User` solo tiene `Email`, `UserName`, `PasswordHash`, `IsActive`. Falta el perfil.

### 2.1 Decidir: VO vs entidad separada

Recomendación: **value object embebido `UserProfile`** dentro de `User` (campos: `FirstName`, `LastName`, `PhoneNumber?`, `AvatarUrl?`). Si el negocio no requiere historial de cambios de perfil, no se justifica una tabla aparte.

### 2.2 Implementación

- `Modules.Identity.Domain/ValueObjects/UserProfile.cs` (`record` con constructor validador).
- Modificar `User`:
  - Agregar `public UserProfile Profile { get; private set; }`.
  - Método `UpdateProfile(UserProfile newProfile)`.
- En `Infrastructure/Persistence/Configurations/UserConfiguration.cs`: configurar como `OwnsOne` (columnas `FirstName`, `LastName`, `PhoneNumber`, `AvatarUrl` en la tabla `Users`).

### 2.3 Migración

```bash
dotnet ef migrations add AddUserProfile \
  --project Company.Product.Modules.Identity.Infrastructure \
  --startup-project Company.Product.WebApi
```

### 2.4 Seed: actualizar el admin

Asignar un `UserProfile` por defecto al admin en `IdentityDataSeeder.SeedAdminUserAsync`.

✅ Tests: agregar test de dominio para `UserProfile` (validaciones) y `User.UpdateProfile`.

---

## Fase 3 — Casos de uso de Authentication (lo mínimo para login completo)

### 3.1 Register

- `Modules.Identity.Application/Features/Auth/Register/RegisterUserCommand.cs` — `Email`, `UserName`, `Password`, `FirstName`, `LastName`.
- `RegisterUserCommandValidator.cs` — email válido, password ≥ 8 chars con mayúscula/número, etc.
- `RegisterUserCommandHandler.cs` — usa `IUserRepository.EmailExistsAsync`, lanza `ConflictException` si existe; crea `User`; opcionalmente asigna rol "User" por defecto.
- Endpoint `POST /api/auth/register` en `AuthController` (sin permiso, rate-limited con `auth`).

> **Decisión a tomar:** ¿registro abierto o solo por invitación? Default sugerido: registro abierto + rol "User" por defecto. Si la org requiere invitación, la fase 6 (gestión de usuarios) lo cubre.

### 3.2 Change Password

- Command: `ChangePasswordCommand` (`CurrentPassword`, `NewPassword`).
- Handler: usa `ICurrentUser.UserId`, valida `CurrentPassword` con `IPasswordHasher.Verify`, actualiza con `User.UpdatePasswordHash`.
- Endpoint `POST /api/auth/change-password` (autenticado).

### 3.3 Refresh tokens

- Entidad nueva en Domain: `RefreshToken` (`Id`, `UserId`, `Token` hash, `ExpiresAtUtc`, `RevokedAtUtc?`, `CreatedByIp`, `ReplacedByTokenId?`).
- Configuración EF + migración `AddRefreshTokens`.
- `IRefreshTokenRepository`.
- Modificar `AuthService.LoginAsync` para emitir también un refresh token (devuelto en cookie HttpOnly **o** en el body — decidir; recomendación: cookie HttpOnly + Secure + SameSite=Strict).
- Endpoint `POST /api/auth/refresh` que rota el refresh token (revoca el viejo, emite uno nuevo + nuevo JWT).
- Endpoint `POST /api/auth/logout` que revoca el refresh token actual.

> Si la plantilla quiere mantenerse mínima al inicio, refresh tokens puede dejarse documentado como **fase opcional** y arrancar solo con JWT corto. Marcar con un TODO claro.

### 3.4 Lockout (opcional, recomendado)

- Campos en `User`: `FailedLoginAttempts`, `LockoutEndUtc?`.
- En `AuthService.LoginAsync`: incrementar contador; tras N intentos (configurable), bloquear por X minutos.
- Migración correspondiente.

✅ Tests: handlers de Register, ChangePassword (camino feliz + ConflictException + ValidationException). Integration tests para `/api/auth/register`, `/api/auth/login`, `/api/auth/change-password`.

---

## Fase 4 — CRUD de Roles

### 4.1 Application

- `Features/Roles/Create/CreateRoleCommand.cs` — `Name`, `Description?`.
- `Features/Roles/Update/UpdateRoleCommand.cs`.
- `Features/Roles/Delete/DeleteRoleCommand.cs`.
- `Features/Roles/Get/GetRoleByIdQuery.cs` → `RoleDto`.
- `Features/Roles/List/ListRolesQuery.cs` → `IReadOnlyList<RoleDto>`.
- Validators correspondientes.
- Handlers que usen `IRoleRepository` + `IUnitOfWork`.

### 4.2 WebApi

- `RolesController` con:
  - `GET /api/roles` — `[AuthorizePermission("roles.read")]`
  - `GET /api/roles/{id}` — `[AuthorizePermission("roles.read")]`
  - `POST /api/roles` — `[AuthorizePermission("roles.write")]`
  - `PUT /api/roles/{id}` — `[AuthorizePermission("roles.write")]`
  - `DELETE /api/roles/{id}` — `[AuthorizePermission("roles.write")]` (con regla de negocio: no se puede borrar el rol Admin).

### 4.3 Asignación de permisos a un rol

- `Features/Roles/AssignPermissions/AssignPermissionsToRoleCommand.cs` — `RoleId`, `PermissionIds[]` (reemplaza el set actual).
- Endpoint `PUT /api/roles/{id}/permissions`.
- Tras la asignación: invalidar el cache de permisos para todos los usuarios con ese rol (ver fase 7.1).

✅ Tests: handler tests + integration tests de los 6 endpoints.

---

## Fase 5 — CRUD de Permisos

> Los permisos son **catálogo**, no se crean en runtime libremente; se gestionan por administradores. Aun así, expongamos endpoints para listar y, si la org lo necesita, crear/editar.

### 5.1 Application

- `Features/Permissions/List/ListPermissionsQuery.cs`.
- `Features/Permissions/Create/CreatePermissionCommand.cs` — `Code`, `Name`. Validar formato `<recurso>.<accion>`.
- `Features/Permissions/Update/UpdatePermissionCommand.cs` — solo `Name` (el `Code` no se modifica para no romper el código que lo referencia).
- `Features/Permissions/Delete/DeletePermissionCommand.cs` — solo si no está asignado a ningún rol.

### 5.2 WebApi

- `PermissionsController`:
  - `GET /api/permissions` — `[AuthorizePermission("permissions.read")]`
  - `POST /api/permissions` — `[AuthorizePermission("permissions.write")]`
  - `PUT /api/permissions/{id}` — `[AuthorizePermission("permissions.write")]`
  - `DELETE /api/permissions/{id}` — `[AuthorizePermission("permissions.write")]`

### 5.3 Permisos nuevos en seed

Agregar al `IdentityDataSeeder`:

```
permissions.read, permissions.write
```

…y asignárselos al rol Admin.

✅ Tests: idem.

---

## Fase 6 — Gestión de Usuarios (CRUD + asignación de roles)

### 6.1 Application

- `Features/Users/Get/GetUserByIdQuery.cs` → `UserDto`.
- `Features/Users/List/ListUsersQuery.cs` → paginado: `Page`, `PageSize`, `Search?`. Devuelve `PagedResult<UserDto>`.
- `Features/Users/Create/CreateUserCommand.cs` — para admin que crea usuarios manualmente (con/ sin envío de invitación).
- `Features/Users/UpdateProfile/UpdateUserProfileCommand.cs`.
- `Features/Users/Deactivate/DeactivateUserCommand.cs` — `User.Deactivate()` ya existe.
- `Features/Users/Activate/ActivateUserCommand.cs`.
- `Features/Users/AssignRoles/AssignRolesToUserCommand.cs` — `UserId`, `RoleIds[]`.

### 6.2 WebApi (`UsersController` ya existe; reemplazar los stubs)

- `GET /api/users` — `[AuthorizePermission("users.read")]`, query params de paginación.
- `GET /api/users/{id}` — `[AuthorizePermission("users.read")]`.
- `POST /api/users` — `[AuthorizePermission("users.write")]`.
- `PUT /api/users/{id}/profile` — el propio usuario (sin permiso, validando ownership) o `[AuthorizePermission("users.write")]`.
- `POST /api/users/{id}/deactivate` — `[AuthorizePermission("users.write")]`.
- `POST /api/users/{id}/activate` — `[AuthorizePermission("users.write")]`.
- `PUT /api/users/{id}/roles` — `[AuthorizePermission("users.write")]`.
- `GET /api/users/me` — devuelve el usuario actual (basado en `ICurrentUser.UserId`), sin permiso (autenticado).
- `GET /api/users/me/permissions` — devuelve los permisos efectivos del usuario actual (cache hit).

### 6.3 Reglas de negocio

- No se puede desactivar al **único** Admin del sistema.
- No se puede asignar a un usuario un rol inexistente.
- Borrar los stubs `TestNotFound` y `TestConflict` del `UsersController` (no van en plantilla productiva).

✅ Tests: idem + un test que verifique que un admin puede ver `/api/users` y un usuario sin permiso recibe 403.

---

## Fase 7 — Endurecer la autorización

### 7.1 Invalidación de cache de permisos

Hoy `PermissionService` cachea por usuario por 10 min. Cuando se cambian los roles/permisos de un usuario, hay que invalidar.

- En `PermissionService`: agregar `void Invalidate(Guid userId)` y `void InvalidateAll()`.
- Llamar en handlers que mutan `UserRole`, `RolePermission` o `Role`/`Permission`.

> Para microservicios futuros, esto se reemplaza por invalidación por evento de bus.

### 7.2 Claims de permisos en el JWT (opcional, mejora rendimiento)

- En `JwtTokenService.GenerateToken`: añadir un claim `permissions` con el listado actual.
- Trade-off: si se cambian permisos del usuario, debe relogarse o usar refresh para reflejarlo.
- Decisión recomendada: **dejarlo fuera del MVP**, mantener el lookup en BBDD con cache. Documentarlo como opción futura.

### 7.3 Endpoint `/api/auth/me` (sustituye a `users/me`?)

Decidir si `me` está en `Auth` o `Users`. Recomendación: `me` en `UsersController` (es info del usuario, no de la sesión).

✅ Tests: integration test que verifique que tras `PUT /api/users/{id}/roles`, el siguiente request del usuario afectado refleja los nuevos permisos.

---

## Fase 8 — `BuildingBlocks` reutilizables

> Lo que ahora sirve a Identity, mañana servirá a otros módulos. Mover al BB.

### 8.1 `Result<T>` u opcional

(Decidir si la plantilla usa Result o se queda con excepciones. La plantilla actual usa **excepciones** — mantener consistencia y **no introducir Result** salvo decisión explícita.)

### 8.2 `PagedResult<T>`

- `BuildingBlocks.SharedKernel/Pagination/PagedResult.cs` — `Items`, `TotalCount`, `Page`, `PageSize`, `HasNext`.
- `BuildingBlocks.SharedKernel/Pagination/PagedRequest.cs` — base para queries paginadas.

### 8.3 `IUnitOfWork` genérico

Generalizar la interfaz de la fase 1 a `BuildingBlocks.SharedKernel.Persistence.IUnitOfWork`.

### 8.4 Documentar en `docs/Standards.md`

Cómo agregar un módulo nuevo siguiendo el patrón.

---

## Fase 9 — Cobertura de tests

### 9.1 Unit tests

- `User`, `Role`, `Permission`, `UserRole`, `RolePermission`, `UserProfile`: factory válida, validaciones, transiciones de estado.
- Handlers de cada Command/Query, con `IUserRepository` mockeado (xUnit + FluentAssertions + NSubstitute o Moq — usar uno solo).

### 9.2 Integration tests (`WebApiFactory`)

- Para cada endpoint nuevo: 1 caso feliz + 1 caso de error (401/403/404/409/400).
- Login → obtener token → usar token → endpoint protegido devuelve 200.
- Login con credenciales inválidas → 401.
- Endpoint protegido sin token → 401.
- Endpoint protegido con token sin permiso → 403.

### 9.3 Architecture tests

Agregar:

- `Application` no debe referenciar `IConfiguration` ni tipos de `Microsoft.EntityFrameworkCore`.
- `Domain` no debe referenciar `Microsoft.AspNetCore.*`, `Microsoft.EntityFrameworkCore.*`, `MediatR`, `FluentValidation`, `AutoMapper`.

---

## Fase 10 — DX y documentación final

### 10.1 `appsettings.Development.json.example`

Si no incluye, agregar valores ejemplo de:

- `ConnectionStrings:DefaultConnection`
- `Jwt:Key` (placeholder de 32+ chars)
- `OpenTelemetry:OtlpEndpoint`
- `Cors:AllowedOrigins`
- `Seed:Enabled = true`

### 10.2 Postman / `.http` / Swagger

- Mantener `Company.Product.WebApi.http` actualizado con ejemplos de:
  - Register / Login / Refresh / Change password
  - Listar usuarios / asignar roles
  - Listar / crear roles
- En Swagger ya hay soporte de bearer; verificar que todos los endpoints anotan respuestas (`[ProducesResponseType]`).

### 10.3 README + Standards + Release Checklist

- Actualizar `README.md` con ejemplos de los nuevos endpoints clave.
- Actualizar `docs/Standards.md` con el patrón completo de módulo (DI, Persistence, Repositories, etc.).
- Actualizar `docs/ReleaseChecklist.md` para incluir:
  - Validar que hay al menos un Admin antes de cada deploy.
  - Confirmar permisos seed-ados.

### 10.4 Renombrado de la plantilla

Cuando la plantilla se use, lo primero que hace el equipo nuevo es buscar/reemplazar `Company.Product` → `<NuevoCliente>.<NuevoProducto>`. Documentar este paso en una sección "Cómo iniciar un proyecto nuevo desde esta plantilla":

```bash
# 1. Clonar como nuevo repo
git clone <template-url> mi-nuevo-proyecto
cd mi-nuevo-proyecto
git remote remove origin
git remote add origin <nuevo-repo-url>

# 2. Reemplazar nombres
# Buscar y reemplazar Company.Product → MiCliente.MiProducto en todos los archivos
# Renombrar carpetas físicas y .csproj

# 3. Configurar secretos locales
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."
dotnet user-secrets set "Jwt:Key" "..."

# 4. Migrar BBDD inicial
dotnet ef database update --project ...Modules.Identity.Infrastructure --startup-project ...WebApi

# 5. Run
dotnet run --project ...WebApi
```

### 10.5 Script auxiliar (opcional)

Un PowerShell `dev/rename-template.ps1` que automatice el rename.

---

## Fase 11 — Preparación para microservicios (opcional, futuro)

> Esta fase **no** es parte del MVP de plantilla. Se documenta para que esté claro el camino.

- Reemplazar el `MemoryCache` de permisos por **Redis**.
- Introducir un **bus de eventos** (MassTransit + RabbitMQ / Azure Service Bus) en `BuildingBlocks.Infrastructure`.
- Outbox pattern para eventos de dominio (`Modules.<X>.Infrastructure/Outbox`).
- Extraer el primer módulo a su propio `WebApi` de Identity, dejando los demás módulos consumiéndolo por HTTP/gRPC.
- Centralizar `Authentication` en un servicio de identidad propio (o adoptar Keycloak / Azure AD B2C).

---

## Resumen ejecutivo (orden de ataque sugerido)

| # | Fase | Esfuerzo aprox. | Bloqueante de… |
|---|---|---|---|
| 1 | Cimientos transversales (ICurrentUser, IDateTimeProvider, repos, IPasswordHasher, AutoMapper, DTOs) | M | Todo lo demás |
| 2 | UserProfile (VO embebido + migración) | S | Fase 3 y 6 |
| 3 | Auth: Register + Change Password (+ Refresh tokens si se decide MVP) | M | Login completo |
| 4 | CRUD Roles + asignación de permisos | M | Fase 6, 7 |
| 5 | CRUD Permisos | S | Nada crítico |
| 6 | Gestión de Usuarios (CRUD + asignación de roles + `me`) | M | Plantilla "completa" |
| 7 | Endurecimiento de autorización (invalidación cache) | S | Cierre |
| 8 | BuildingBlocks (PagedResult, IUnitOfWork, etc.) | S | Reutilización entre módulos |
| 9 | Cobertura de tests | M | Calidad |
| 10 | DX y docs (rename, ejemplos, checklist) | S | Adopción interna |
| 11 | (Opcional) Preparación microservicios | L | Crecimiento futuro |

Esfuerzo: **S** = ½–1 día, **M** = 2–4 días, **L** = >1 semana.

---

## Definición de "plantilla terminada"

Marcar como completa la plantilla cuando:

- [ ] Los endpoints `register`, `login`, `change-password`, `me`, CRUD de `users`, `roles` y `permissions` están implementados, autorizados, validados y testeados.
- [ ] Hay al menos 1 unit test por handler y 1 integration test por endpoint.
- [ ] `dotnet test` pasa en verde, incluyendo Architecture Tests.
- [ ] Swagger muestra correctamente cada endpoint con su `[ProducesResponseType]` y bearer auth.
- [ ] El admin seed-ado puede:
  1. Login.
  2. Crear un permiso, un rol y un usuario nuevo.
  3. Asignarle el rol al usuario.
  4. El usuario nuevo puede login y consultar lo que su rol permite, no más.
- [ ] La documentación `docs/01..03` está alineada con el código real.
- [ ] El `appsettings.Development.json.example` es suficiente para que un dev nuevo arranque en < 15 min.

Cuando todos los checks estén ✅, la plantilla está lista para usarse como punto de partida en cualquier proyecto backend con login de la organización.
