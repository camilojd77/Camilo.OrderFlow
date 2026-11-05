# Decisiones de arquitectura (ADR)

Cada decisión vigente con alternativas reales tiene aquí su ADR ([ADR-0001](0001-registrar-decisiones-con-adr.md)).

## Proceso

1. Copia [`plantilla.md`](plantilla.md) como `NNNN-<decision-en-minusculas-con-guiones>.md` con el siguiente número
   libre (o usa la skill `nuevo-adr`).
2. Un ADR **Aceptada** no se edita. Si la decisión cambia, el ADR nuevo dice qué reemplaza y el antiguo cambia solo su
   estado a «Reemplazada por [ADR-NNNN](…)».
3. En el **mismo PR**: fila en esta tabla y línea en [`CLAUDE.md`](../../CLAUDE.md) §7.

## Índice

| # | Decisión | Estado | Fecha |
|---|---|---|---|
| [0001](0001-registrar-decisiones-con-adr.md) | Registrar las decisiones de arquitectura con ADR | Aceptada | 2026-11-05 |
| [0002](0002-monolito-modular-clean-architecture.md) | Monolito modular con Clean Architecture por módulo | Aceptada | 2026-11-05 |
| [0003](0003-validar-capas-con-tests-de-arquitectura.md) | Validar las capas con referencias de proyecto y tests de arquitectura | Aceptada | 2026-11-05 |
| [0004](0004-dispatcher-propio-y-mapperly.md) | Dispatcher CQRS propio y Mapperly, sin librerías de licencia comercial | Aceptada | 2026-11-05 |
| [0005](0005-dbcontext-esquema-y-execution-context-por-modulo.md) | Un DbContext, un esquema SQL y un execution context por módulo | Aceptada | 2026-11-05 |
| [0006](0006-integracion-modulos-adaptador-en-host.md) | Integrar módulos con un puerto en el consumidor y un adaptador en el host | Aceptada | 2026-11-05 |
| [0007](0007-errores-excepciones-dominio-problemdetails.md) | Errores como excepciones de dominio traducidas a ProblemDetails | Aceptada | 2026-11-05 |
| [0008](0008-versionado-por-cabecera-y-swashbuckle.md) | Versionar la API por cabecera `api-version` y documentarla con Swashbuckle | Aceptada | 2026-11-05 |
| [0009](0009-autorizacion-por-permisos-con-hybridcache.md) | Autorizar por permisos en base de datos con caché HybridCache | Aceptada | 2026-11-05 |
| [0010](0010-identidad-propia-bcrypt-jwt.md) | Identidad propia con BCrypt y JWT sin tolerancia de reloj | Aceptada | 2026-11-05 |
| [0011](0011-migraciones-fuera-del-arranque.md) | Aplicar migraciones fuera del arranque | Aceptada | 2026-11-05 |
| [0012](0012-versiones-centralizadas-y-pines-transitivos.md) | Versiones centralizadas (CPM) y pines transitivos solo por seguridad | Aceptada | 2026-11-05 |
| [0013](0013-configuracion-sin-secretos-y-opciones-validadas.md) | Configuración sin secretos en el repo y opciones validadas al arrancar | Aceptada | 2026-11-05 |
| [0014](0014-estilo-editorconfig-y-cero-avisos-en-ci.md) | Estilo por `.editorconfig` en `suggestion` y cero avisos en CI | Aceptada | 2026-11-05 |
| [0015](0015-telemetria-otel-y-serilog-loki.md) | Trazas y métricas por OpenTelemetry/OTLP; logs por Serilog a Loki | Aceptada | 2026-11-05 |
| [0016](0016-estrategia-de-pruebas.md) | Pruebas con xUnit v3 sobre MTP, Shouldly, NSubstitute, SQLite y Testcontainers | Aceptada | 2026-11-05 |
| [0017](0017-timeprovider-en-codigo-nuevo.md) | `TimeProvider` en código nuevo; `IDateTimeProvider` obsoleto | Aceptada | 2026-11-05 |
| [0018](0018-loggermessage-para-logs-nuevos.md) | Logs nuevos con `[LoggerMessage]` generado | Aceptada | 2026-11-05 |
| [0019](0019-endpoints-nuevos-minimal-apis.md) | Endpoints nuevos con Minimal APIs y `TypedResults` | Aceptada | 2026-11-05 |
| [0020](0020-constructores-primarios-en-codigo-nuevo.md) | Constructores primarios en código nuevo | Aceptada | 2026-11-05 |
