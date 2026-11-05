# CLAUDE.md — Camilo.OrderFlow

Instrucciones para personas nuevas y para modelos de IA. El estado y el stack están en el [README](README.md); el
historial, en el [CHANGELOG](CHANGELOG.md); la arquitectura, en [docs/architecture.md](docs/architecture.md); la
observabilidad, en [docs/observabilidad.md](docs/observabilidad.md); las decisiones, en [docs/adr/](docs/adr/README.md).
Este archivo no los repite. Documentación en español; código, commits y ramas en inglés.

## 1. Primera vez que clonas

1. Rama base `develop` (`git switch develop`). SDK .NET 10 (no fijado en `global.json`; el CI usa `10.0.x`).
2. `dotnet tool restore` (trae `dotnet-ef`) y `dotnet restore Camilo.OrderFlow.slnx`.
3. Copia `Camilo.OrderFlow.WebApi/appsettings.Development.json.example` a `appsettings.Development.json` (ignorado) y
   rellena las dos cadenas de conexión y `Jwt:Key` (≥ 32 caracteres).
4. Dependencias: `.\dev\database.ps1 up` (SQL Server); opcional `.\dev\observability.ps1 up` (Grafana y compañía).
5. Migraciones: `dotnet ef database update --project Camilo.OrderFlow.Modules.Identity.Infrastructure --startup-project Camilo.OrderFlow.WebApi --context IdentityDbContext`.
6. `dotnet run --project Camilo.OrderFlow.WebApi` (o `--launch-profile Local+OTel` con telemetría).
7. Comprueba: `http://localhost:5070/health/ready` → 200; Swagger en `/swagger`; con el stack, la traza de esa
   petición en Grafana → Explore → Tempo (servicio `Camilo.OrderFlow.WebApi`).

No hay hooks de git: la verificación antes de commitear es la skill `verificar-cambio`. Detalle y problemas
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
  de capas, paquetes prohibidos, aislamiento, execution context por módulo). Rompe `dotnet test`.
- **Contrato público**: no hay `*.Contracts` ni `internal`; el host usa los requests de Application y la autorización
  de Infrastructure. Lo que otro módulo pueda usar se expone como fachada (ADR-0006).
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
- **Solo en código nuevo** (lo existente no se migra): constructores primarios
  ([ADR-0020](docs/adr/0020-constructores-primarios-en-codigo-nuevo.md)), `TimeProvider` en vez de `DateTime.UtcNow`
  ([ADR-0017](docs/adr/0017-timeprovider-en-codigo-nuevo.md)), logs con `[LoggerMessage]`
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
| Build como el CI | `dotnet build Camilo.OrderFlow.slnx -c Release -p:TreatWarningsAsErrors=true` |

xUnit v3 sobre Microsoft.Testing.Platform: los argumentos del runner van después de `--`. En Git Bash, `-p:` y no
`/p:`. Sin umbral de cobertura (se reporta). Convenciones: `.claude/rules/pruebas.md` y
[ADR-0016](docs/adr/0016-estrategia-de-pruebas.md).

## 6. Commits

- Conventional Commits en **inglés**: `type(scope): subject`; el cuerpo explica el porqué. Ramas
  `<tipo>/<tema-corto>` desde `develop`.
- **Sin atribución de IA** (ni `Co-Authored-By`, ni `Generated with`): `.claude/settings.json` la desactiva.
- Mensaje en archivo UTF-8 sin BOM y `git commit -F`; nunca `--no-verify`.
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
- [ADR-0010](docs/adr/0010-identidad-propia-bcrypt-jwt.md) Identidad propia, BCrypt, JWT con `ClockSkew = 0`.
- [ADR-0011](docs/adr/0011-migraciones-fuera-del-arranque.md) Migraciones fuera del arranque.
- [ADR-0012](docs/adr/0012-versiones-centralizadas-y-pines-transitivos.md) CPM; pines transitivos solo por seguridad; SDK sin fijar.
- [ADR-0013](docs/adr/0013-configuracion-sin-secretos-y-opciones-validadas.md) Sin secretos en el repo; opciones validadas al arrancar.
- [ADR-0014](docs/adr/0014-estilo-editorconfig-y-cero-avisos-en-ci.md) Estilo en `suggestion`; cero avisos en CI.
- [ADR-0015](docs/adr/0015-telemetria-otel-y-serilog-loki.md) OTel/OTLP para trazas y métricas; Serilog → Loki para logs.
- [ADR-0016](docs/adr/0016-estrategia-de-pruebas.md) xUnit v3 + MTP, Shouldly, NSubstitute, SQLite y Testcontainers.
- [ADR-0017](docs/adr/0017-timeprovider-en-codigo-nuevo.md) `TimeProvider` en código nuevo; `IDateTimeProvider` obsoleto.
- [ADR-0018](docs/adr/0018-loggermessage-para-logs-nuevos.md) Logs nuevos con `[LoggerMessage]`.
- [ADR-0019](docs/adr/0019-endpoints-nuevos-minimal-apis.md) Endpoints nuevos con Minimal APIs; los controladores se quedan.
- [ADR-0020](docs/adr/0020-constructores-primarios-en-codigo-nuevo.md) Constructores primarios en código nuevo.

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
