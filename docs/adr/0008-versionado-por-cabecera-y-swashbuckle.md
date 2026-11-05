# ADR-0008 — Versionar la API por cabecera `api-version` y documentarla con Swashbuckle

- **Estado:** Aceptada
- **Fecha:** 2026-11-05 · **Decide:** Camilo Jaramillo
- **Revisar cuando:** se pase al OpenAPI integrado de .NET (`Microsoft.AspNetCore.OpenApi`) o un consumidor exija la
  versión en la URL.

## Contexto

La API necesita evolucionar sin romper clientes y publicar un contrato navegable. Alternativas de versionado: segmento
en la URL (`/api/v1/...`), query string o tipo de medio. Alternativas de documentación: el generador OpenAPI integrado
de .NET 9/10 (el paquete `Microsoft.AspNetCore.OpenApi` está referenciado pero el documento lo genera Swashbuckle) o
NSwag.

## Decisión

- **Asp.Versioning** con lector de cabecera `api-version`; versión por defecto 1.0 asumida si falta; se informan las
  versiones soportadas. Grupos de API `'v'VVV`.
- **Swashbuckle** genera un documento por versión, con esquema de seguridad JWT. Swagger UI solo en Development y
  Staging.
- Un cambio incompatible de contrato se publica como versión nueva (`[ApiVersion("2.0")]` o su equivalente en
  Minimal APIs); no se rompe la 1.0 sin acordarlo.

## Consecuencias

- Las rutas no cambian entre versiones; los clientes eligen versión por cabecera.
- Los endpoints Minimal API ([ADR-0019](0019-endpoints-nuevos-minimal-apis.md)) necesitan `Asp.Versioning.Http`, que
  aún no está en `Directory.Packages.props`.
- Migrar al OpenAPI integrado es una decisión futura (ADR nuevo).

## Referencias

`Camilo.OrderFlow.WebApi/Versioning/ApiVersioningExtensions.cs`, `Camilo.OrderFlow.WebApi/Swagger/*`,
`Camilo.OrderFlow.WebApi/Program.cs`, `Directory.Packages.props` (grupo WebApi).
