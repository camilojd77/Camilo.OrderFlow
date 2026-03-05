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

1. Crear cuatro proyectos siguiendo el patrón `Company.Product.Modules.<Nombre>.<Capa>`.
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
| Validación de inputs | `*.Application/Validators/` (FluentValidation) |
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

1. Crear la constante en `*.Infrastructure/Authorization/Permissions.cs` (o similar).
2. Decorar el endpoint:
   ```csharp
   [AuthorizePermission("orders.read")]
   public IActionResult Get() { ... }
   ```
3. Insertar el permiso en el seed de BBDD si aplica.

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
