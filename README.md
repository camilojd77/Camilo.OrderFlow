# Camilo.OrderFlow — Backend Template

Plantilla enterprise para APIs REST en **.NET 10** con Clean Architecture + Modular Monolith, lista para clonar y arrancar en minutos. Diseñada para crecer de monolito modular a microservicios sin reescribir el código de negocio.

---

## Características incluidas

| Área | Detalle |
|---|---|
| Arquitectura | Clean Architecture + Modular Monolith (Domain / Application / Infrastructure / WebApi) |
| Auth | JWT Bearer + refresh token rotativo + autorización basada en permisos en BBDD |
| CQRS | Dispatcher propio (`ISender`) — sin MediatR; validación automática con FluentValidation |
| Mapping | Mapperly (source generator, compile-time) |
| Observabilidad | OpenTelemetry → OTel Collector → Tempo + Loki + Prometheus + Grafana |
| Logging | Serilog estructurado con enrichment de traceId / spanId / correlationId |
| Caché de permisos | HybridCache (L1 in-memory por defecto, L2 Redis opcional con 1 línea de config) |
| Resiliencia | `Microsoft.Extensions.Http.Resilience` — timeout + retry + circuit breaker |
| Rate limiting | Políticas `default` y `auth` configurables por appsettings |
| API versioning | Por header `api-version`, versión por defecto `1.0` |
| Health checks | `/health/live` y `/health/ready` (SQL Server) |
| Seguridad | Security headers, CORS, HTTPS, lockout de cuentas |
| Tests | xunit.v3 sobre Microsoft Testing Platform: unit (Shouldly + NSubstitute), integración (SQLite), arquitectura (NetArchTest), RealDb (Testcontainers) |
| Contenedores | `Dockerfile` multi-stage (usuario no-root, healthcheck) + compose de app completa y de dependencias |
| CI | GitHub Actions — restore → build → test → coverage |
| Adaptación | `dev/rename.ps1` para renombrar la plantilla a un producto nuevo |

---

## Prerrequisitos

| Herramienta | Versión mínima | Notas |
|---|---|---|
| Git | 2.40 | `git --version` |
| .NET SDK | 10.0 | `dotnet --version` |
| `dotnet ef` | — | No viene con el SDK: `dotnet tool restore` (versión fijada en `.config/dotnet-tools.json`) |
| Docker Desktop | 4.x (Compose v2) | `docker compose version`. Necesario para BBDD en contenedor, observabilidad y tests RealDb |
| SQL Server | 2019+ | LocalDB, contenedor Docker o instancia local |
| Certificado HTTPS dev | — | `dotnet dev-certs https --trust`, solo para el perfil `https` |

> **Mapperly (source generator):** IntelliSense no reconoce los mappers generados hasta el primer `dotnet build`. Compila antes de consumir un mapper nuevo.

> **¿Primera vez en este repositorio?** La guía detallada, con contenedores, verificaciones y tabla de troubleshooting, es [`docs/03-Guia-Nuevo-Equipo.md`](docs/03-Guia-Nuevo-Equipo.md). Lo de abajo es la versión corta.

---

## Arranque rápido (5 pasos)

### 1. Clonar

```pwsh
git clone <url-del-repo>
cd Camilo.OrderFlow
```

### 2. Configurar secretos locales

```pwsh
Copy-Item Camilo.OrderFlow.WebApi\appsettings.Development.json.example `
          Camilo.OrderFlow.WebApi\appsettings.Development.json
```

Editar la copia y ajustar al menos:
- `ConnectionStrings:DefaultConnection` → apuntar a tu instancia SQL Server
- `Jwt:Key` → mínimo 32 caracteres (el valor de ejemplo es solo para desarrollo)

### 3. Levantar SQL Server y observabilidad (opcional)

**SQL Server** (si no tienes instancia local):
```pwsh
# Levantar SQL Server 2022 en Docker
.\dev\database.ps1 up

# Derribar cuando termines
.\dev\database.ps1 down
```

**Stack de observabilidad** (Grafana / Loki / Tempo / Prometheus — opcional):
```pwsh
.\dev\observability.ps1 up
```

### 4. Aplicar migraciones

```pwsh
# dotnet ef no viene con el SDK; se instala desde el manifiesto del repo
dotnet tool restore

dotnet ef database update `
  --project Camilo.OrderFlow.Modules.Identity.Infrastructure `
  --startup-project Camilo.OrderFlow.WebApi
```

Aplica las 4 migraciones en orden:
1. `20260304152336_InitialIdentity`
2. `20260507202105_AddUserProfile`
3. `20260507203931_AddUserLockout`
4. `20260507203947_AddRefreshTokens`

### 5. Ejecutar

```pwsh
dotnet run --project Camilo.OrderFlow.WebApi
```

- Swagger → `http://localhost:5070/swagger`
- Info de servicio → `http://localhost:5070/_info`

---

## Validación rápida

Una vez levantada la API, verifica los endpoints principales:

```pwsh
# 1. Login con el admin seed
$login = Invoke-RestMethod -Uri http://localhost:5070/api/auth/login `
  -Method Post -ContentType "application/json" `
  -Body '{"usernameOrEmail":"admin@company.com","password":"Admin123!"}'
$token = $login.accessToken

# 2. Perfil del usuario autenticado
Invoke-RestMethod -Uri http://localhost:5070/api/users/me `
  -Headers @{ Authorization = "Bearer $token" }

# 3. Permisos efectivos del admin
Invoke-RestMethod -Uri http://localhost:5070/api/users/me/permissions `
  -Headers @{ Authorization = "Bearer $token" }
```

También puedes usar el archivo `.http` incluido:
```
Camilo.OrderFlow.WebApi/Camilo.OrderFlow.WebApi.http
```
(compatible con Visual Studio y VS Code + REST Client)

---

## Tests

```pwsh
# Todo (unit + architecture + integración + RealDb) — requiere Docker
dotnet test

# Suite rápida (unit + architecture + integración SQLite) — no requiere Docker
dotnet test -- --filter-not-trait "Category=RealDb" --ignore-exit-code 8

# Solo la suite contra SQL Server real vía Testcontainers — requiere Docker
dotnet test -- --filter-trait "Category=RealDb" --ignore-exit-code 8

# Solo architecture tests
dotnet test --project tests/Camilo.OrderFlow.ArchitectureTests/Camilo.OrderFlow.ArchitectureTests.csproj

# Con coverage (cobertura) y reporte TRX
dotnet test -- --coverage --coverage-output-format cobertura --report-trx
```

> **Sobre la sintaxis:** xunit.v3 corre sobre Microsoft Testing Platform (el runner está
> declarado en `global.json`). Lo que va después de `--` son argumentos de la app de test, no
> del SDK. `--ignore-exit-code 8` es necesario porque, al filtrar, algún proyecto se queda sin
> tests que ejecutar y el runner lo trataría como fallo. Sin filtro (`dotnet test` a secas) corre
> todo y no hace falta.

Registrar cantidades actuales por suite y fecha de ejecución. RealDb excluida o sin casos no acredita SQL Server.

**Cobertura de referencia (plantilla base):**
- Unit tests (Domain): ~80% líneas
- Integration tests (API): ~47% líneas

No hay umbral mínimo en la plantilla; el equipo consumidor decide su umbral.

**Nota sobre la suite RealDb:** arranca un contenedor SQL Server 2022 via Testcontainers. Usa Docker Desktop corriendo. Registrar duración real según el entorno.

---

## Estructura del repositorio

```
<workspace>/
├── .github/workflows/        ← CI (GitHub Actions)
├── Dockerfile                ← imagen de la API (build → migrator → runtime)
├── deploy/
│   ├── app/                  ← docker-compose de la app completa (BBDD + migraciones + API)
│   ├── database/             ← docker-compose para SQL Server de desarrollo
│   └── observability/        ← docker-compose: OTel Collector, Tempo, Loki, Prometheus, Grafana
├── dev/
│   ├── database.ps1          ← wrapper: dev/database.ps1 up | down | logs
│   ├── observability.ps1     ← wrapper: dev/observability.ps1 up | down | logs | status | wipe
│   └── rename.ps1            ← renombra la plantilla para un proyecto nuevo
├── docs/                     ← Documentación técnica (ver tabla abajo)
├── src/ (proyectos — en raíz por convención .slnx)
│   ├── BuildingBlocks/
│   │   ├── SharedKernel/     ← DomainException, dispatcher CQRS (ISender), paginación
│   │   └── Infrastructure/   ← IExecutionContext, abstracciones reutilizables
│   ├── Modules/Identity/
│   │   ├── Domain/           ← User, Role, Permission, value objects, excepciones
│   │   ├── Application/      ← Commands, Queries, DTOs, Mapperly mappers, validators
│   │   └── Infrastructure/   ← EF Core, repos, JWT, BCrypt, HybridCache, seeders
│   └── Host/
│       └── WebApi/           ← Controllers, middleware, DI root, observabilidad, Swagger
└── tests/
    ├── ArchitectureTests/    ← Clean Architecture no violable (NetArchTest)
    ├── Identity.UnitTests/   ← Tests de dominio y handlers (xUnit + Shouldly + NSubstitute)
    ├── WebApi.IntegrationTests/       ← Tests de endpoints con SQLite in-memory
    └── WebApi.IntegrationTests.RealDb/ ← Tests E2E con SQL Server real (Testcontainers)
```

---

## Cómo extender la plantilla

Para agregar un nuevo módulo de negocio (ej. `Catalog`), sigue el [patrón completo de módulo](docs/Standards.md#patrón-completo-de-módulo) en `docs/Standards.md`.

Para contribuciones con IA (Copilot, Claude, Cursor), lee primero [`docs/02-Instrucciones-IA.md`](docs/02-Instrucciones-IA.md) — define reglas no-negociables de arquitectura, nomenclatura y orden de implementación.

---

## Checklist de aprobación TI

El revisor de TI marca estos criterios antes de aprobar la plantilla:

- [ ] `git clone` + copiar `appsettings.Development.json.example` + `dotnet ef database update` + `dotnet run` arranca con prerrequisitos verificados y tiempo real registrado
- [ ] Login admin (`admin@company.com` / `Admin123!`) devuelve `accessToken` + cookie `refresh_token`
- [ ] `dotnet test` → verde con cantidades registradas y casos RealDb realmente ejecutados con Docker
- [ ] `dotnet build /p:TreatWarningsAsErrors=true` → 0 warnings, 0 errores
- [ ] `dotnet list package --vulnerable` → vacío
- [ ] `dotnet list package --deprecated` → vacío
- [ ] Cada endpoint público tiene `[ProducesResponseType]` y autenticación correcta
- [ ] Architecture Tests cubren todas las reglas de Clean Architecture y aislamiento entre módulos
- [ ] `appsettings.Development.json.example` permite arrancar sin adivinar campos
- [ ] No hay archivos de IDE (`*.user`, `.vs/`, etc.) ni `bin/`/`obj/` versionados
- [ ] Las 4 migraciones EF aplican limpiamente sobre una BD vacía
- [ ] `docker compose -f deploy/app/docker-compose.yml up --build` levanta la app y el login del admin funciona
- [ ] Smoke E2E (login → `/api/users/me` → `/api/users/me/permissions`) pasa

---

## Documentación de soporte

Índice completo, por rol y en orden de lectura: [`docs/README.md`](docs/README.md).

| Documento | Contenido |
|---|---|
| [`docs/01-Arquitectura.md`](docs/01-Arquitectura.md) | Visión estructural, reglas de dependencias, decisiones explícitas |
| [`docs/02-Instrucciones-IA.md`](docs/02-Instrucciones-IA.md) | Convenciones para agentes IA que extiendan la plantilla |
| [`docs/03-Guia-Nuevo-Equipo.md`](docs/03-Guia-Nuevo-Equipo.md) | Puesta en marcha paso a paso en un equipo nuevo, contenedores y troubleshooting |
| [`docs/04-Guia-Renombrado-y-Repositorio.md`](docs/04-Guia-Renombrado-y-Repositorio.md) | Renombrar la plantilla y apuntar a un repositorio nuevo |
| [`docs/Standards.md`](docs/Standards.md) | Patrones completos: módulo, entidad, command, query, endpoint, tests |
| [`docs/Observability.md`](docs/Observability.md) | Cómo usar Grafana, Loki, Tempo, Prometheus |
| [`docs/EntenderObservabilidad.md`](docs/EntenderObservabilidad.md) | Ruta de estudio de observabilidad moderna (formación) |
| [`docs/ReleaseChecklist.md`](docs/ReleaseChecklist.md) | Checklist antes de cada deploy a Staging / Production |
| [`docs/05-Plan-Definitivo.md`](docs/05-Plan-Definitivo.md) | Decisiones D1–D10 tomadas durante la construcción (histórico) |
| [`docs/archive/`](docs/archive/) | Planes ya ejecutados; no describen el estado actual |

---

## Variables de entorno útiles en CI / deploy

| Variable | Descripción | Ejemplo |
|---|---|---|
| `COMMIT_SHA` | SHA del commit del artefacto | `84b52e0` |
| `BUILD_DATE` | Timestamp ISO-8601 del build | `2026-05-08T11:00:00Z` |
| `ASPNETCORE_ENVIRONMENT` | Entorno de ejecución | `Production` |

Aparecen en la respuesta de `GET /_info`.

---

## Renombrar la plantilla para un proyecto nuevo

El script conserva el origen Company.Product y solo deriva una copia de plantilla sin renombrar. Este producto ya se llama Camilo.OrderFlow; no volver a ejecutar el script esperando renombrarlo arbitrariamente. Los nombres de contenedor, proyectos de
compose y labels de Loki también forman parte del inventario técnico. Ejemplo sobre una copia de plantilla original:

```pwsh
.\dev\rename.ps1 -NewName "Acme.Inventory" -WhatIf   # simula
.\dev\rename.ps1 -NewName "Acme.Inventory"           # aplica
```

El procedimiento completo — incluido qué queda a mano, qué hacer con las migraciones y cómo apuntar
a otro repositorio — está en [`docs/04-Guia-Renombrado-y-Repositorio.md`](docs/04-Guia-Renombrado-y-Repositorio.md).

---

## Alcance de la base actual

Nombre técnico Camilo.OrderFlow, rama develop; la carpeta física del workspace, historia y remotos se conservan. No se ha autorizado una fase de negocio.

HybridCache usa L1 local y L2 Redis opcional. Redis compartido no demuestra revocación inmediata entre réplicas: verificar propagación por clave y por tag y medir la ventana de permisos obsoletos. CacheInvalidationTests solo prueba una WebApiFactory; no hay garantía temporal multiinstancia medida.

ModuleCatalog descubre ensamblados referenciados y copiados a la salida de tests. Al agregar un módulo, comprobar esas referencias y registrar manualmente Add<Module>Infrastructure y la resolución de su execution context en ModuleRegistrationTests. El aislamiento entre dos módulos todavía no tiene ejemplo ejecutable: hoy solo existe Identity.

CI: push/PR a main/develop, SDK 10.0.x, restore/build/tests sin filtro y publicación TRX/cobertura. Sin nightly, umbral de cobertura ni auditoría dedicada. global.json fija runner, no SDK. Las cifras de cobertura anteriores son antecedentes, no una medición de esta fase.
