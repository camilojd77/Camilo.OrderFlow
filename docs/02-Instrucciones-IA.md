# Documento 2 — Instrucciones para agentes de IA

> **Audiencia:** modelos de IA (Claude, Copilot, Cursor, etc.) que generan o modifican código en este repositorio.
> **Objetivo:** establecer reglas no-negociables de arquitectura, nomenclatura y orden de implementación para que cualquier output sea consistente con la plantilla.

> Lee este documento **antes** de proponer o escribir cualquier cambio. Si una instrucción del usuario contradice este documento, **prioriza este documento** y avisa al usuario antes de continuar.

---

## 1. Contexto del repositorio

- **Plantilla backend en .NET 10** pensada como **monolito modular**, lista para crecer a microservicios.
- **Lenguaje:** C# (`net10.0`), nullables habilitados.
- **Patrones principales:** Clean Architecture + Modular Monolith + CQRS (dispatcher propio en `BuildingBlocks.SharedKernel.Messaging`) + Result-by-exception (excepciones de dominio).
- **Persistencia:** EF Core 10 + SQL Server, un `DbContext` por módulo, schema separado por módulo.
- **Autorización:** basada en **permisos en BBDD** (`[AuthorizePermission("xxx.read")]`), no en roles hardcoded.
- **Versiones de NuGet:** **siempre** centralizadas en `Directory.Packages.props`. Nunca poner `Version="..."` en un `.csproj`.

Antes de proponer una librería externa, verifica que ya esté en `Directory.Packages.props`. Si no está, propónla al usuario antes de añadirla.

---

## 2. Reglas de arquitectura (NO violar)

### 2.1 Dirección de dependencias

```
WebApi → Infrastructure → Application → Domain
```

- `Domain` no referencia a nadie (excepto `BuildingBlocks.SharedKernel`).
- `Application` referencia solo `Domain` y `SharedKernel`.
- `Infrastructure` referencia `Application` y `Domain`.
- `WebApi` referencia `Infrastructure` y `Application`.
- `BuildingBlocks.SharedKernel` no referencia a nadie del proyecto.
- `BuildingBlocks.Infrastructure` solo referencia `SharedKernel`.

> Hay tests que rompen el build si esto se viola: `tests/Company.Product.ArchitectureTests/CleanArchitectureTests.cs`. Si tu cambio rompe esos tests, el cambio está mal — corrige el cambio, no el test.

### 2.2 Aislamiento entre módulos

- Un módulo (`Modules.Catalog.*`) **NUNCA** referencia directamente otro módulo (`Modules.Identity.*`).
- Si necesitas datos de otro módulo:
  - Define una **interfaz en `Application`** del módulo consumidor.
  - **Implementa esa interfaz** en `Infrastructure` del mismo módulo, llamando al otro módulo por una API pública (interfaz expuesta) o por HTTP/mensajería en el futuro.
- Cada módulo es candidato a ser extraído como microservicio sin tocar su código de Domain/Application.

### 2.3 Capas: qué va dónde

| Quieres añadir… | Va en… |
|---|---|
| Una entidad o value object | `Modules.<X>.Domain/Entities` |
| Una excepción de negocio | Reutiliza las de `BuildingBlocks.SharedKernel/Exceptions` (`NotFoundException`, `ConflictException`, `ForbiddenException`, `ValidationException`). Solo si ninguna encaja, crea una en `Modules.<X>.Domain/Exceptions` heredando de `DomainException` |
| Un caso de uso (Command) | `Modules.<X>.Application/Commands/<Caso>/` |
| Un caso de uso (Query) | `Modules.<X>.Application/Queries/<Recurso>/<Caso>/` |
| Un DTO de respuesta | `Modules.<X>.Application/Dtos` |
| Un validator de FluentValidation | En la misma carpeta que su Command/Query, como `<Caso>CommandValidator.cs` |
| Un mapper de Mapperly | `Modules.<X>.Application/Mapping/<Entity>Mapper.cs (partial)` |
| Un repositorio (interfaz) | `Modules.<X>.Application/Abstractions` |
| Un repositorio (impl) | `Modules.<X>.Infrastructure/Persistence/Repositories` |
| Una `IEntityTypeConfiguration<T>` | `Modules.<X>.Infrastructure/Persistence/Configurations` |
| Una migración EF | `Modules.<X>.Infrastructure/Migrations` |
| Un seeder | `Modules.<X>.Infrastructure/Persistence/Seed` |
| Un servicio externo (HttpClient, email…) | `Modules.<X>.Infrastructure/<Subsistema>` |
| Un controller | `WebApi/Controllers` |
| Middleware transversal | `WebApi/Middleware` |
| Configuración de seguridad/observabilidad/etc | `WebApi/Security`, `WebApi/Observability`, `WebApi/Extensions` |

---

## 3. Nomenclatura

### 3.1 Proyectos

```
Company.Product.<Layer>                        ← BuildingBlocks o WebApi
Company.Product.BuildingBlocks.<Block>          ← p.ej. SharedKernel, Infrastructure
Company.Product.Modules.<Module>.<Layer>        ← p.ej. Modules.Identity.Domain
```

### 3.2 Namespaces

Coinciden 1:1 con la ruta de carpetas dentro del proyecto:

```
Company.Product.Modules.Identity.Application.Commands.Users.CreateUser
Company.Product.Modules.Identity.Application.Queries.Users.GetUserById
```

### 3.3 Archivos

| Tipo | Patrón | Ejemplo |
|---|---|---|
| Entidad | `<Entity>.cs` | `User.cs`, `Role.cs` |
| Command | `<Verb><Noun>Command.cs` | `CreateUserCommand.cs` |
| Command Handler | `<Verb><Noun>CommandHandler.cs` | `CreateUserCommandHandler.cs` |
| Command Validator | `<Verb><Noun>CommandValidator.cs` | `CreateUserCommandValidator.cs` |
| Query | `Get<Noun>Query.cs` o `List<Noun>Query.cs` | `GetUserByIdQuery.cs`, `ListUsersQuery.cs` |
| Query Handler | igual + `Handler` | `GetUserByIdQueryHandler.cs` |
| DTO | `<Noun>Dto.cs` o `<Noun>Response.cs` | `UserDto.cs`, `LoginResponse.cs` |
| Repositorio (interfaz) | `I<Entity>Repository.cs` | `IUserRepository.cs` |
| Repositorio (impl) | `<Entity>Repository.cs` | `UserRepository.cs` |
| EF Configuration | `<Entity>Configuration.cs` | `UserConfiguration.cs` |
| Migración | autogenerado por EF | `20260304152336_InitialIdentity.cs` |
| Mapper de Mapperly | `<Entity>Mapper.cs` (partial) | `UserMapper.cs` |
| Extensión DI | `ServiceCollectionExtensions.cs` (o particionado por tema con `.Cors.cs`, `.Resilience.cs`) | |

### 3.4 Permisos

- Formato: `<recurso>.<accion>` en minúsculas. Ejemplos: `users.read`, `users.write`, `roles.read`, `roles.write`, `orders.cancel`.
- **Constantes** centralizadas en cada módulo: `Modules.<X>.Infrastructure/Authorization/<X>Permissions.cs` (en Identity: `IdentityPermissions.cs`). **Nunca uses el literal en el atributo**: `[AuthorizePermission(IdentityPermissions.UsersRead)]`.
- Al agregar una constante, añádela también a la lista `All` y siémbrala en `IdentityDataSeeder`; el seeder lanza al arrancar si una constante declarada no está sembrada.

### 3.5 Endpoints HTTP

- Ruta: `api/<recurso>` en plural y minúsculas (`api/users`, `api/roles`).
- Verbos REST: `GET`, `POST`, `PUT`, `PATCH`, `DELETE`.
- Códigos:
  - `200 OK` para GET con cuerpo.
  - `201 Created` para POST con `Location`.
  - `204 No Content` para DELETE / PUT sin cuerpo.
  - `400` para validación de request (lo lanza `ValidationPipelineBehavior`).
  - `401` sin token / token inválido.
  - `403` con token pero sin permiso.
  - `404` recurso inexistente.
  - `409` conflicto (email ya registrado).

### 3.6 Branching y commits

- Branches: `feature/<tema-corto>`, `fix/<tema-corto>`, `chore/<tema-corto>`.
- Commits en imperativo, en español. Ejemplo: `Agrega CRUD de usuarios`. Sin scope ni `feat:` salvo que el repo lo adopte explícitamente.

---

## 4. Patrones que ya están en la plantilla — úsalos, no reinventes

| Patrón | Ya implementado | Cómo usarlo |
|---|---|---|
| **CQRS con dispatcher propio** | `AddIdentityApplication()` registra `ISender` + behaviors | Crea `IRequest<T>` + `IRequestHandler<T,R>` en `BuildingBlocks.SharedKernel.Messaging`; se descubre por reflexión. |
| **Validación automática** | `ValidationPipelineBehavior<TRequest,TResponse>` | Crea un `AbstractValidator<T>`; se ejecuta antes del handler y lanza `ValidationException`. |
| **Excepciones de dominio → ProblemDetails** | `GlobalExceptionHandler` mapea las excepciones de `SharedKernel.Exceptions` | Lanza `NotFoundException`, `ConflictException`, `ForbiddenException`, `ValidationException`. NO inventes nuevos handlers. |
| **JWT** | `JwtTokenService` + `AuthService.LoginAsync` | Para endpoints que devuelven tokens, usa `IAuthService` desde el controller. |
| **Autorización por permiso** | `[AuthorizePermission("...")]` + `PermissionPolicyProvider` | Decora el endpoint o método. Cachea automáticamente con `HybridCache` (TTL configurable). |
| **Commit transaccional** | `IIdentityExecutionContext` (una interfaz por módulo) | Inyéctala en el handler y llama `CommitAsync(ct)` al final. Los repos nunca llaman `SaveChangesAsync`. |
| **Correlation ID** | `CorrelationIdMiddleware` + `CorrelationIdHandler` | Para outgoing HttpClients: `.AddHttpMessageHandler<CorrelationIdHandler>()`. |
| **Resiliencia HTTP** | `AddResilientHttpClients` | Añade nueva config en `appsettings:Resilience:<Cliente>` y registra siguiendo el patrón de `ErpClient`. |
| **Rate limiting** | Política `default` (todos los endpoints) y `auth` (login) | `[EnableRateLimiting("auth")]` para endpoints sensibles. |
| **Health checks** | `/health/live`, `/health/ready` (incluye SQL) | Si añades una dependencia crítica, agrega un health check en `HealthChecksExtensions`. |
| **Observabilidad** | OpenTelemetry exporta a OTel Collector. Serilog con traceId/spanId. | No registres logs con `Console.WriteLine`. Usa `ILogger<T>`. |

---

## 5. Orden de implementación

Cuando te pidan **agregar una funcionalidad nueva**, sigue este orden estricto:

1. **Domain primero**
   - Entidad / VO con encapsulamiento (`private set`, factory).
   - Excepciones de dominio si el escenario lo amerita.
   - Tests de dominio (Arrange/Act/Assert con xUnit + Shouldly).

2. **Application después**
   - Definir el **contrato** (interfaz de repositorio o servicio que necesite el caso de uso) en `Application/Abstractions`.
   - Crear el **Command/Query** + **Handler** + **Validator**.
   - Crear el **DTO** de respuesta (no devolver entidades de dominio).
   - Crear el **mapper de Mapperly** (`<Entity>Mapper.cs`, partial) si hay mapeo.
   - Tests de handler con doble (mock/fake) del repositorio.

3. **Infrastructure después**
   - Implementar la interfaz definida en Application.
   - Si es entidad nueva: `IEntityTypeConfiguration<T>`, agregar `DbSet<T>` al `DbContext`, generar **migración** (`dotnet ef migrations add ...`).
   - Si es servicio externo: implementar el cliente; registrar en `ServiceCollectionExtensions`.

4. **WebApi al final**
   - Controller con la ruta correcta y `[AuthorizePermission("...")]` cuando aplique.
   - Tests de integración en `tests/Company.Product.WebApi.IntegrationTests` para el camino feliz y un caso de error.

5. **Seeder / config / docs**
   - Si añadiste un permiso: agregarlo al seeder.
   - Si añadiste config: documentarla en el `appsettings.json` con valor por defecto y en `appsettings.Development.json.example` si es secreto.
   - Si afecta arranque/deploy: actualiza `docs/ReleaseChecklist.md`.

> **Nunca empieces por el controller.** Eso lleva a fugas de detalles de Infrastructure hacia capas internas.

---

## 6. Reglas de código

- **Encapsulamiento de entidades**: propiedades con `private set`, mutaciones por método (`user.Deactivate()`, no `user.IsActive = false`).
- **Constructores `private` para EF + constructor público para creación válida**:
  ```csharp
  private User() {} // EF
  public User(Guid id, string email, ...) { /* validar y asignar */ }
  ```
- **Nullable reference types ON**: usa `?` cuando el valor puede ser null y `default!` solo en propiedades que EF rellenará.
- **CancellationToken** en todo método async público.
- **`AsNoTracking()`** en queries de solo lectura.
- **No `async void`** salvo event handlers.
- **No catch genérico** (`catch(Exception)`) para silenciar; deja que `GlobalExceptionHandler` se encargue.
- **No `string.Format` ni interpolación** para logs estructurados — usa template `_logger.LogInformation("User {UserId} created", id);`.
- **Sin comentarios redundantes**. Comenta solo el "por qué" no obvio.
- **Sin TODOs sin issue asociado**.

---

## 7. Seguridad

- **Nunca** loggear `password`, `token`, `Authorization` header, `Cookie` ni datos sensibles del usuario. Si Sí entra en logs, redactar (`***`).
- **Nunca** poner secretos en `appsettings.json`. Va en User Secrets (dev) o Key Vault / variables de entorno (otros entornos).
- **Validar input siempre con FluentValidation**. No confíes en que el cliente filtra.
- **Hashing de passwords**: BCrypt (`BCrypt.Net-Next`). Coste por defecto OK.
- **Tokens JWT**: clave ≥ 32 caracteres, `ClockSkew = TimeSpan.Zero`. Ya están así — no debilitar.
- **Headers de seguridad**: ya configurados (`SecurityHeadersMiddleware`); no comentar/desactivar.
- **CORS**: `Cors:AllowedOrigins` debe enumerar orígenes; no usar `*` con `AllowCredentials=true`.

---

## 8. Tests

Antes de marcar una funcionalidad como completa, deben pasar:

```bash
dotnet test tests/Company.Product.ArchitectureTests   # arquitectura
dotnet test tests/Company.Product.Identity.UnitTests  # dominio + handlers
dotnet test tests/Company.Product.WebApi.IntegrationTests  # endpoints
```

Estructura de un test (Arrange/Act/Assert):

```csharp
[Fact(DisplayName = "Login con credenciales correctas devuelve token")]
public async Task Login_ValidCredentials_ReturnsToken()
{
    // Arrange
    ...
    // Act
    var result = await sut.LoginAsync(request, default);
    // Assert
    result.ShouldNotBeNull();
    result.Token.ShouldNotBeNullOrWhiteSpace();
}
```

---

## 9. Cuándo NO actuar y preguntar

Antes de:

- Agregar/quitar un proyecto en la solución.
- Cambiar versiones de NuGet en `Directory.Packages.props`.
- Tocar `Program.cs` para algo que no sea registrar un módulo nuevo (`AddXxxApplication` / `AddXxxInfrastructure`).
- Modificar tests de arquitectura.
- Cambiar el esquema de BBDD de un módulo ya existente sin migración limpia.
- Borrar archivos.

→ **Pregunta primero al usuario.** Estos cambios suelen tener consecuencias amplias.

---

## 10. Lista rápida de "no" frecuentes

- ❌ No usar `IConfiguration` desde `Domain` o `Application`.
- ❌ No inyectar `DbContext` en controllers.
- ❌ No exponer entidades en respuestas HTTP.
- ❌ No registrar servicios desde `Program.cs` que pertenezcan a un módulo.
- ❌ No registrar ni inyectar `IExecutionContext` directamente: cada módulo usa su propia `I<Modulo>ExecutionContext`. Registrar la genérica rompe el commit de los otros módulos en silencio.
- ❌ No usar `Identity` de ASP.NET Core (`Microsoft.AspNetCore.Identity`). Esta plantilla tiene su **propio** sistema de identidad — usa `Modules.Identity` y sus servicios.
- ❌ No reemplazar el sistema de permisos por `[Authorize(Roles="...")]`.
- ❌ No silenciar excepciones para "limpiar" logs.
- ❌ No reescribir `GlobalExceptionHandler` para manejar excepciones nuevas: crea una `DomainException` apropiada y, si hace falta, agrega su mapeo allí.
- ❌ No introducir librerías nuevas sin que el usuario lo apruebe explícitamente.

---

## 11. Cuando se agrega un módulo nuevo (checklist mínimo)

1. Crear los 3 `.csproj` (`Domain`, `Application`, `Infrastructure`) con las referencias correctas.
2. Agregar al `Company.Product.slnx` bajo `/src/Modules/`.
3. Definir entidades en `Domain/Entities/`.
4. Crear `<Module>DbContext` propio en `Infrastructure/Persistence/Context`, con schema propio (`HasDefaultSchema("<module>")`).
5. Configurar entidades con `IEntityTypeConfiguration<T>`.
6. Generar primera migración: `dotnet ef migrations add Initial<Module> --project Company.Product.Modules.<Module>.Infrastructure --startup-project Company.Product.WebApi --context <Module>DbContext`.
7. Crear `I<Module>ExecutionContext : IExecutionContext` en `Application/Abstractions` y su implementación en `Infrastructure/Persistence`.
8. Crear `Add<Module>Application` y `Add<Module>Infrastructure(IConfiguration)` extensions, registrando `I<Module>ExecutionContext` (nunca `IExecutionContext`).
9. Registrarlos en `Program.cs`.
10. (Si aplica) seeder, permisos, controllers, integration tests.
11. Verificar que los Architecture Tests siguen pasando: descubren el módulo nuevo automáticamente por convención de nombre.

---

## 12. Cuando se modifica algo existente

- Lee primero las entidades y handlers afectados; no asumas estructura.
- Si la modificación afecta el esquema de BBDD, **siempre** generar una migración nueva, **nunca** editar una migración ya commiteada.
- Si afecta el contrato HTTP, considera si necesitas una versión `2.0` con `[ApiVersion("2.0")]`. **No** hagas breaking changes en `1.0` sin acordarlo con el usuario.
- Actualiza tests existentes y agrega los que falten.

---

## 13. Resumen ejecutivo (para pegar al inicio de cualquier prompt)

> Este repo es una plantilla backend en .NET 10 con Clean Architecture + Modular Monolith. Reglas no-negociables:
> 1. Dependencias solo hacia adentro (`WebApi → Infrastructure → Application → Domain`).
> 2. Cada módulo es independiente, con su `DbContext`, su schema y su `I<Modulo>ExecutionContext` propios.
> 3. Versiones de NuGet **solo** en `Directory.Packages.props`.
> 4. Autorización por permiso (`[AuthorizePermission("...")]`), no por rol hardcoded.
> 5. CQRS con dispatcher propio (`ISender`), validación con FluentValidation, mapping con **Mapperly** o manual. Tests con **Shouldly**.
> 6. Excepciones de dominio extienden `DomainException`; el `GlobalExceptionHandler` las traduce a `ProblemDetails`.
> 7. Orden de implementación: Domain → Application → Infrastructure → WebApi → tests.
> 8. Antes de cualquier cambio destructivo o estructural, preguntar.
