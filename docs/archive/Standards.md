> **Archivado el 2026-11-05.** Reemplazado por las skills de `.claude/skills/`, las reglas de `.claude/rules/` y [`docs/architecture.md`](../architecture.md). Se conserva como histórico: no describe el estado actual y no se actualiza; sus enlaces internos pueden no resolver.

# Estándares y Convenciones

## Regla de oro: las dependencias solo fluyen hacia adentro

```
WebApi → Infrastructure → Application → Domain
```

**Nunca al revés.** Los Architecture Tests (`CleanArchitectureTests`) rompen el build de CI si se viola esto.

---

## Estructura de capas

| Proyecto | Responsabilidad | Puede referenciar |
|---|---|---|
| `*.Domain` | Entidades, value objects, interfaces de dominio | Solo SharedKernel |
| `*.Application` | Casos de uso (Commands/Queries), DTOs, contratos (interfaces) | Domain, SharedKernel |
| `*.Infrastructure` | EF Core, repos, servicios externos, JWT, etc. | Application, Domain |
| `WebApi` | Controllers, middleware, DI root, configuración | Infrastructure, Application |
| `BuildingBlocks.SharedKernel` | Tipos compartidos sin lógica de negocio | Nadie externo |
| `BuildingBlocks.Infrastructure` | Abstracciones de infraestructura reutilizables | SharedKernel |

---

## Cómo agregar un nuevo módulo

1. Crear **tres** proyectos siguiendo el patrón `Camilo.OrderFlow.Modules.<Nombre>.<Capa>`: `Domain`, `Application` e `Infrastructure` (el host `WebApi` se comparte).
2. Agregar referencias solo en la dirección permitida (tabla anterior).
3. Exponer la DI mediante extension methods en `DependencyInjection/ServiceCollectionExtensions.cs`.
4. Registrar en `Program.cs`:
   ```csharp
   builder.Services.AddOrdersApplication();
   builder.Services.AddOrdersInfrastructure(builder.Configuration);
   ```
5. El módulo **no necesita** tocar `Program.cs` para nada más.

---

## Dónde poner qué

| ¿Qué? | ¿Dónde? |
|---|---|
| Entidad de negocio | `*.Domain/Entities/` |
| Validación de inputs | Junto a su Command/Query, en `*.Application/Commands/<Caso>/` (FluentValidation) |
| Lógica de negocio | `*.Domain` o `*.Application` (handler/service) |
| Llamada a BBDD | `*.Infrastructure/Persistence/` |
| Llamada a API externa | `*.Infrastructure/Http/` |
| Configuración de seguridad | `WebApi/Security/` |
| Middleware transversal | `WebApi/Middleware/` |
| DTO de respuesta HTTP | `*.Application/Dtos/` o junto al controller si es plano |

---

## Cómo agregar un HttpClient resiliente

1. Añadir sección en `appsettings.json` bajo `"Resilience"`:
   ```json
   "PaymentClient": {
     "TimeoutSeconds": 10,
     "RetryCount": 2,
     "BaseDelayMs": 300,
     "CircuitBreaker": { "FailureThreshold": 0.5, "SamplingDurationSeconds": 30, "MinimumThroughput": 10, "BreakDurationSeconds": 15 }
   }
   ```
2. Registrar en `ServiceCollectionExtensions.Resilience.cs` siguiendo el patrón `ErpClient`.
3. Inyectar `IHttpClientFactory` y crear con `_factory.CreateClient("PaymentClient")`.

---

## Cómo agregar un permiso

1. Añadir la constante en `*.Infrastructure/Authorization/<Modulo>Permissions.cs`
   (en Identity: `IdentityPermissions.cs`), con el formato `<recurso>.<accion>` en minúsculas,
   y agregarla también a la lista `All`:
   ```csharp
   public static class OrdersPermissions { public const string OrdersRead = "orders.read"; }
   ```
2. Sembrarlo en el seeder del módulo, para que exista en BBDD y el rol Admin lo tenga.
   `IdentityDataSeeder` falla al arrancar si una constante declarada no está en el seed.
3. Decorar el endpoint — siempre con la constante, nunca con el literal:
   ```csharp
   [AuthorizePermission(OrdersPermissions.OrdersRead)]
   public IActionResult Get() { ... }
   ```

> Un literal mal escrito compila igual y produce un 403 imposible de diagnosticar. La constante
> convierte ese error en un fallo de compilación.

---

## Versionado de API

El versionado se hace por header `api-version`. La versión por defecto es `1.0`.  
Para agregar una nueva versión en un controller existente:
```csharp
[ApiVersion("2.0")]
[ApiVersion("1.0")]
[Route("api/users")]
public class UsersController : ControllerBase { ... }
```

---

## Gestión central de versiones de paquetes

Todas las versiones de NuGet están en `Directory.Packages.props` en la raíz.  
**Nunca** agregar `Version="..."` en un `.csproj`. Siempre actualizar el `.props`.

---

## Patrón completo de módulo

### Registración DI

Cada módulo expone dos extension methods en `DependencyInjection/ServiceCollectionExtensions.cs`:

```csharp
// Application: handlers, validators and mappers
builder.Services.AddOrdersApplication();

// Infrastructure: persistence and external services
builder.Services.AddOrdersInfrastructure(builder.Configuration);
```

Ambos se registran en `Program.cs`, en ese orden. El módulo no toca nada más en `Program.cs`.

---

### Cómo añadir una entidad

1. Crear en `*.Domain/Entities/MyEntity.cs` — sin dependencias de EF ni de Application.
2. Crear la configuración EF en `*.Infrastructure/Persistence/Configurations/MyEntityConfiguration.cs`.
3. Registrar en `DbContext` con `modelBuilder.ApplyConfiguration(new MyEntityConfiguration())`.
4. Agregar `public DbSet<MyEntity> MyEntities => Set<MyEntity>();` al `DbContext`.
5. Generar migración: `dotnet ef migrations add AddMyEntity --project <Infrastructure> --startup-project <WebApi>`.
6. Crear interfaz del repo en `*.Application/Abstractions/IMyEntityRepository.cs`.
7. Crear implementación en `*.Infrastructure/Persistence/Repositories/MyEntityRepository.cs`.
8. Registrar en `AddXxxInfrastructure`: `services.AddScoped<IMyEntityRepository, MyEntityRepository>()`.

> Nunca inyectar `DbContext` directamente en handlers. Siempre a través del repositorio.

---

### Cómo añadir un Command

Orden obligatorio: Domain → Application → Infrastructure → WebApi → Tests.

```
Commands/
  MyAction/
    MyActionCommand.cs          // record que implementa IRequest<TResponse>
    MyActionCommandValidator.cs // AbstractValidator<MyActionCommand> con FluentValidation
    MyActionCommandHandler.cs   // IRequestHandler<MyActionCommand, TResponse>
```

**Command:**
```csharp
public sealed record MyActionCommand(Guid Id, string Name) : IRequest<MyEntityDto>;
```

**Validator** (ValidationPipelineBehavior lo ejecuta automáticamente):
```csharp
public sealed class MyActionCommandValidator : AbstractValidator<MyActionCommand>
{
    public MyActionCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
```

**Handler:**
```csharp
public sealed class MyActionCommandHandler : IRequestHandler<MyActionCommand, MyEntityDto>
{
    private readonly IMyEntityRepository _repo;
    private readonly IMyModuleExecutionContext _executionContext;

    public MyActionCommandHandler(IMyEntityRepository repo, IMyModuleExecutionContext executionContext)
    {
        _repo = repo;
        _executionContext = executionContext;
    }

    public async Task<MyEntityDto> Handle(MyActionCommand request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException($"Entity {request.Id} not found.");

        entity.DoSomething(request.Name);
        await _executionContext.CommitAsync(ct);  // Commit at the end of the handler

        return entity.ToDto();
    }
}
```

> El handler llama `CommitAsync`. Los repos **nunca** llaman `SaveChangesAsync`.

---

### Cómo añadir un Query paginado

```csharp
// PagedRequest provides SafePage, SafePageSize and Skip
public sealed record ListMyEntitiesQuery(int Page, int PageSize, string? Search)
    : PagedRequest(Page, PageSize), IRequest<PagedResult<MyEntityDto>>;
```

**Handler:**
```csharp
public async Task<PagedResult<MyEntityDto>> Handle(ListMyEntitiesQuery request, CancellationToken ct)
{
    var items = await _repo.GetPagedAsync(request.Search, request.Skip, request.SafePageSize, ct);
    var total = await _repo.CountAsync(request.Search, ct);

    return new PagedResult<MyEntityDto>(
        items.Select(e => e.ToDto()).ToList(),
        total,
        request.SafePage,
        request.SafePageSize);
}
```

`PagedRequest` garantiza que `Page ≥ 1` y `PageSize ∈ [1, 100]` antes de llegar al repositorio.

---

### Cómo añadir un endpoint

```csharp
[ApiController]
[Route("api/my-entities")]
[ApiVersion("1.0")]
public sealed class MyEntitiesController : ControllerBase
{
    private readonly ISender _sender;
    public MyEntitiesController(ISender sender) => _sender = sender;

    [HttpGet]
    [AuthorizePermission(MyEntitiesPermissions.Read)]
    [ProducesResponseType(typeof(PagedResult<MyEntityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new ListMyEntitiesQuery(page, pageSize, search), cancellationToken);
        return Ok(result);
    }
}
```

Reglas:
- `[AuthorizePermission(IdentityPermissions.UsersRead)]`, nunca `[Authorize(Roles="...")]`.
- Todo permiso nuevo debe estar en el seed (`IdentityDataSeeder`) y asignado al rol Admin.
- `[ProducesResponseType]` para cada código HTTP posible (200, 201, 400, 401, 403, 404, 409).

---

### Cómo invalidar caché de permisos

El sistema cachea los permisos efectivos de cada usuario (`perms:user:{userId}`) con tags por rol (`role:{roleId}`).

Cuándo invalidar:

| Acción | Método |
|---|---|
| Se cambian roles de un usuario | `await _permissionService.InvalidateUserAsync(userId, ct)` |
| Se cambian permisos de un rol | `await _permissionService.InvalidateRoleAsync(roleId, ct)` |
| Se elimina un rol | `await _permissionService.InvalidateRoleAsync(roleId, ct)` |

Patrón en un handler:
```csharp
await _executionContext.CommitAsync(ct);            // Persist first
await _permissionService.InvalidateRoleAsync(roleId, ct);  // Invalidate after commit
```

> En producción multi-instancia se requiere Redis configurado (`Cache:Redis` en appsettings).  
> Sin Redis, solo se invalida la caché L1 local de la instancia que recibe la petición.

---

### El punto de commit de cada módulo

`IExecutionContext` (en `BuildingBlocks.SharedKernel`) es **global**: si dos módulos registraran esa
misma interfaz, la última registración ganaría y los handlers del otro módulo dejarían de persistir
**sin lanzar ningún error**.

Por eso cada módulo declara la suya y registra esa:

```csharp
// Application/Abstractions/IOrdersExecutionContext.cs
public interface IOrdersExecutionContext : IExecutionContext;

// Infrastructure/Persistence/OrdersExecutionContext.cs
internal sealed class OrdersExecutionContext : IOrdersExecutionContext
{
    private readonly OrdersDbContext _dbContext;
    public OrdersExecutionContext(OrdersDbContext dbContext) => _dbContext = dbContext;
    public Task<int> CommitAsync(CancellationToken ct = default) => _dbContext.SaveChangesAsync(ct);
}

// Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs
services.AddScoped<IOrdersExecutionContext, OrdersExecutionContext>();
```

Los handlers del módulo inyectan `IOrdersExecutionContext`. El test `ModuleRegistrationTests`
falla si un módulo registrado explícitamente en ese test registra `IExecutionContext` directamente. Incorporar manualmente cada registro nuevo y su comprobación de resolución.

---

### Repos vs DbContext directo

**Siempre usar el repositorio.** Nunca inyectar `DbContext` en un handler o controller.

| ✅ Correcto | ❌ Incorrecto |
|---|---|
| `IMyEntityRepository` en el handler | `MyDbContext` en el handler |
| `_repo.GetByIdAsync(id, ct)` | `_context.MyEntities.FirstOrDefaultAsync(...)` |

Los repos usan `AsNoTracking()` en queries de solo lectura. En operaciones de escritura, el handler llama `_executionContext.CommitAsync(ct)` al final.

---

### Cómo escribir un test de handler (unit test)

Usar NSubstitute para repos, Shouldly para asserts:

```csharp
public sealed class MyActionCommandHandlerTests
{
    private readonly IMyEntityRepository _repo = Substitute.For<IMyEntityRepository>();
    private readonly IMyModuleExecutionContext _ctx = Substitute.For<IMyModuleExecutionContext>();

    private MyActionCommandHandler CreateHandler() => new(_repo, _ctx);

    [Fact]
    public async Task Handle_ValidRequest_ReturnsDto()
    {
        var entity = new MyEntity(Guid.NewGuid(), "Original");
        _repo.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>()).Returns(entity);
        _ctx.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await CreateHandler().Handle(
            new MyActionCommand(entity.Id, "New Name"),
            TestContext.Current.CancellationToken);

        result.Name.ShouldBe("New Name");
        await _ctx.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((MyEntity?)null);

        await Should.ThrowAsync<NotFoundException>(() =>
            CreateHandler().Handle(
                new MyActionCommand(Guid.NewGuid(), "X"),
                TestContext.Current.CancellationToken));
    }
}
```

---

### Cómo escribir un test de integración

Los tests de integración usan `WebApiFactory` (SQLite in-memory, toda la pipeline ASP.NET Core):

```csharp
public sealed class MyEntitiesTests : IClassFixture<WebApiFactory>
{
    private readonly HttpClient _client;

    public MyEntitiesTests(WebApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_AsAdmin_Returns200()
    {
        var token = await LoginAsAdminAsync();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/my-entities");
        request.Headers.Authorization = new("Bearer", token);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private async Task<string> LoginAsAdminAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { usernameOrEmail = "admin@company.com", password = "Admin123!" },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement;
        return body.GetProperty("accessToken").GetString()!;
    }
}
```

> **`TestContext.Current.CancellationToken`:** en xunit.v3 todo método que acepte un
> `CancellationToken` debe recibir el del test, para que la ejecución responda a cancelaciones y
> timeouts. El analizador `xUnit1051` lo exige, y el CI compila con warnings como errores.

Convenciones:
- Un caso feliz + al menos un caso de error por endpoint.
- Datos únicos por test (`Guid.NewGuid().ToString("N")[..8]`) para evitar colisiones entre tests que comparten la misma instancia de BD.
- No testear reglas de negocio en integration tests — para eso están los unit tests de handlers.

## Límites y ejemplos previstos

OrdersPermissions y MyEntitiesPermissions son ejemplos hipotéticos que requieren crear su clase, lista All y seed del módulo; no son miembros existentes de IdentityPermissions. Para el endpoint del ejemplo definir `public static class MyEntitiesPermissions { public const string Read = "myentities.read"; }`. No se crean módulos de negocio como parte de esta conciliación.

ModuleCatalog descubre ensamblados referenciados y copiados a la salida de tests. Al agregar un módulo, comprobar esas referencias y registrar manualmente Add<Module>Infrastructure y la resolución de su execution context en ModuleRegistrationTests. El aislamiento entre dos módulos todavía no tiene ejemplo ejecutable: hoy solo existe Identity.

HybridCache usa L1 local y L2 Redis opcional. Redis compartido no demuestra revocación inmediata entre réplicas: verificar propagación por clave y por tag y medir la ventana de permisos obsoletos. CacheInvalidationTests solo prueba una WebApiFactory; no hay garantía temporal multiinstancia medida.

Patrón previsto para integración entre módulos: puerto en Application del consumidor y adaptador en el host de composición que llama una fachada pública del proveedor. No añadir referencias directas entre módulos. Este patrón no está implementado en un escenario multimódulo actual; la extracción a red requiere decisiones de contratos, latencia y fallos parciales.

Documentación en español; código, identificadores y comentarios nuevos/modificados en inglés. Los ejemplos son fragmentos de extensión, no capacidades ya implementadas.
