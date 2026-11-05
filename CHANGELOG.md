# Changelog

Historial de cambios relevantes de **Camilo.OrderFlow**, del más reciente al más antiguo. Empieza en el commit
`b3ad03a` (2026-11-05); lo anterior está en `git log`. Cada commit explica el porqué en su cuerpo. El estado actual del
proyecto vive en el [README](README.md), no aquí.

Formato de cada entrada: **Qué** cambió, qué es **Provisional** y las **Cifras** del día.

## 2026-11-05 — Deuda funcional heredada de la fase cero

- **Qué:**
  - **Sesiones de usuarios desactivados:** el host comprueba en cada petición autenticada que el usuario sigue activo
    (401 si no) y desactivar revoca sus refresh tokens ([ADR-0029](docs/adr/0029-revocacion-de-sesiones.md),
    reemplaza a ADR-0010). Antes el JWT seguía valiendo hasta 60 min.
  - **Refresh tokens:** consumo atómico (UPDATE condicional en la misma transacción que el token nuevo) y detección
    de reutilización (un token ya rotado revoca toda la cadena). Antes, 8 refresh paralelos daban 5 tokens válidos.
  - **Caché de permisos:** L1 local por defecto 5 → 1 min; ventana entre réplicas documentada y Redis obligatorio con
    varias réplicas ([ADR-0030](docs/adr/0030-cache-de-permisos-con-ventana-acotada.md), reemplaza a ADR-0009).
  - **Validación:** un único `ValidationPipelineBehavior` en BuildingBlocks, registrado una vez y secuencial.
  - **Reintentos HTTP:** el cliente resiliente ya no reintenta `POST`, `PATCH`, `PUT`, `DELETE` ni `CONNECT`.
- **Provisional:** la ventana de 1 min entre réplicas es un límite conocido; eliminarla requiere un backplane de
  invalidación.
- **Cifras:** tests 212 → 227 (SessionRevocation 4, RefreshTokenRotation 3, RefreshTokenConcurrency 1 en RealDb,
  Resilience 5, registro de behaviors 1, unitario de desactivación 1); cada test nuevo falló antes de su arreglo.
  Cobertura total 87,1 % → 88,9 %; 0 avisos; formato OK; modelo de EF sin cambios.

## 2026-11-05 — Superficie pública mínima de Identity

- **Qué:** `internal` por defecto ([ADR-0028](docs/adr/0028-superficie-publica-de-los-modulos.md)). En Identity pasan a `internal` 21 handlers, 8
  validators, el behavior y 3 mappers (Application), y en Infrastructure `AuthService`, `JwtTokenService`,
  `IJwtTokenService`, `PermissionService`, el handler/proveedor/requisito de autorización, las opciones de bloqueo y
  refresh token, el seeder e `IdentityDbContext`. `InternalsVisibleTo` solo hacia los tests del módulo. Nuevo
  `ModuleSurfaceTests` para todos los módulos. Sin cambios de comportamiento.
- **Provisional:** la fachada `Identity.Contracts` se creará con el primer módulo que la necesite.
- **Cifras:** tipos públicos Application 67 → 34 e Infrastructure 21 → 10; tests de arquitectura 22 → 24.

## 2026-11-05 — Grafana: dashboard como código y acceso de solo lectura

- **Qué:** dashboard «Camilo.OrderFlow — API» provisionado desde `deploy/observability/grafana/dashboards/` (20 paneles:
  tráfico, 5xx, latencias, códigos, rate limiting, salientes, runtime y logs de aviso/error), no editable desde la UI.
  Grafana local pasa de Admin anónimo a **Viewer anónimo**; editar requiere `admin` (`GRAFANA_ADMIN_PASSWORD`). El
  datasource de Prometheus declara `timeInterval: 60s`, la resolución real de las métricas (exportación OTLP cada 60 s):
  con 15 s, los `rate()` salían vacíos.
- **Provisional:** sin reglas de alerta (decisión: hasta tener canal de notificación).
- **Cifras:** 19/19 consultas del dashboard devuelven datos con tráfico real; anónimo recibe 403 al crear dashboards;
  revisado visualmente en el navegador.

## 2026-11-05 — Hooks de git

- **Qué:** `.githooks/` con `commit-msg` (Conventional Commits, asunto ≤ 72, sin atribución ni mención a modelos o
  asistentes), `pre-commit` (`dotnet format` de los `.cs` en el commit) y `pre-push` (build Release y suite rápida).
  Se activan con `git config core.hooksPath .githooks` ([ADR-0027](docs/adr/0027-hooks-de-git-con-core-hookspath.md)). Pruebas en
  `eng/probar-commit-msg.sh`.
- **Provisional:** nada.
- **Cifras:** `commit-msg` 24/24 casos y acepta los 35 commits del historial desde `b3ad03a`; `pre-commit` rechaza un
  `.cs` mal formateado y acepta uno correcto; `pre-push` ~31 s (204 tests).

## 2026-11-05 — Umbral de cobertura por proyecto

- **Qué:** `eng/coverage-gate.cs` fusiona los informes Cobertura de todas las suites, excluye migraciones y `obj/`, y
  comprueba los mínimos de `eng/coverage-thresholds.json`; el CI lo ejecuta tras los tests y la skill
  `verificar-cambio` en local con Docker ([ADR-0026](docs/adr/0026-umbral-de-cobertura-por-proyecto.md), reemplaza a
  ADR-0016). Mínimos = línea base redondeada hacia abajo a múltiplo de 5; solo suben.
- **Provisional:** nada.
- **Cifras** (cobertura de líneas fusionada, 210/210 tests con RealDb):

  | Proyecto | Cobertura | Mínimo |
  |---|---|---|
  | BuildingBlocks.Infrastructure | 100,0 % (29/29) | 100 |
  | BuildingBlocks.SharedKernel | 79,2 % (19/24) | 75 |
  | Identity.Application | 100,0 % (510/510) | 100 |
  | Identity.Domain | 98,9 % (94/95) | 95 |
  | Identity.Infrastructure | 93,2 % (454/487) | 90 |
  | WebApi | 72,3 % (529/732) | 70 |
  | **Total** | **87,1 % (1635/1877)** | **85** |

## 2026-11-05 — `TimeProvider` en todo el código

- **Qué:** las 20 lecturas de `DateTime.UtcNow`/`DateTimeOffset.UtcNow` de producción pasan a `TimeProvider`
  ([ADR-0025](docs/adr/0025-timeprovider-en-todo-el-codigo.md), reemplaza a ADR-0017). El dominio recibe la hora por
  parámetro (`new User(…, createdAtUtc)`, `Deactivate(utcNow)`, `IsLockedOut(utcNow)`, `RefreshToken.IsActive(utcNow)`…);
  handlers, `AuthService`, `JwtTokenService`, `RefreshTokenRepository`, el seeder y `AuthController` inyectan
  `TimeProvider`; el uptime de `/_info` usa `Stopwatch`. Se eliminan `IDateTimeProvider` y `SystemDateTimeProvider`.
  Los tests de dominio comparan valores exactos con un reloj fijo (`TestTime`). `SystemClockUsageTests` queda con la
  lista de excepciones vacía.
- **Provisional:** nada. Sin cambios en el modelo de EF (`has-pending-model-changes`: ninguno).
- **Cifras:** tests 210/210 (sin cambio de número); 0 avisos; formato OK; 0 lecturas del reloj en producción.

## 2026-11-05 — Registro con nombre de usuario repetido

- **Qué:** `RegisterUser` comprueba también el nombre de usuario; uno repetido devuelve 409 en vez de 500 (antes lo
  rechazaba el índice único de la base de datos). Test unitario y de integración nuevos; el de integración reproducía
  el 500 antes de la corrección.
- **Provisional:** nada.
- **Cifras:** tests 210/210 (Identity.UnitTests 122, WebApi.IntegrationTests 60, ArchitectureTests 22, RealDb 6);
  0 avisos; formato OK.

## 2026-11-05 — Endurecimiento: datos personales, avisos y versiones fijadas

- **Qué:**
  - Los mensajes de `ConflictException` de `CreateUser` y `RegisterUser` ya no incluyen el correo ni el nombre de
    usuario (llegan al `ProblemDetails`).
  - `TreatWarningsAsErrors=true` en `Directory.Build.props`, también en local
    ([ADR-0023](docs/adr/0023-avisos-como-errores-en-local-y-ci.md), reemplaza a ADR-0014).
  - Versiones fijadas ([ADR-0024](docs/adr/0024-versiones-fijadas-sdk-imagenes-y-acciones.md), reemplaza a ADR-0012):
    SDK `10.0.403` (`latestPatch`) en `global.json` y en el CI; imágenes `sdk:10.0.403`, `aspnet:10.0.14` y
    `mssql/server:2022-CU27-GDR1-ubuntu-22.04`; acciones de GitHub por SHA.
- **Provisional:** `RegisterUser` no comprueba si el nombre de usuario ya existe; con uno repetido, el índice único
  de la base de datos devolvería un 500 en vez de un 409 (pendiente de corregir).
- **Cifras:**

  | Comprobación | Resultado |
  |---|---|
  | Tests | 208/208: ArchitectureTests 22 · Identity.UnitTests 121 · WebApi.IntegrationTests 59 · WebApi.IntegrationTests.RealDb 6 (SQL Server CU27-GDR1) |
  | Avisos (ahora siempre como errores) | 0 en Debug y Release |
  | `dotnet format --verify-no-changes` | OK |
  | Imágenes Docker | `final` y `migrator` construyen con las etiquetas fijadas |
  | Cobertura | No se volvió a medir |

## 2026-11-05 — Seguimiento de la revisión del plan 01

- **Qué:**
  - [ADR-0021](docs/adr/0021-main-principal-y-develop-de-desarrollo.md): `main` como rama principal (se recrea desde
    `develop`) y `develop` como rama de desarrollo, a la que vuelve cada rama corta con un merge. [ADR-0022](docs/adr/0022-clientes-http-de-modulo-en-infrastructure.md): los clientes HTTP de un módulo
    van en su `Infrastructure/Http`, detrás de un puerto.
  - `SystemClockUsageTests`: test de arquitectura que impide lecturas nuevas de `DateTime.UtcNow`/`Now` en producción
    ([ADR-0017](docs/adr/0017-timeprovider-en-codigo-nuevo.md)); hoy hay 20 permitidas en 8 archivos.
  - `OpenTelemetry:Environment` vale `dev` por defecto en `appsettings.json` (antes `production`); el
    ReleaseChecklist pide fijarlo en cada entorno.
  - `.gitattributes` con LF en el repositorio y CRLF/LF en disco según `.editorconfig`.
  - Comentarios corregidos en `CleanArchitectureTests.cs` (adaptador en el host) y `dev/rename.ps1` (ruta archivada).
- **Provisional:** `main` se creó en local; publicarla y marcarla como rama por defecto en GitHub lo hace el
  responsable del repositorio.
- **Cifras:**

  | Comprobación | Resultado |
  |---|---|
  | Tests | 208/208: ArchitectureTests 22 (antes 20) · Identity.UnitTests 121 · WebApi.IntegrationTests 59 · WebApi.IntegrationTests.RealDb 6 |
  | Avisos (`-c Release -p:TreatWarningsAsErrors=true`) | 0 |
  | Paquetes vulnerables | 0 |
  | `dotnet format --verify-no-changes` | OK |
  | Cobertura | No se volvió a medir (sin cambios de código de producción salvo `appsettings.json`) |

## 2026-11-05 — Estructura para trabajar con Claude (plan 01)

- **Qué:**
  - 20 ADR en [`docs/adr/`](docs/adr/README.md): 14 decisiones que ya estaban en el código, la integración entre
    módulos (adaptador en el host) y cuatro reglas solo para código nuevo (`TimeProvider`, `[LoggerMessage]`, Minimal
    APIs con `TypedResults`, constructores primarios).
  - [`docs/architecture.md`](docs/architecture.md) y [`docs/observabilidad.md`](docs/observabilidad.md) como fuentes
    únicas; README reorganizado; [`CLAUDE.md`](CLAUDE.md) como manual operativo.
  - `.claude/`: 11 reglas por ruta, 12 skills, subagente `revisor-arquitectura` con hook de solo lectura
    (`dotnet run`), `settings.json` sin atribución y con denegaciones; `.mcp.json` con Microsoft Learn.
  - `.editorconfig`: `csharp_style_prefer_primary_constructors = true:suggestion`. `.gitignore`: `.claude/` versionado
    salvo `settings.local.json` y `CLAUDE.local.md`.
  - Guías antiguas (`01-Arquitectura`, `02-Instrucciones-IA`, `03-Guia-Nuevo-Equipo`, `04-Guia-Renombrado…`,
    `05-Plan-Definitivo`, `Standards`, `Observability`) movidas a `docs/archive/` con nota de reemplazo.
  - Sin cambios en código, proyectos, paquetes ni CI.
- **Provisional:**
  - Integración entre módulos y Minimal APIs: decididas, sin ejemplo en el código todavía. El primer endpoint Minimal
    API añadirá `Asp.Versioning.Http`.
  - Cobertura sin umbral (se propone uno con estas cifras). Sin hooks de git.
  - Ruta a microservicios: pendiente, sin criterios decididos.
- **Cifras** (`b3ad03a`, SDK 10.0.403, sin cambios de código en este plan):

  | Comprobación | Resultado |
  |---|---|
  | Tests | 206/206: ArchitectureTests 20 · Identity.UnitTests 121 · WebApi.IntegrationTests 59 · WebApi.IntegrationTests.RealDb 6 |
  | Avisos (`-c Release -p:TreatWarningsAsErrors=true`) | 0 en los 10 proyectos |
  | Paquetes vulnerables (`--include-transitive`) | 0 |
  | `dotnet format --verify-no-changes` | OK |
  | Cobertura de líneas, tests unitarios | Identity.Application 76,6 % · Identity.Domain 92,6 % · SharedKernel 70,8 % |
  | Cobertura de líneas, integración SQLite | WebApi 56,3 % · Identity.Infrastructure 23,3 % (incluye migraciones) · Identity.Application 98,8 % · Identity.Domain 87,4 % |
  | Cobertura de líneas, RealDb | Identity.Infrastructure 92,2 % · WebApi 45,1 % |

  La cobertura es por ejecución (no fusionada).
