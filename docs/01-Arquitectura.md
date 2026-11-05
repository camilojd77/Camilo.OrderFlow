# Documento 1 — Arquitectura de la solución

> **Audiencia:** desarrolladores que van a usar `Camilo.OrderFlow` como plantilla para iniciar un nuevo backend dentro de la organización.
> **Objetivo:** entender la arquitectura, qué se debe respetar al extenderla y qué ventajas ofrece.

---

## 1. Visión general

`Camilo.OrderFlow` es una **plantilla backend en .NET 10** diseñada como **monolito modular** con separación de capas que facilita evaluar una extracción futura; todavía requiere contratos y tratamiento de fallos distribuidos.

La estructura nace lista para soportar:

- Autenticación con **JWT Bearer**.
- Autorización **basada en permisos** (granular, no solo por rol).
- Módulo **Identity** completo: usuarios, roles, permisos y la relación entre ellos.
- **Observabilidad de extremo a extremo**: trazas, métricas y logs correlacionados.
- **Resiliencia**, **rate limiting**, **versionado de API** y **security headers**.
- Tests de **arquitectura** que rompen el build si se viola la regla de dependencias.

---

## 2. Estructura de la solución

Definida en `Camilo.OrderFlow.slnx`:

```
src/
├── BuildingBlocks/
│   ├── Camilo.OrderFlow.BuildingBlocks.SharedKernel/      ← tipos compartidos (excepciones)
│   └── Camilo.OrderFlow.BuildingBlocks.Infrastructure/    ← abstracciones reutilizables
├── Host/
│   └── Camilo.OrderFlow.WebApi/                           ← entrada HTTP, DI root
└── Modules/
    └── Identity/
        ├── Camilo.OrderFlow.Modules.Identity.Domain/         ← entidades, reglas
        ├── Camilo.OrderFlow.Modules.Identity.Application/    ← casos de uso (CQRS dispatcher)
        └── Camilo.OrderFlow.Modules.Identity.Infrastructure/ ← EF Core, JWT, repos
tests/
├── Camilo.OrderFlow.ArchitectureTests/                    ← Clean Architecture no violable
├── Camilo.OrderFlow.Identity.UnitTests/                   ← tests de dominio y handlers
├── Camilo.OrderFlow.WebApi.IntegrationTests/              ← endpoints con SQLite in-memory
└── Camilo.OrderFlow.WebApi.IntegrationTests.RealDb/       ← E2E con SQL Server real (Testcontainers)
```

Cada módulo tiene **3 proyectos** propios — `Domain`, `Application` e `Infrastructure` — y comparte el `WebApi` host con los demás. Para crear otro módulo (por ejemplo, `Catalog`), se replica el mismo patrón:

```
Camilo.OrderFlow.Modules.Catalog.Domain
Camilo.OrderFlow.Modules.Catalog.Application
Camilo.OrderFlow.Modules.Catalog.Infrastructure
```

---

## 3. Regla de oro: dirección de dependencias

```
WebApi → Infrastructure → Application → Domain
```

**Nunca al revés.** Esta regla está protegida por `tests/Camilo.OrderFlow.ArchitectureTests/CleanArchitectureTests.cs` y rompe el build de CI si se viola.

| Proyecto | Puede referenciar |
|---|---|
| `*.Domain` | Solo `BuildingBlocks.SharedKernel` |
| `*.Application` | `Domain`, `SharedKernel` |
| `*.Infrastructure` | `Application`, `Domain` |
| `WebApi` | `Infrastructure`, `Application` |
| `BuildingBlocks.SharedKernel` | Nada |
| `BuildingBlocks.Infrastructure` | `SharedKernel` |

---

## 4. Responsabilidad de cada capa

### 4.1 Domain (núcleo)

- **Entidades** con encapsulamiento (`private set`, factory por constructor).
- **Value Objects** (cuando aplique).
- **Interfaces de dominio** (`IUserRepository`, `IUnitOfWork`).
- **Excepciones de negocio** (heredan de `DomainException` en SharedKernel).
- **No** depende de EF Core, ASP.NET, ni de ninguna librería de infraestructura.

> Hoy ya existen: `User`, `Role`, `Permission`, `UserRole`, `RolePermission`.

### 4.2 Application (casos de uso)

- **Commands / Queries** con el dispatcher propio (`IRequest<T>` + `IRequestHandler<T, R>` en `BuildingBlocks.SharedKernel.Messaging`).
- **DTOs** y mapeos con **Mapperly** (source generator) o métodos de extensión manuales.
- **Validators** con FluentValidation (se ejecutan automáticamente vía `ValidationPipelineBehavior`).
- **Contratos** (interfaces) que Infrastructure implementa.
- **No** depende de EF Core ni de ASP.NET.

### 4.3 Infrastructure (detalles)

- **EF Core**: `DbContext`, `IEntityTypeConfiguration<T>`, migraciones.
- **Implementaciones de repositorios**.
- **Servicios externos**: JWT, BCrypt, HttpClients, file storage, correo, etc.
- **Seeders** y datos iniciales.
- **DI** del módulo expuesta por `ServiceCollectionExtensions.AddXxxInfrastructure(IConfiguration)`.

### 4.4 WebApi (host)

- **Controllers** y endpoints.
- **Middleware transversal** (correlation id, exception handling, security headers).
- **Configuración cross-cutting**: rate limiting, CORS, Swagger, versioning, observabilidad.
- **DI root**: registra los módulos y los servicios del propio host.
- Es el **único** proyecto que decide cómo se exponen las cosas hacia afuera.

### 4.5 BuildingBlocks

- **SharedKernel**: tipos compartidos sin lógica de negocio (`DomainException`, `NotFoundException`, `ValidationException`, etc.). Lo puede usar cualquier capa.
- **Infrastructure**: abstracciones de infraestructura **reutilizables entre módulos** (por ejemplo, `IDateTimeProvider`, `IExecutionContext` para commit transaccional). Para infra específica de un módulo, va dentro del propio módulo.

---

## 5. Qué se debe respetar al extender la plantilla

1. **No saltarse la dirección de dependencias.** Si los Architecture Tests fallan, el problema está en el código nuevo, no en los tests.
2. **No registrar servicios desde `Program.cs`** que pertenezcan a un módulo. Cada módulo expone su DI mediante:
   ```csharp
   builder.Services.AddXxxApplication();
   builder.Services.AddXxxInfrastructure(builder.Configuration);
   ```
3. **No escribir SQL ni queries de EF en Application.** Application define la intención (Command/Query); Infrastructure resuelve el "cómo".
4. **No poner `Version="..."` en archivos `.csproj`.** Toda versión de NuGet va en `Directory.Packages.props` (gestión central).
5. **No exponer entidades de Domain en respuestas HTTP.** Se mapean con DTOs vía Mapperly (source generator) o métodos de extensión manuales.
6. **Toda escritura de password debe pasar por BCrypt** (o el hasher que se centralice en Infrastructure).
7. **Toda autorización por endpoint usa `[AuthorizePermission(IdentityPermissions.UsersRead)]`**, no `[Authorize(Roles = "...")]`. Los permisos viven en BBDD; los roles solo agrupan permisos.
8. **El `IdentityDbContext` es interno al módulo Identity.** Cada módulo nuevo tendrá su propio `DbContext` con su propio schema.
9. **Cada migración nueva se genera contra el `DbContext` del módulo correspondiente**, no contra un contexto global.
10. **Cada módulo registra su propio `I<Modulo>ExecutionContext : IExecutionContext`**, nunca la interfaz genérica de SharedKernel. Registrar `IExecutionContext` desnudo hace que el último módulo cargado gane y que los handlers de los demás dejen de persistir **sin lanzar ningún error**. El test `ModuleRegistrationTests` lo impide.
11. **Excepciones de negocio heredan de `DomainException`** (o sus hijas: `NotFoundException`, `ConflictException`, `ValidationException`, `ForbiddenException`). El `GlobalExceptionHandler` ya las traduce a `ProblemDetails` con el status correcto.

---

## 6. Convenciones transversales ya implementadas

| Tema | Detalle | Dónde mirar |
|---|---|---|
| **Auth** | JWT Bearer + permisos en cache (**HybridCache**, TTL configurable); códigos en `IdentityPermissions` | `Modules.Identity.Infrastructure/Auth` y `/Authorization` |
| **Errores HTTP** | `ProblemDetails` con `traceId` + `correlationId` | `WebApi/Errors/GlobalExceptionHandler.cs` |
| **Correlation** | Header `X-Correlation-Id`, propagado a logs y a outgoing HttpClients | `WebApi/Middleware/CorrelationIdMiddleware.cs` |
| **Versionado** | Header `api-version`, default `1.0` | `WebApi/Versioning/ApiVersioningExtensions.cs` |
| **Rate limiting** | Política `default` y `auth`, configurables vía `appsettings` | `WebApi/Extensions/ServiceCollectionExtensions.RateLimiting.cs` |
| **Resiliencia HTTP** | Timeout + retry + circuit breaker, vía `Microsoft.Extensions.Http.Resilience` | `WebApi/Extensions/ServiceCollectionExtensions.Resilience.cs` |
| **Security headers** | HSTS, X-Frame-Options, Referrer-Policy, CSP opcional | `WebApi/Security/SecurityHeadersMiddleware.cs` |
| **Health checks** | `/health/live`, `/health/ready` (incluye SQL Server) | `WebApi/Observability/Health` |
| **Observabilidad** | Trazas/métricas: OpenTelemetry → Collector → Tempo/Prometheus; logs: Serilog → Loki cuando el sink está configurado; Grafana consulta los tres | `WebApi/Observability/OpenTelemetry` y `deploy/observability` |
| **Logs** | Serilog estructurado con `traceId`, `spanId`, `correlationId` | `Program.cs` (sección `UseSerilog`) |
| **Tests** | **xunit.v3** sobre Microsoft Testing Platform + **Shouldly**; arquitectura con `NetArchTest` | `tests/` y `global.json` |
| **CI** | GitHub Actions: restore → build → test → coverage | `.github/workflows/ci.yml` |

---

## 7. Ventajas de iniciar con esta plantilla

### 7.1 Ahorro inmediato (lo que ya **no** hay que decidir ni codear)

- Estructura de proyectos y referencias correctas desde el primer commit.
- JWT + autorización por permisos lista, con cache.
- ProblemDetails uniforme para errores 4xx/5xx.
- Logs estructurados correlacionados con trazas distribuidas.
- Stack de observabilidad completo en `docker-compose`.
- CI con tests de arquitectura que protegen al equipo de regresiones de diseño.

### 7.2 Listo para escalar

- **Modular monolith**: cada nuevo dominio (Catalog, Orders, Billing…) se agrega como un módulo independiente con sus propios `Domain`/`Application`/`Infrastructure` y su propio schema en BBDD. Esto **aísla** las dependencias entre dominios desde el día 1.
- **Extracción futura**: la separación favorece reutilizar dominio y casos de uso, pero mover un módulo a red exige contratos, ownership de datos, timeouts, consistencia y validaciones; no es una garantía de cambio mecánico.
- **Versionado de API por header** desde el inicio: permite evolucionar contratos sin romper consumidores.

### 7.3 Calidad y operación

- **Architecture Tests**: las violaciones de Clean Architecture se detectan en CI, no en code review.
- **Health checks** y `/_info` con `commitSha` y `buildDate`: trazabilidad inmediata del artefacto desplegado.
- **Rate limiting** y **security headers** activados por defecto.
- **Resiliencia HTTP** lista para clientes externos (timeout + retry + circuit breaker).
- **Seeders** para datos iniciales en Development sin contaminar Production.

### 7.4 DX (Developer Experience)

- `Directory.Packages.props` centraliza versiones de NuGet (no hay drift entre proyectos).
- `appsettings.Development.json.example` documenta los secretos que cada dev debe configurar localmente.
- Swagger con soporte de bearer token, listo para probar endpoints autenticados.
- Comandos documentados en `README.md` (raíz) para arranque local en 5 pasos.

---

## 8. Cómo crece la plantilla

Cuando se agrega un nuevo módulo de negocio, la regla es:

1. **Crear los 3 proyectos** del módulo siguiendo `Camilo.OrderFlow.Modules.<Nombre>.<Capa>`.
2. **Agregarlos al `Camilo.OrderFlow.slnx`** dentro de la carpeta `/src/Modules/`.
3. **Definir `AddXxxApplication()` y `AddXxxInfrastructure(IConfiguration)`** en `DependencyInjection/ServiceCollectionExtensions.cs` de cada capa.
4. **Registrarlos en `Program.cs`** (dos líneas, sin más).
5. **Crear su propio `DbContext`** con su propio schema y sus propias migraciones.
6. **No referenciar otros módulos directamente.** Si un módulo necesita información de otro, se hace por:
   - Un **puerto en Application del consumidor y adaptador en el host**, hacia la fachada pública del proveedor (patrón previsto, sin ejemplo multimódulo actual).
   - Una llamada HTTP / mensajería (cuando se separe a microservicio).

Esto último es la pieza clave: **diseñar como si ya fueran servicios separados**, aunque corran en el mismo proceso.

---

## 9. Decisiones explícitas (lo que **no** se debe cambiar sin justificación)

- **Permisos en BBDD**, consultados mediante caché: los cambios no requieren reemitir el JWT, pero su propagación depende de invalidación/TTL. La desactivación de usuario no demuestra revocación de access tokens vigentes; requiere una decisión y prueba posterior.
- **Cache de permisos con `HybridCache`** (L1 in-memory + L2 Redis opcional), TTL configurable. La migración a Redis es un setting de config (una línea de DI), sin cambiar el código.
- **Schema separado por módulo en SQL Server** (`identity.Users`, `catalog.Products`, …): facilita la futura extracción a microservicios y deja claro a quién pertenece cada tabla.
- **`_info` endpoint público** con `commitSha` / `buildDate`: necesario para diagnósticos en entornos compartidos.
- **`ClockSkew = TimeSpan.Zero`** en validación de JWT: tokens expirados son rechazados al instante.
- **`PrivateSet` + constructor en entidades**: estado solo se cambia con métodos explícitos del dominio.

---

## 10. Lectura complementaria

- [`docs/03-Guia-Nuevo-Equipo.md`](03-Guia-Nuevo-Equipo.md) — puesta en marcha en un equipo nuevo.
- [`docs/04-Guia-Renombrado-y-Repositorio.md`](04-Guia-Renombrado-y-Repositorio.md) — adaptar la plantilla a un producto y repositorio nuevos.
- [`docs/Standards.md`](Standards.md) — convenciones detalladas, cómo agregar permisos, HttpClients, etc.
- [`docs/Observability.md`](Observability.md) — Grafana, queries útiles de Prometheus/Loki/Tempo.
- [`docs/ReleaseChecklist.md`](ReleaseChecklist.md) — qué validar antes de cada deploy.
- [`docs/02-Instrucciones-IA.md`](02-Instrucciones-IA.md) — convenciones para agentes de IA que trabajen sobre este repo.
- [`docs/05-Plan-Definitivo.md`](05-Plan-Definitivo.md) — decisiones de diseño tomadas durante la construcción de la plantilla (evidencia de qué se construyó y por qué).

## Alcance comprobable

ModuleCatalog descubre ensamblados referenciados y copiados a la salida de tests. Al agregar un módulo, comprobar esas referencias y registrar manualmente Add<Module>Infrastructure y la resolución de su execution context en ModuleRegistrationTests. El aislamiento entre dos módulos todavía no tiene ejemplo ejecutable: hoy solo existe Identity.

HybridCache usa L1 local y L2 Redis opcional. Redis compartido no demuestra revocación inmediata entre réplicas: verificar propagación por clave y por tag y medir la ventana de permisos obsoletos. CacheInvalidationTests solo prueba una WebApiFactory; no hay garantía temporal multiinstancia medida.

Patrón previsto para integración entre módulos: puerto en Application del consumidor y adaptador en el host de composición que llama una fachada pública del proveedor. No añadir referencias directas entre módulos. Este patrón no está implementado en un escenario multimódulo actual; la extracción a red requiere decisiones de contratos, latencia y fallos parciales.

`global.json` selecciona Microsoft Testing Platform sin fijar SDK. CI usa 10.0.x, push/PR a main/develop y tests sin filtro; publica TRX/cobertura sin umbral ni auditoría dedicada de vulnerabilidades.
