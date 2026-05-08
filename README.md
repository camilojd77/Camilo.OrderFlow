# Company.Product — Backend Template

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
| Tests | Unit tests (Shouldly + NSubstitute), Integration tests (SQLite), Architecture tests, RealDb (Testcontainers) |
| CI | GitHub Actions — restore → build → test → coverage |

---

## Prerrequisitos

| Herramienta | Versión mínima | Notas |
|---|---|---|
| .NET SDK | 10.0 | `dotnet --version` |
| Docker Desktop | 4.x | Para stack de observabilidad y tests RealDb |
| SQL Server | 2019+ | LocalDB, contenedor Docker o instancia local |

> **Mapperly (source generator):** IntelliSense no reconoce los mappers generados hasta el primer `dotnet build`. Compila antes de consumir un mapper nuevo.

---

## Arranque rápido (5 pasos)

### 1. Clonar

```pwsh
git clone <url-del-repo>
cd Company.Product
```

### 2. Configurar secretos locales

```pwsh
Copy-Item Company.Product.WebApi\appsettings.Development.json.example `
          Company.Product.WebApi\appsettings.Development.json
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
dotnet ef database update `
  --project Company.Product.Modules.Identity.Infrastructure `
  --startup-project Company.Product.WebApi
```

Aplica las 4 migraciones en orden:
1. `20260304152336_InitialIdentity`
2. `20260507202105_AddUserProfile`
3. `20260507203931_AddUserLockout`
4. `20260507203947_AddRefreshTokens`

### 5. Ejecutar

```pwsh
dotnet run --project Company.Product.WebApi
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
Company.Product.WebApi/Company.Product.WebApi.http
```
(compatible con Visual Studio y VS Code + REST Client)

---

## Tests

```pwsh
# Suite rápida (unit + architecture + integración SQLite) — no requiere Docker
dotnet test --filter "Category!=RealDb"

# Suite completa con SQL Server real vía Testcontainers — requiere Docker
dotnet test --filter "Category=RealDb"

# Solo architecture tests
dotnet test tests/Company.Product.ArchitectureTests

# Con coverage
dotnet test --filter "Category!=RealDb" --collect:"XPlat Code Coverage"
```

**Cobertura de referencia (plantilla base):**
- Unit tests (Domain): ~80% líneas
- Integration tests (API): ~47% líneas

No hay umbral mínimo en la plantilla; el equipo consumidor decide su umbral.

**Nota sobre la suite RealDb:** arranca un contenedor SQL Server 2022 via Testcontainers. Usa Docker Desktop corriendo. Tiempo: ~30 s.

---

## Estructura del repositorio

```
Company.Product/
├── .github/workflows/        ← CI (GitHub Actions)
├── deploy/
│   ├── database/             ← docker-compose para SQL Server de desarrollo
│   └── observability/        ← docker-compose: OTel Collector, Tempo, Loki, Prometheus, Grafana
├── dev/
│   ├── database.ps1          ← wrapper: dev/database.ps1 up | down
│   └── observability.ps1     ← wrapper: dev/observability.ps1 up | down
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

- [ ] `git clone` + copiar `appsettings.Development.json.example` + `dotnet ef database update` + `dotnet run` arranca en < 5 minutos en máquina sin estado previo
- [ ] Login admin (`admin@company.com` / `Admin123!`) devuelve `accessToken` + cookie `refresh_token`
- [ ] `dotnet test --filter "Category!=RealDb"` → verde (197 tests)
- [ ] `dotnet test --filter "Category=RealDb"` → verde con Docker corriendo (6 tests)
- [ ] `dotnet build --warnaserror` → 0 warnings, 0 errores
- [ ] `dotnet list package --vulnerable` → vacío
- [ ] `dotnet list package --deprecated` → vacío (o documentado)
- [ ] Cada endpoint público tiene `[ProducesResponseType]` y autenticación correcta
- [ ] Architecture Tests cubren todas las reglas de Clean Architecture y aislamiento entre módulos
- [ ] `appsettings.Development.json.example` permite arrancar sin adivinar campos
- [ ] No hay archivos de IDE (`*.user`, `.vs/`, etc.) ni `bin/`/`obj/` versionados
- [ ] Las 4 migraciones EF aplican limpiamente sobre una BD vacía
- [ ] Smoke E2E (Fase E del plan de hand-off) pasa

---

## Documentación de soporte

| Documento | Contenido |
|---|---|
| [`docs/01-Arquitectura.md`](docs/01-Arquitectura.md) | Visión estructural, reglas de dependencias, decisiones explícitas |
| [`docs/02-Instrucciones-IA.md`](docs/02-Instrucciones-IA.md) | Convenciones para agentes IA que extiendan la plantilla |
| [`docs/Standards.md`](docs/Standards.md) | Patrones completos: módulo, entidad, command, query, endpoint, tests |
| [`docs/Observability.md`](docs/Observability.md) | Cómo usar Grafana, Loki, Tempo, Prometheus |
| [`docs/ReleaseChecklist.md`](docs/ReleaseChecklist.md) | Checklist antes de cada deploy a Staging / Production |
| [`docs/05-Plan-Definitivo.md`](docs/05-Plan-Definitivo.md) | Decisiones D1–D10 tomadas durante la construcción (evidencia) |

---

## Variables de entorno útiles en CI / deploy

| Variable | Descripción | Ejemplo |
|---|---|---|
| `COMMIT_SHA` | SHA del commit del artefacto | `84b52e0` |
| `BUILD_DATE` | Timestamp ISO-8601 del build | `2026-05-08T11:00:00Z` |
| `ASPNETCORE_ENVIRONMENT` | Entorno de ejecución | `Production` |

Aparecen en la respuesta de `GET /_info`.

---

## Renombrar la plantilla

Para adaptar a un proyecto concreto, reemplaza `Company.Product` por el nombre real en toda la solución:

1. Renombrar `Company.Product.slnx` → `Acme.Inventory.slnx` (o el nombre elegido).
2. Búsqueda global: `Company.Product` → `Acme.Inventory`, `company-product` → `acme-inventory`, `company_product` → `acme_inventory`.
3. Renombrar directorios y archivos `.csproj`.
4. Actualizar rutas en el archivo `.slnx`.
5. Actualizar `Jwt:Issuer`, `Jwt:Audience`, `OpenTelemetry:ServiceName` y labels de Loki en `appsettings.json`.
6. Verificar: `dotnet test --filter "Category!=RealDb"`.

---

## Licencia / propiedad

Plantilla interna de **EmasT**. No publicar en repositorios públicos sin autorización.
