---
name: dotnet-referencia
description: "Cómo consultar la documentación oficial de .NET/ASP.NET Core/EF Core (Microsoft Learn por MCP) en Camilo.OrderFlow y qué excepciones del repo prevalecen sobre ella. Úsala antes de seguir una receta oficial, una plantilla de dotnet new o un ejemplo de internet."
---

# Referencia oficial de .NET, con las excepciones del repo por delante

## Cómo consultar

- Servidor MCP `microsoft-learn` (`.mcp.json`): busca en la documentación oficial (.NET 10, ASP.NET Core 10, EF Core
  10, OpenTelemetry .NET) y cita la página.
- Comprueba que la página es de la versión que usa el repo (`net10.0`, C# 14, EF Core 10).

## Excepciones del repo (prevalecen)

| Id | La documentación o la plantilla dice | En este repo | Fuente |
|---|---|---|---|
| E-1 | Crear proyectos con `dotnet new` | Los módulos se crean con la skill `nuevo-modulo` (tres proyectos, referencias y registros concretos) | [ADR-0002](../../../docs/adr/0002-monolito-modular-clean-architecture.md) |
| E-2 | `dotnet add package` / versión en el `.csproj` | Versiones solo en `Directory.Packages.props`, con la skill `actualizar-dependencias` | [ADR-0024](../../../docs/adr/0024-versiones-fijadas-sdk-imagenes-y-acciones.md) |
| E-3 | MediatR para CQRS, AutoMapper para mapeo, FluentAssertions | Dispatcher propio (`ISender`), Mapperly, Shouldly | [ADR-0004](../../../docs/adr/0004-dispatcher-propio-y-mapperly.md) |
| E-4 | ASP.NET Core Identity | Módulo Identity propio, BCrypt, JWT | [ADR-0010](../../../docs/adr/0010-identidad-propia-bcrypt-jwt.md) |
| E-5 | `[Authorize(Roles = …)]` | Permisos: `[AuthorizePermission(IdentityPermissions.X)]` o política | [ADR-0009](../../../docs/adr/0009-autorizacion-por-permisos-con-hybridcache.md) |
| E-6 | `Database.Migrate()` al arrancar | Migraciones fuera del arranque (CLI o contenedor `migrator`) | [ADR-0011](../../../docs/adr/0011-migraciones-fuera-del-arranque.md) |
| E-7 | Validación integrada de .NET 10 (`AddValidation()`, data annotations en requests) | FluentValidation + `ValidationPipelineBehavior` | [ADR-0007](../../../docs/adr/0007-errores-excepciones-dominio-problemdetails.md) |
| E-8 | Patrón Result / `Results.Problem` en cada endpoint | Excepciones de dominio + `GlobalExceptionHandler` → `ProblemDetails` | [ADR-0007](../../../docs/adr/0007-errores-excepciones-dominio-problemdetails.md) |
| E-9 | OpenAPI integrado (`AddOpenApi`/`MapOpenApi`) | Swashbuckle, un documento por versión | [ADR-0008](../../../docs/adr/0008-versionado-por-cabecera-y-swashbuckle.md) |
| E-10 | Versión en la URL (`/v1/…`) | Cabecera `api-version` | [ADR-0008](../../../docs/adr/0008-versionado-por-cabecera-y-swashbuckle.md) |
| E-11 | .NET Aspire (AppHost, ServiceDefaults) | No se usa: compose en `deploy/` y OpenTelemetry configurado a mano | [ADR-0015](../../../docs/adr/0015-telemetria-otel-y-serilog-loki.md) |
| E-12 | Logs por el exportador OTLP de OpenTelemetry | Serilog → Loki | [ADR-0015](../../../docs/adr/0015-telemetria-otel-y-serilog-loki.md) |
| E-13 | `DbContext` inyectado en el endpoint o el handler | Repositorios + `I<Modulo>ExecutionContext` | [ADR-0005](../../../docs/adr/0005-dbcontext-esquema-y-execution-context-por-modulo.md) |
| E-14 | EF Core InMemory para tests | SQLite en memoria y Testcontainers | [ADR-0026](../../../docs/adr/0026-umbral-de-cobertura-por-proyecto.md) |
| E-15 | Controladores (en ejemplos antiguos) | Endpoints nuevos con Minimal APIs; controladores existentes se quedan | [ADR-0019](../../../docs/adr/0019-endpoints-nuevos-minimal-apis.md) |

Si la referencia contradice una excepción, `CLAUDE.md` o un ADR, **manda el repo**. No cambies el repo para alinearlo
con la documentación; si crees que debería cambiar, propónlo con la skill `nuevo-adr`.
