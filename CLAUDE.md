# CLAUDE.md — Camilo.OrderFlow

Instrucciones para personas nuevas y para modelos de IA. El estado y el stack están en el [README](README.md); el
historial, en el [CHANGELOG](CHANGELOG.md); la arquitectura, en [docs/architecture.md](docs/architecture.md); la
observabilidad, en [docs/observabilidad.md](docs/observabilidad.md); las decisiones, en [docs/adr/](docs/adr/README.md).
Este archivo no los repite. Documentación en español; código, commits y ramas en inglés.

## 1. Primera vez que clonas

1. Trabaja desde `develop` (rama de desarrollo); `main` es la principal. SDK .NET `10.0.403` o un parche posterior de la banda 10.0.4xx (`global.json`).
2. `dotnet tool restore` (trae `dotnet-ef`) y `dotnet restore Camilo.OrderFlow.slnx`.
3. Copia `Camilo.OrderFlow.WebApi/appsettings.Development.json.example` a `appsettings.Development.json` (ignorado) y
   rellena las dos cadenas de conexión y `Jwt:Key` (≥ 32 caracteres).
4. Dependencias: `.\dev\database.ps1 up` (SQL Server); opcional `.\dev\observability.ps1 up` (Grafana y compañía).
5. Migraciones: `dotnet ef database update --project Camilo.OrderFlow.Modules.Identity.Infrastructure --startup-project Camilo.OrderFlow.WebApi --context IdentityDbContext`.
6. `dotnet run --project Camilo.OrderFlow.WebApi` (o `--launch-profile Local+OTel` con telemetría).
7. Comprueba: `http://localhost:5070/health/ready` → 200; Swagger en `/swagger`; con el stack, la traza de esa
   petición en Grafana → Explore → Tempo (servicio `Camilo.OrderFlow.WebApi`).

Hooks de git: `git config core.hooksPath .githooks` (una vez por clon; [ADR-0027](docs/adr/0027-hooks-de-git-con-core-hookspath.md)). Detalle y problemas
frecuentes: [README](README.md) § Desarrollo local y § Resolución de problemas.

## 2. Arquitectura

Monolito modular; los proyectos están **en la raíz** (no en `src/`). Tabla completa en
[docs/architecture.md](docs/architecture.md) §2.

| Proyecto | Capa | Puede referenciar |
|---|---|---|
| `BuildingBlocks.SharedKernel` | Kernel | nada |
| `BuildingBlocks.Infrastructure` | Kernel | SharedKernel |
| `Modules.<M>.Domain` | Dominio | SharedKernel |
| `Modules.<M>.Application` | Aplicación | Domain del módulo, SharedKernel |
| `Modules.<M>.Infrastructure` | Infraestructura | Application y Domain del módulo, BuildingBlocks |
| `WebApi` | Host | todo (compone) |

- **Un módulo nunca referencia a otro.** Integración: puerto en Application del consumidor + adaptador en el host
  ([ADR-0006](docs/adr/0006-integracion-modulos-adaptador-en-host.md)).
- **Dónde se valida**: referencias de `.csproj` + `tests/Camilo.OrderFlow.ArchitectureTests` (NetArchTest: dirección
  de capas, paquetes prohibidos, aislamiento, execution context por módulo) y `SystemClockUsageTests` (sin
  `DateTime.UtcNow`). Rompe `dotnet test`.
- **Superficie pública mínima** ([ADR-0028](docs/adr/0028-superficie-publica-de-los-modulos.md)): `internal` por defecto. Público solo requests, DTOs,
  puertos de `Abstractions`, `Add<Modulo>…` y lo que el host necesita para componer; handlers, validators, mappers,
  implementaciones y `DbContext` son `internal` (tests con `InternalsVisibleTo`; lo comprueba `ModuleSurfaceTests`). La
  fachada para otros módulos (`<M>.Contracts`) se crea con el primer consumidor.
- Hoy solo existe el módulo **Identity** (esquema `identity`).

## 3. Cómo implementar algo nuevo

**¿Dónde va?** Regla de negocio de un dominio → su módulo (Domain si protege la entidad; Application si orquesta).
Algo transversal y estable que usarán varios módulos → BuildingBlocks. HTTP, middleware, composición o adaptadores
entre módulos → host. En la duda, en el módulo.

| Tarea | Skill |
|---|---|
| Caso de uso (command/query) | `nueva-funcionalidad` |
| Endpoint HTTP nuevo (Minimal API) | `nuevo-endpoint` |
| Permiso nuevo | `nuevo-permiso` |
| Módulo nuevo | `/nuevo-modulo` (manual) |
| Cambio de esquema | `migracion-ef` |
| Span, métrica o log | `instrumentar` |
| Investigar un fallo con trazas/logs/métricas | `diagnosticar` |
| Decisión de arquitectura | `nuevo-adr` |
| Paquetes NuGet, herramientas, imágenes | `/actualizar-dependencias` (manual, solo si se pide) |
| Dudas sobre la documentación oficial de .NET | `dotnet-referencia` |
| Verificar antes de commitear / commitear | `verificar-cambio` / `/commit` |

**Reglas de código** (detalle por ruta en `.claude/rules/`):

- Nullable activado y respetado; `sealed` en clases que no se heredan; `record` para requests y DTOs.
- `CancellationToken` de punta a punta; nada de `.Result`, `.Wait()` ni `async void`.
- Handlers con repositorios y `I<Modulo>ExecutionContext.CommitAsync`; nunca `DbContext` en handlers ni endpoints.
- Errores con excepciones de `SharedKernel.Exceptions`; mensajes sin datos personales.
- Configuración por `IOptions<T>` validadas al arrancar; nunca `IConfiguration` en Domain o Application.
- La hora viene de `TimeProvider` inyectado; el dominio la recibe por parámetro. Nunca `DateTime.UtcNow`
  ([ADR-0025](docs/adr/0025-timeprovider-en-todo-el-codigo.md)).
- **Solo en código nuevo** (lo existente no se migra): constructores primarios
  ([ADR-0020](docs/adr/0020-constructores-primarios-en-codigo-nuevo.md)), logs con `[LoggerMessage]`
  ([ADR-0018](docs/adr/0018-loggermessage-para-logs-nuevos.md)), endpoints como Minimal APIs con `TypedResults`
  ([ADR-0019](docs/adr/0019-endpoints-nuevos-minimal-apis.md)).
- C# 14 se puede usar libremente en código nuevo; migrar código existente a sintaxis nueva no.

## 4. Observabilidad

1. Trazas y métricas por OpenTelemetry/OTLP; logs por Serilog ([ADR-0015](docs/adr/0015-telemetria-otel-y-serilog-loki.md)).
2. Un `ActivitySource` y un `Meter` por módulo, `Camilo.OrderFlow.<Modulo>`, registrados en `OpenTelemetryExtensions.cs`
   y anotados en el catálogo de [docs/observabilidad.md](docs/observabilidad.md) §3.2.
3. Convenciones semánticas de OpenTelemetry; métricas con unidad.
4. Nunca IDs, correos ni rutas con parámetros como etiqueta de métrica.
5. Sin datos personales ni secretos en logs, atributos ni etiquetas.
6. Logs nuevos con `[LoggerMessage]`, plantilla con propiedades, nunca interpolación.
7. Toda llamada saliente propaga el contexto (`HttpClient` de DI, `CorrelationIdHandler`).
8. Un flujo de negocio nuevo dice qué señal lo observa.

## 5. Pruebas

| Qué | Comando |
|---|---|
| Todo (RealDb necesita Docker) | `dotnet test` |
| Rápida, sin Docker | `dotnet test -- --filter-not-trait "Category=RealDb" --ignore-exit-code 8` |
| Solo SQL Server real | `dotnet test -- --filter-trait "Category=RealDb" --ignore-exit-code 8` |
| Un proyecto | `dotnet test --project tests/<Proyecto>/<Proyecto>.csproj` |
| Un test o clase | `dotnet test --project <csproj> -- --filter-method "*Nombre*"` (o `--filter-class`) |
| Cobertura | `dotnet test -- --coverage --coverage-output-format cobertura --report-trx` |
| Build como el CI (avisos = errores) | `dotnet build Camilo.OrderFlow.slnx -c Release` |

xUnit v3 sobre Microsoft.Testing.Platform: los argumentos del runner van después de `--`. En Git Bash, `-p:` y no
`/p:`. Cobertura mínima por proyecto en `eng/coverage-thresholds.json` (solo sube): tras la suite completa con
cobertura, `dotnet run eng/coverage-gate.cs -- <directorio-de-resultados>`. Convenciones: `.claude/rules/pruebas.md` y
[ADR-0026](docs/adr/0026-umbral-de-cobertura-por-proyecto.md).

## 6. Commits

- Conventional Commits en **inglés**: `type(scope): subject`; el cuerpo explica el porqué. Ramas
  `<tipo>/<tema-corto>` desde `develop`.
- **Al terminar una rama y estar estable** (`verificar-cambio` en verde): merge local a `develop`
  (`git merge --no-ff`) y borrar la rama corta. `main` solo avanza desde `develop` cuando la persona lo pide.
- **Sin atribución de IA** (ni `Co-Authored-By`, ni `Generated with`): `.claude/settings.json` la desactiva.
- Mensaje en archivo UTF-8 sin BOM y `git commit -F`; nunca `--no-verify`.
- Hooks (`.githooks/`): `commit-msg` exige el formato y rechaza la atribución; `pre-commit` comprueba el formato de
  los `.cs`; `pre-push` compila y pasa la suite rápida. Si uno falla, se corrige la causa.
- Antes: `verificar-cambio`. Para commitear: `/commit`. El push lo hace una persona.

## 7. Decisiones deliberadas (no «arreglar»)

- [ADR-0001](docs/adr/0001-registrar-decisiones-con-adr.md) Las decisiones se registran como ADR; uno aceptado no se edita, se reemplaza.
- [ADR-0002](docs/adr/0002-monolito-modular-clean-architecture.md) Monolito modular con tres capas por módulo.
- [ADR-0003](docs/adr/0003-validar-capas-con-tests-de-arquitectura.md) Capas validadas por referencias y NetArchTest.
- [ADR-0004](docs/adr/0004-dispatcher-propio-y-mapperly.md) Dispatcher propio y Mapperly; sin MediatR, AutoMapper ni FluentAssertions.
- [ADR-0005](docs/adr/0005-dbcontext-esquema-y-execution-context-por-modulo.md) DbContext, esquema y execution context por módulo; repos sin `SaveChanges`.
- [ADR-0006](docs/adr/0006-integracion-modulos-adaptador-en-host.md) Integración entre módulos con adaptador en el host.
- [ADR-0007](docs/adr/0007-errores-excepciones-dominio-problemdetails.md) Excepciones de dominio → `ProblemDetails`, no patrón Result.
- [ADR-0008](docs/adr/0008-versionado-por-cabecera-y-swashbuckle.md) Versión por cabecera `api-version`; Swashbuckle.
- [ADR-0009](docs/adr/0009-autorizacion-por-permisos-con-hybridcache.md) Permisos en base de datos con HybridCache, no roles.
- [ADR-0010](docs/adr/0010-identidad-propia-bcrypt-jwt.md) Reemplazada por ADR-0029.
- [ADR-0011](docs/adr/0011-migraciones-fuera-del-arranque.md) Migraciones fuera del arranque.
- [ADR-0012](docs/adr/0012-versiones-centralizadas-y-pines-transitivos.md) Reemplazada por ADR-0024.
- [ADR-0013](docs/adr/0013-configuracion-sin-secretos-y-opciones-validadas.md) Sin secretos en el repo; opciones validadas al arrancar.
- [ADR-0014](docs/adr/0014-estilo-editorconfig-y-cero-avisos-en-ci.md) Reemplazada por ADR-0023.
- [ADR-0015](docs/adr/0015-telemetria-otel-y-serilog-loki.md) OTel/OTLP para trazas y métricas; Serilog → Loki para logs.
- [ADR-0016](docs/adr/0016-estrategia-de-pruebas.md) Reemplazada por ADR-0026.
- [ADR-0017](docs/adr/0017-timeprovider-en-codigo-nuevo.md) Reemplazada por ADR-0025.
- [ADR-0018](docs/adr/0018-loggermessage-para-logs-nuevos.md) Logs nuevos con `[LoggerMessage]`.
- [ADR-0019](docs/adr/0019-endpoints-nuevos-minimal-apis.md) Endpoints nuevos con Minimal APIs; los controladores se quedan.
- [ADR-0020](docs/adr/0020-constructores-primarios-en-codigo-nuevo.md) Constructores primarios en código nuevo.
- [ADR-0021](docs/adr/0021-main-principal-y-develop-de-desarrollo.md) `main` principal; `develop` de desarrollo; ramas cortas que vuelven a `develop` con merge.
- [ADR-0022](docs/adr/0022-clientes-http-de-modulo-en-infrastructure.md) Clientes HTTP de un módulo en su `Infrastructure/Http`, detrás de un puerto.
- [ADR-0023](docs/adr/0023-avisos-como-errores-en-local-y-ci.md) Avisos como errores en local y en CI; estilo en `suggestion`.
- [ADR-0024](docs/adr/0024-versiones-fijadas-sdk-imagenes-y-acciones.md) CPM; SDK en `global.json`; imágenes por versión exacta; acciones por SHA.
- [ADR-0025](docs/adr/0025-timeprovider-en-todo-el-codigo.md) `TimeProvider` en todo el código; el dominio recibe la hora por parámetro.
- [ADR-0026](docs/adr/0026-umbral-de-cobertura-por-proyecto.md) xUnit v3 + MTP, Shouldly, NSubstitute, SQLite, Testcontainers; cobertura mínima por proyecto con trinquete.
- [ADR-0027](docs/adr/0027-hooks-de-git-con-core-hookspath.md) Hooks de git versionados en `.githooks/` con `core.hooksPath`.
- [ADR-0028](docs/adr/0028-superficie-publica-de-los-modulos.md) `internal` por defecto en los módulos; fachada `Contracts` solo con un consumidor real.
- [ADR-0029](docs/adr/0029-revocacion-de-sesiones.md) Identidad propia; usuario activo comprobado en cada petición; refresh tokens con consumo atómico y detección de reutilización.

## 8. Reglas para modelos de IA

- **Nunca `git push`** ni cambios de remotos; sin atribución de IA en commits ni PR.
- No desactives analizadores ni suprimas avisos (`#pragma warning disable`, `NoWarn`, `[SuppressMessage]`), no bajes
  severidades ni umbrales, no uses `--no-verify`. Se corrige el código.
- No uses `!` para callar la nulabilidad sin justificarlo (el `default!` de entidades EF es la excepción documentada).
- No commitees secretos ni valores reales de entorno; no leas ni muestres `appsettings.Development.json`.
- No añadas ni actualices paquetes, herramientas ni imágenes sin que se pida (`/actualizar-dependencias`).
- No referencies internos de otro módulo; no edites migraciones existentes; no modifiques ni relajes los tests de
  arquitectura para que pase un cambio.
- Pregunta antes de: añadir o quitar proyectos, tocar `Program.cs` para algo que no sea registrar un módulo, cambiar
  el esquema de un módulo existente, borrar archivos o romper el contrato de la API 1.0.
- Solo comandos que existan: los de la solución, `dotnet`, `dotnet ef`, `docker compose` y los scripts de `dev/`.
  Nunca `.\dev\observability.ps1 wipe` ni `down -v` sin que te lo pidan.
- Si el código contradice este archivo, una regla o un ADR, **dilo**; no elijas en silencio.

## 9. Configuración de Claude en el repo

| Ruta | Qué |
|---|---|
| `.claude/rules/` | 11 reglas que se cargan solo al leer o editar archivos de su ruta (dominio, aplicación, infraestructura, migraciones, API, host, building blocks, observabilidad, configuración, pruebas, build/CI) |
| `.claude/skills/` | 12 recetas (tabla de §3, más `verificar-cambio` y `commit`) |
| `.claude/agents/revisor-arquitectura.md` | Revisor de solo lectura: `@agent-revisor-arquitectura revisa <rama> contra develop` |
| `.claude/hooks/git-solo-lectura.cs` | Hook del revisor (`dotnet run`; ~13 s la primera vez); pruebas en `probar-git-solo-lectura.sh` |
| `.claude/settings.json` | Sin atribución; deniega push, `--no-verify`, cambios de remoto, `nuget push`, `ef database drop`, `down -v`, `wipe` |
| `.mcp.json` | Microsoft Learn (documentación oficial de .NET) |

Los `deny` **no son una frontera de seguridad** (`git -C . push` no coincide con `git push *`): las reglas escritas
siguen valiendo. Lo personal va en `CLAUDE.local.md` y `.claude/settings.local.json` (ignorados).

**Mantenimiento (mismo PR).** Si un cambio deja desactualizado este archivo, una regla, una skill, `docs/`, el README
o un ADR, se corrige en el mismo PR. ADR nuevo = fila en `docs/adr/README.md` + línea en §7. Señal nueva = fila en el
catálogo de `docs/observabilidad.md`. Cifras del día en el CHANGELOG; el estado, en el README.
