# Arquitectura

Fuente única de la arquitectura de **Camilo.OrderFlow**. El porqué de cada decisión está en su ADR
([`docs/adr/`](adr/README.md)); aquí se describe **qué hay** y **qué reglas lo protegen**. Las convenciones de
telemetría están en [`observabilidad.md`](observabilidad.md).

## 1. Contexto

Backend en .NET 10 para una plataforma de pedidos, construido a partir de una plantilla de **monolito modular**
([ADR-0002](adr/0002-monolito-modular-clean-architecture.md)). Hoy contiene un único módulo, **Identity** (usuarios,
roles, permisos, autenticación con JWT y refresh tokens). Los módulos de negocio aún no existen.

| Habla con | Cómo |
|---|---|
| Clientes HTTP (front, integraciones) | API REST versionada por cabecera, JWT Bearer |
| SQL Server | EF Core 10, un esquema por módulo |
| Redis (opcional) | Caché L2 de permisos si `Cache:Redis` tiene valor |
| OpenTelemetry Collector | OTLP (trazas y métricas) |
| Loki | Logs de Serilog, si el sink está configurado |
| Servicios HTTP externos | `HttpClient` con resiliencia (cliente de ejemplo `ErpClient`) |

## 2. Estructura de la solución

Solución `Camilo.OrderFlow.slnx`. **Los proyectos están en la raíz del repo**; `/src/...` y `/tests/...` son carpetas
virtuales de la solución.

| Proyecto | Capa | Responsabilidad |
|---|---|---|
| `Camilo.OrderFlow.BuildingBlocks.SharedKernel` | Kernel | Abstracciones transversales: excepciones de dominio, mensajería CQRS (`IRequest`, `IRequestHandler`, `ISender`, `IPipelineBehavior`), paginación, `IExecutionContext`, `IDateTimeProvider` (obsoleto, [ADR-0017](adr/0017-timeprovider-en-codigo-nuevo.md)) |
| `Camilo.OrderFlow.BuildingBlocks.Infrastructure` | Kernel | Implementaciones transversales: `Sender` (dispatcher), reloj del sistema; `AddBuildingBlocks()` |
| `Camilo.OrderFlow.Modules.Identity.Domain` | Dominio | Entidades (`User`, `Role`, `Permission`, `RefreshToken`, relaciones) y value objects |
| `Camilo.OrderFlow.Modules.Identity.Application` | Aplicación | Commands, queries, validators, handlers, DTOs, mappers Mapperly, puertos (`Abstractions/`) |
| `Camilo.OrderFlow.Modules.Identity.Infrastructure` | Infraestructura | `IdentityDbContext`, configuraciones EF, repositorios, migraciones, JWT, BCrypt, autorización por permisos, seed |
| `Camilo.OrderFlow.WebApi` | Host | Composición (`Program.cs`), controladores, middleware, errores, versionado, Swagger, observabilidad, health checks |
| `tests/Camilo.OrderFlow.ArchitectureTests` | Tests | Reglas de capas y aislamiento (NetArchTest) |
| `tests/Camilo.OrderFlow.Identity.UnitTests` | Tests | Dominio y handlers |
| `tests/Camilo.OrderFlow.WebApi.IntegrationTests` | Tests | Endpoints con `WebApplicationFactory` + SQLite |
| `tests/Camilo.OrderFlow.WebApi.IntegrationTests.RealDb` | Tests | Endpoints contra SQL Server real (Testcontainers), trait `Category=RealDb` |

## 3. Reglas de dependencia

```
WebApi ──► Modules.<M>.Infrastructure ──► Modules.<M>.Application ──► Modules.<M>.Domain
   │                 │                              │                        │
   └────────────────►└──────► BuildingBlocks.Infrastructure ──► BuildingBlocks.SharedKernel ◄┘
```

| Proyecto | Puede referenciar | No puede |
|---|---|---|
| `Modules.<M>.Domain` | SharedKernel | Application, Infrastructure, WebApi, otro módulo; EF Core, ASP.NET Core, FluentValidation, Mapperly, `SharedKernel.Messaging`, MediatR, AutoMapper |
| `Modules.<M>.Application` | Domain del módulo, SharedKernel; FluentValidation, Mapperly | Infrastructure, WebApi, otro módulo; EF Core, ASP.NET Core, MediatR, AutoMapper |
| `Modules.<M>.Infrastructure` | Application y Domain del módulo, SharedKernel, BB.Infrastructure; EF Core, ASP.NET Core | WebApi, otro módulo |
| `BuildingBlocks.Infrastructure` | SharedKernel | Cualquier módulo, WebApi |
| `BuildingBlocks.SharedKernel` | — | Todo lo demás |
| `WebApi` | Todo lo anterior | — (es la composición) |

**Dónde se valida** ([ADR-0003](adr/0003-validar-capas-con-tests-de-arquitectura.md)):

1. Referencias entre `.csproj`.
2. `tests/Camilo.OrderFlow.ArchitectureTests/CleanArchitectureTests.cs`: dirección de dependencias, dependencias
   prohibidas por capa y aislamiento entre módulos. Los módulos se descubren por nombre de ensamblado
   (`ModuleCatalog.cs`), así que un módulo nuevo queda cubierto en cuanto el host lo referencia.
3. `ModuleRegistrationTests.cs`: nadie registra `IExecutionContext` a secas; cada módulo registra el suyo. El registro
   de cada módulo nuevo se añade **a mano** en este test.

Una violación rompe `dotnet test`, no `dotnet build`.

## 4. Módulos y su contrato público

| Módulo | Esquema | Lo que usa el host hoy |
|---|---|---|
| Identity | `identity` | Commands y queries de `Application` (vía `ISender`), `IdentityPermissions` y `AuthorizePermission` (autorización), `IAuthService` y `JwtOptions` (login y JWT), `SeedIdentityDataAsync` |

**No hay proyecto `*.Contracts` ni tipos `internal` que limiten la superficie**: todo `Application` es público y el host
lo consume directamente. Para que otro módulo use Identity hará falta la **fachada pública** de
[ADR-0006](adr/0006-integracion-modulos-adaptador-en-host.md); delimitarla es trabajo del primer caso real.

## 5. Dentro de un módulo: la cadena de un caso de uso

1. **Request**: `sealed record XCommand(...) : IRequest<TResponse>` (o `XQuery`) en
   `Application/Commands|Queries/<Agregado>/<CasoDeUso>/`.
2. **Validación**: `XCommandValidator : AbstractValidator<XCommand>` (FluentValidation), ejecutado por
   `ValidationPipelineBehavior` antes del handler. Un fallo lanza `ValidationException` → 400.
3. **Handler**: `XCommandHandler : IRequestHandler<XCommand, TResponse>`; usa puertos de `Application/Abstractions`
   (repositorios, `IPasswordHasher`, `ICurrentUser`…), opera sobre entidades y confirma con
   `I<Modulo>ExecutionContext.CommitAsync(ct)` ([ADR-0005](adr/0005-dbcontext-esquema-y-execution-context-por-modulo.md)).
4. **Mapeo**: mapper Mapperly en `Application/Mapping` → DTO de `Application/Dtos`.
5. **Infraestructura**: repositorio en `Infrastructure/Persistence/Repositories` (no llama a `SaveChanges`),
   configuración EF en `Persistence/Configurations`, clientes HTTP propios del módulo en `Infrastructure/Http`
   ([ADR-0022](adr/0022-clientes-http-de-modulo-en-infrastructure.md)), registro en `Add<Modulo>Infrastructure`.
6. **Endpoint**: el host llama a `ISender.Send(...)`. Hoy son controladores; los endpoints nuevos son Minimal APIs
   ([ADR-0019](adr/0019-endpoints-nuevos-minimal-apis.md)).

Los handlers se registran por reflexión en `Add<Modulo>Application()`. Ejemplo completo:
`Camilo.OrderFlow.Modules.Identity.Application/Commands/Users/CreateUser/` y `WebApi/Controllers/UsersController.cs`.

Orden de implementación: Domain (y sus tests) → Application (puertos, request, validator, handler, DTO, mapper, tests
del handler) → Infrastructure (implementación, configuración EF, migración) → endpoint (y tests de integración) →
seed, configuración y documentación. Nunca se empieza por el endpoint.

### Nomenclatura

| Elemento | Patrón | Ejemplo |
|---|---|---|
| Proyecto | `Camilo.OrderFlow.Modules.<Modulo>.<Capa>` · `Camilo.OrderFlow.BuildingBlocks.<Bloque>` | `Camilo.OrderFlow.Modules.Identity.Domain` |
| Namespace | Igual a la ruta de carpetas dentro del proyecto | `…Identity.Application.Commands.Users.CreateUser` |
| Command / handler / validator | `<Verbo><Nombre>Command`, `…CommandHandler`, `…CommandValidator` | `CreateUserCommand` |
| Query | `Get<Nombre>Query` · `List<Nombre>Query` (+ `Handler`) | `GetUserByIdQuery`, `ListUsersQuery` |
| DTO | `<Nombre>Dto` · `<Nombre>Response` | `UserDto`, `LoginResponse` |
| Repositorio | `I<Entidad>Repository` / `<Entidad>Repository` | `IUserRepository` |
| Configuración EF | `<Entidad>Configuration` | `UserConfiguration` |
| Mapper | `<Entidad>Mapper` (partial, Mapperly) | `UserMapper` |
| Extensión de DI | `ServiceCollectionExtensions.cs`, partida por tema (`.Cors.cs`, `.Resilience.cs`) | `ServiceCollectionExtensions.RateLimiting.cs` |
| Permiso | `<recurso>.<accion>` en minúsculas, constante en `<Modulo>Permissions` | `users.read` → `IdentityPermissions.UsersRead` |
| Ruta HTTP | `api/<recurso-en-plural>` | `api/users` |

## 6. Comunicación entre módulos

- **Hoy**: no hay ninguna (un solo módulo). Sin eventos de integración, outbox/inbox ni broker.
- **Regla** ([ADR-0006](adr/0006-integracion-modulos-adaptador-en-host.md)): puerto en `Application` del consumidor,
  fachada pública en el proveedor y adaptador **en el host**. Ningún módulo referencia a otro.
- Mensajería asíncrona (eventos, outbox) no está decidida: requiere ADR antes de introducirla.

## 7. Datos

| Tema | Regla |
|---|---|
| Propiedad | Cada módulo es dueño de sus tablas, en su esquema (`identity.Users`, …) y con su `DbContext` |
| Historial de migraciones | `<esquema>.__EFMigrationsHistory` (`MigrationsHistoryTable` en `Add<Modulo>Infrastructure`) |
| Migraciones | En `<Modulo>.Infrastructure/Migrations`; se aplican fuera del arranque ([ADR-0011](adr/0011-migraciones-fuera-del-arranque.md)); una aplicada no se edita |
| Transacciones | Una por caso de uso y por módulo (`CommitAsync`); no hay transacciones entre módulos |
| Consultas | `AsNoTracking` en lecturas; nunca joins entre esquemas de módulos distintos |
| Seed | `IdentityDataSeeder`: en Development (o con `Seed:Enabled=true`) crea permisos, rol administrador y usuario de demostración; fuera de Development el usuario recibe un hash inutilizable |

## 8. API

| Tema | Lo que hay |
|---|---|
| Estilo | Controladores (`[ApiController]`, rutas `api/<recurso>`); endpoints nuevos con Minimal APIs ([ADR-0019](adr/0019-endpoints-nuevos-minimal-apis.md)) |
| Versionado | Cabecera `api-version`, 1.0 por defecto ([ADR-0008](adr/0008-versionado-por-cabecera-y-swashbuckle.md)) |
| OpenAPI | Swashbuckle, un documento por versión, UI en Development y Staging |
| Errores | Excepciones de dominio → `GlobalExceptionHandler` → `ProblemDetails` con `traceId` y `correlationId` ([ADR-0007](adr/0007-errores-excepciones-dominio-problemdetails.md)) |
| Autenticación | JWT Bearer, `ClockSkew = 0`; refresh token en cookie ([ADR-0010](adr/0010-identidad-propia-bcrypt-jwt.md)) |
| Autorización | Por permiso: `[AuthorizePermission(IdentityPermissions.X)]` ([ADR-0009](adr/0009-autorizacion-por-permisos-con-hybridcache.md)) |
| Transversales | Rate limiting (`default` en todo, `auth` en autenticación), security headers, CORS, límites de Kestrel, `X-Correlation-Id`, resiliencia de `HttpClient` |
| Diagnóstico | `/health/live`, `/health/ready` (SQL Server), `/health`, `/_info` (versión, commit, fecha de build) |

## 9. Configuración en runtime

- `appsettings.json` sin secretos; `appsettings.Development.json` local (ignorado) desde `.example`; variables de
  entorno en contenedores ([ADR-0013](adr/0013-configuracion-sin-secretos-y-opciones-validadas.md)).
- Secciones con clase `*Options` validadas al arrancar: `Jwt`, `OpenTelemetry`, `SecurityHeaders`, `Cors`,
  `RateLimiting`, `Resilience`.
- Cadenas de conexión: `DefaultConnection` (EF) y `Db` (health check), las dos obligatorias.

## 10. Despliegue

- `Dockerfile` multietapa: `build` → `migrator` (ejecuta `dotnet ef database update` y termina) → `final` (ASP.NET
  runtime, usuario sin privilegios, puerto 8080, `HEALTHCHECK` a `/health/live`; `COMMIT_SHA` y `BUILD_DATE` llegan a
  `/_info`).
- `deploy/app/docker-compose.yml`: SQL Server + migrator + API para demos y QA. **No** es un despliegue de producción.
- No hay pipeline de despliegue; el CI (`.github/workflows/ci.yml`) compila y prueba en push y PR a `develop`, la única
  rama de larga vida ([ADR-0021](adr/0021-una-sola-rama-develop.md)); su disparador de `main` ya no aplica.

## 11. Ruta a microservicios (pendiente)

No hay criterios de extracción decididos ni ningún módulo extraído. Lo que **ya** protege una extracción futura:

- Un `DbContext`, un esquema y un historial de migraciones por módulo.
- Aislamiento entre módulos comprobado por test; integración solo por puerto + adaptador en el host.
- Domain y Application sin EF Core ni ASP.NET Core.

Lo que **no se permite hoy** porque impediría extraer un módulo: referencias entre módulos, joins o claves foráneas
entre esquemas de módulos distintos, transacciones que abarquen dos módulos, y tipos de un módulo en la firma pública
de otro.

Al extraer un módulo cambiaría, como mínimo: el adaptador del host pasa a HTTP/gRPC o mensajería; el módulo tiene su
base de datos y su `service.name`; la fachada pública se convierte en contrato versionado. Los criterios para decidir
cuándo extraer quedan **pendientes** (sin ADR).

## 12. Decisiones

Índice completo en [`docs/adr/README.md`](adr/README.md).
