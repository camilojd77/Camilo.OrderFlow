# Camilo.OrderFlow

Backend REST en **.NET 10** organizado como **monolito modular** con Clean Architecture por módulo, preparado para
separar módulos en servicios más adelante. Hoy incluye el módulo **Identity** (usuarios, roles, permisos, JWT).

| Para… | Lee |
|---|---|
| Trabajar en el repo (personas y modelos de IA) | [`CLAUDE.md`](CLAUDE.md) |
| Entender la arquitectura | [`docs/architecture.md`](docs/architecture.md) |
| Saber por qué algo es así | [`docs/adr/`](docs/adr/README.md) |
| Observabilidad y diagnóstico | [`docs/observabilidad.md`](docs/observabilidad.md) |
| Historial de cambios con cifras | [`CHANGELOG.md`](CHANGELOG.md) |
| Índice de toda la documentación | [`docs/README.md`](docs/README.md) |

---

## Estado del proyecto

**Listo**

- Módulo Identity completo: registro, login, refresh token rotativo, bloqueo por intentos, usuarios, roles y
  permisos, autorización por permiso con caché HybridCache.
- Tests verdes (unitarios, arquitectura, integración con SQLite y con SQL Server real), cero avisos de compilación con
  avisos como errores, sin paquetes vulnerables, `dotnet format` limpio. Cifras del día en el [`CHANGELOG.md`](CHANGELOG.md).
- Trazas y métricas por OpenTelemetry; logs estructurados con Serilog; stack local Grafana/Tempo/Prometheus/Loki.
- Imagen Docker con contenedor de migraciones; CI en GitHub Actions.

**A tener en cuenta**

- Solo hay un módulo: la integración entre módulos ([ADR-0006](docs/adr/0006-integracion-modulos-adaptador-en-host.md))
  y las Minimal APIs ([ADR-0019](docs/adr/0019-endpoints-nuevos-minimal-apis.md)) no tienen todavía un ejemplo en el
  código.
- La revocación de permisos entre varias réplicas no está medida ([ADR-0009](docs/adr/0009-autorizacion-por-permisos-con-hybridcache.md));
  desactivar un usuario no invalida sus access tokens vigentes.
- `deployment.environment` vale `production` en `appsettings.json` aunque el código use `dev` por defecto.
- El SDK no está fijado en `global.json` ([ADR-0012](docs/adr/0012-versiones-centralizadas-y-pines-transitivos.md)).

**Aplazado a propósito**

- Umbral de cobertura y hooks de git (se reportan / se verifican con la skill `verificar-cambio`).
- Dashboards y alertas como código.
- Criterios de extracción a microservicios ([`docs/architecture.md`](docs/architecture.md) §11).

---

## Requisitos e instalación

| Herramienta | Versión | Comprobar | Notas |
|---|---|---|---|
| Git | 2.40+ | `git --version` | |
| .NET SDK | 10.0 | `dotnet --version` | No está fijado; el CI usa `10.0.x` |
| Docker Desktop | 4.x, Compose v2 | `docker info` | Para SQL Server en contenedor, observabilidad y tests RealDb. En Windows requiere WSL2 |
| SQL Server | 2019+ | — | Contenedor, LocalDB o instancia propia |
| Certificado HTTPS de desarrollo | — | `dotnet dev-certs https --check` | Solo para el perfil `https` |

```pwsh
git clone https://github.com/camilojd77/Camilo.OrderFlow.git
cd Camilo.OrderFlow
git switch develop
dotnet tool restore          # dotnet-ef desde .config/dotnet-tools.json
dotnet restore Camilo.OrderFlow.slnx
```

---

## Desarrollo local

**1. Configuración.** Copia la plantilla de configuración local (ignorada por git) y rellena al menos
`ConnectionStrings:DefaultConnection`, `ConnectionStrings:Db` (el mismo valor; lo usa el health check) y `Jwt:Key`
(≥ 32 caracteres):

```pwsh
Copy-Item Camilo.OrderFlow.WebApi\appsettings.Development.json.example Camilo.OrderFlow.WebApi\appsettings.Development.json
```

**2. Dependencias.**

```pwsh
.\dev\database.ps1 up          # SQL Server 2022 en localhost:1433 (también: logs, down)
.\dev\observability.ps1 up     # opcional: Grafana, Tempo, Prometheus, Loki, Collector
```

Las credenciales de esos contenedores son solo de desarrollo local y están en `deploy/database/` y
`deploy/observability/`.

**3. Migraciones** (no se aplican al arrancar, [ADR-0011](docs/adr/0011-migraciones-fuera-del-arranque.md)):

```pwsh
dotnet ef database update --project Camilo.OrderFlow.Modules.Identity.Infrastructure --startup-project Camilo.OrderFlow.WebApi --context IdentityDbContext
```

**4. Ejecutar.**

```pwsh
dotnet run --project Camilo.OrderFlow.WebApi                              # perfil http
dotnet run --project Camilo.OrderFlow.WebApi --launch-profile Local+OTel  # con telemetría al stack local
```

| URL | Qué |
|---|---|
| http://localhost:5070/swagger | Swagger (Development y Staging) |
| http://localhost:5070/health/live · /health/ready | Health checks |
| http://localhost:5070/_info | Versión, commit y fecha de build |
| http://localhost:3000 | Grafana (si levantaste la observabilidad) |

En Development el seed crea los permisos, el rol administrador y un usuario de demostración (ver
`Camilo.OrderFlow.Modules.Identity.Infrastructure/Persistence/Seed/IdentityDataSeeder.cs`). Para probar login, perfil y
permisos usa `Camilo.OrderFlow.WebApi/Camilo.OrderFlow.WebApi.http`.

**Todo en contenedores** (demo/QA, puertos 8080 y 1434):
`docker compose -f deploy/app/docker-compose.yml up --build`.

**IDE.** Abre `Camilo.OrderFlow.slnx` (Visual Studio, Rider o VS Code con C# Dev Kit). Los mappers de Mapperly son
código generado: compila antes de concluir que un método no existe.

---

## Build, tests y calidad

```pwsh
dotnet build Camilo.OrderFlow.slnx -c Release -p:TreatWarningsAsErrors=true    # como el CI: 0 avisos
dotnet test                                                                     # todo (RealDb necesita Docker)
dotnet test -- --filter-not-trait "Category=RealDb" --ignore-exit-code 8       # suite rápida, sin Docker
dotnet test -- --filter-trait "Category=RealDb" --ignore-exit-code 8           # solo SQL Server real
dotnet test --project tests/Camilo.OrderFlow.ArchitectureTests/Camilo.OrderFlow.ArchitectureTests.csproj
dotnet test -- --coverage --coverage-output-format cobertura --report-trx       # cobertura + TRX
dotnet format Camilo.OrderFlow.slnx --verify-no-changes
dotnet list Camilo.OrderFlow.slnx package --vulnerable --include-transitive
```

- Los tests usan xUnit v3 sobre Microsoft.Testing.Platform (runner en `global.json`): lo que va después de `--` es
  para la app de test. `--ignore-exit-code 8` evita que un proyecto sin casos tras el filtro cuente como fallo; una
  suite RealDb filtrada sin casos **no** valida SQL Server.
- En Git Bash usa `-p:` y no `/p:` (MSYS lo convierte en ruta).
- Sin umbral de cobertura por ahora ([ADR-0016](docs/adr/0016-estrategia-de-pruebas.md)).

---

## Arquitectura

Módulos con tres capas (`Domain ← Application ← Infrastructure`), building blocks compartidos y un host
(`Camilo.OrderFlow.WebApi`) que los compone. Dependencias solo hacia dentro, validadas con tests de arquitectura.
Detalle, reglas y cadena de un caso de uso: [`docs/architecture.md`](docs/architecture.md).

```
Camilo.OrderFlow.BuildingBlocks.SharedKernel/      abstracciones transversales (excepciones, CQRS, paginación)
Camilo.OrderFlow.BuildingBlocks.Infrastructure/    dispatcher, reloj
Camilo.OrderFlow.Modules.Identity.{Domain,Application,Infrastructure}/
Camilo.OrderFlow.WebApi/                           host: Program.cs, controladores, middleware, observabilidad
tests/                                             arquitectura, unitarios, integración (SQLite y RealDb)
deploy/                                            compose: app, database, observability
dev/                                               scripts: database.ps1, observability.ps1, rename.ps1
docs/                                              arquitectura, observabilidad, ADR
```

---

## Configuración y secretos

- `appsettings.json` versionado **sin secretos**; `appsettings.Development.json` local desde el `.example`, ignorado
  por git; variables de entorno (`ConnectionStrings__DefaultConnection`, `Jwt__Key`, …) en contenedores
  ([ADR-0013](docs/adr/0013-configuracion-sin-secretos-y-opciones-validadas.md)).
- La app no arranca si `Jwt:Key` falta o es corta, si falta una cadena de conexión o si una sección validada es
  inválida.

| Variable de build/despliegue | Para qué |
|---|---|
| `COMMIT_SHA`, `BUILD_DATE` | Argumentos de build de la imagen; aparecen en `/_info` |
| `ASPNETCORE_ENVIRONMENT` | Entorno (`Development` activa Swagger y el seed) |

---

## Observabilidad

Trazas y métricas por OpenTelemetry → Collector → Tempo y Prometheus; logs de Serilog → Loki; Grafana consulta todo y
enlaza logs y trazas por `traceId`. Convenciones, catálogo de señales, consultas y runbooks:
[`docs/observabilidad.md`](docs/observabilidad.md).

---

## Despliegue

No hay pipeline de despliegue. El `Dockerfile` produce la imagen de la API (`final`) y la de migraciones (`migrator`);
`deploy/app/docker-compose.yml` sirve para demos y QA, no para producción. Antes de desplegar:
[`docs/ReleaseChecklist.md`](docs/ReleaseChecklist.md).

---

## Stack y versiones

Las versiones exactas de paquetes están en `Directory.Packages.props` (fuente única,
[ADR-0012](docs/adr/0012-versiones-centralizadas-y-pines-transitivos.md)).

| Área | Tecnología |
|---|---|
| Plataforma | .NET 10, C# 14, ASP.NET Core |
| Datos | EF Core 10 + SQL Server; HybridCache (+ Redis opcional) |
| Aplicación | Dispatcher CQRS propio, FluentValidation, Mapperly |
| API | Controladores (Minimal APIs para endpoints nuevos), Asp.Versioning, Swashbuckle, JWT Bearer |
| Resiliencia | `Microsoft.Extensions.Http.Resilience`, rate limiting de ASP.NET Core |
| Observabilidad | OpenTelemetry .NET, Serilog, OTel Collector, Tempo, Prometheus, Loki, Grafana |
| Pruebas | xUnit v3 + Microsoft.Testing.Platform, Shouldly, NSubstitute, NetArchTest, SQLite, Testcontainers |
| CI | GitHub Actions |

---

## Resolución de problemas

| Síntoma | Solución |
|---|---|
| `dotnet ef` no se encuentra | `dotnet tool restore` |
| Error de red o instancia de SQL Server | `.\dev\database.ps1 up` y revisa `ConnectionStrings` |
| SQL Server en contenedor queda `unhealthy` | Dale ≥ 4 GB de RAM a Docker Desktop; `.\dev\database.ps1 logs` |
| Puerto ocupado (`port is already allocated`) | Cambia el puerto **del host** (el primer número) en el compose y el connection string o `OtlpEndpoint` |
| Error de validación de `JwtOptions` al arrancar | `Jwt:Key` ausente o < 32 caracteres |
| `Missing connection string: DefaultConnection` / `Db` | Las dos deben tener valor |
| Swagger no abre en https | `dotnet dev-certs https --trust` |
| Errores rojos en mappers nuevos | `dotnet build` (Mapperly genera el código al compilar) |
| Tests RealDb fallan al arrancar | Docker Desktop apagado (`docker info`) |
| `Testing with VSTest target is no longer supported` | Ejecuta desde la raíz del repo (allí está `global.json`) |
| `dotnet test` con filtro sale con código 8 | Añade `--ignore-exit-code 8` |
| `warning xUnit1051` | Pasa `TestContext.Current.CancellationToken` |
| `warning NU1902`/`NU1903` | `dotnet list package --vulnerable --include-transitive` y `dotnet nuget why`; ver la skill `actualizar-dependencias` |
| `MSB1008` con `/p:` en Git Bash | Usa `-p:` |
| No llegan trazas o logs a Grafana | Runbooks en [`docs/observabilidad.md`](docs/observabilidad.md) §8 |

---

## Contribuir

- Rama base `develop`; `main` solo recibe merges desde `develop`. Ramas `feat/`, `fix/`, `docs/`, `chore/`… en inglés.
- Commits en inglés con Conventional Commits (`type(scope): subject`), sin `--no-verify`. Documentación en español.
- Antes de commitear: compilar con avisos como errores, formato y tests (lo hace la skill `verificar-cambio`).
- Una decisión de arquitectura nueva o cambiada lleva su ADR en el mismo PR.
