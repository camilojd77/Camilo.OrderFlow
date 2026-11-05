---
paths:
  - "Camilo.OrderFlow.Modules.*.Application/**"
---

# Capa Application de los módulos

- Un caso de uso = una carpeta `Commands|Queries/<Agregado>/<CasoDeUso>/` con `XCommand` (`sealed record … :
  IRequest<T>`), `XCommandValidator` (FluentValidation) y `XCommandHandler` (`IRequestHandler<,>`). Los handlers se
  registran solos por reflexión; los validators, con `AddValidatorsFromAssembly`.
- Puertos (repositorios, `IPasswordHasher`, `ICurrentUser`, `I<Modulo>ExecutionContext`, puertos hacia otros módulos)
  en `Abstractions/`. Sin EF Core, ASP.NET Core, MediatR ni AutoMapper (test de arquitectura).
- Los handlers usan repositorios, nunca el `DbContext`, y confirman al final con
  `I<Modulo>ExecutionContext.CommitAsync(ct)`. Nunca `IExecutionContext` a secas.
- Si cambian roles o permisos: invalidar la caché **después** del commit (`IPermissionService.InvalidateUserAsync` /
  `InvalidateRoleAsync`).
- Respuestas como DTO de `Dtos/`, mapeadas con Mapperly (`Mapping/`) o a mano; nunca entidades.
- `CancellationToken` en todo método asíncrono público, propagado hasta el repositorio.
- Errores con las excepciones de SharedKernel; no se capturan excepciones para silenciarlas.
- Mensajes de excepción **sin datos personales** (llegan al `Detail` del `ProblemDetails`).
- **Código nuevo:** constructores primarios ([ADR-0020](../../docs/adr/0020-constructores-primarios-en-codigo-nuevo.md)),
  `TimeProvider` para la hora ([ADR-0017](../../docs/adr/0017-timeprovider-en-codigo-nuevo.md)) y `[LoggerMessage]`
  si se registra algo ([ADR-0018](../../docs/adr/0018-loggermessage-para-logs-nuevos.md)). Lo existente no se migra.
- Sin `IConfiguration`: la configuración llega como `IOptions<T>` desde Infrastructure.

Referencia: `Camilo.OrderFlow.Modules.Identity.Application/Commands/Users/CreateUser/` (command, validator, handler),
`Queries/Users/ListUsers/`, `Behaviors/ValidationPipelineBehavior.cs`.

Fuente: [CLAUDE.md](../../CLAUDE.md) §3 · [docs/architecture.md](../../docs/architecture.md) §5 ·
[ADR-0004](../../docs/adr/0004-dispatcher-propio-y-mapperly.md) ·
[ADR-0005](../../docs/adr/0005-dbcontext-esquema-y-execution-context-por-modulo.md)
