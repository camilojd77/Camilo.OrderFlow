---
name: nueva-funcionalidad
description: "Implementa un caso de uso (command o query) en un módulo de Camilo.OrderFlow siguiendo la cadena del repo: dominio, request, validator, handler, repositorio, endpoint, tests y telemetría. Úsala al añadir o cambiar comportamiento de negocio."
argument-hint: "<Modulo> <CasoDeUso>"
---

# Nueva funcionalidad (caso de uso)

**Antes de cada paso, lee el archivo de referencia real y escribe el tuyo a partir de él; no copies de memoria.** El
código nuevo usa constructores primarios ([ADR-0020](../../../docs/adr/0020-constructores-primarios-en-codigo-nuevo.md)),
`TimeProvider` ([ADR-0025](../../../docs/adr/0025-timeprovider-en-todo-el-codigo.md)) y `[LoggerMessage]`
([ADR-0018](../../../docs/adr/0018-loggermessage-para-logs-nuevos.md)); las referencias son anteriores y usan
constructores clásicos: copia la estructura, no ese estilo.

1. **Dominio** (si cambia el modelo): entidad o value object con `private set`, métodos de negocio y validación de
   invariantes. Ref.: `Camilo.OrderFlow.Modules.Identity.Domain/Entities/User.cs`.
2. **Request**: `sealed record XCommand(...) : IRequest<TDto>` en `Application/Commands/<Agregado>/<CasoDeUso>/` (o
   `Queries/…`). Ref.: `Application/Commands/Users/CreateUser/CreateUserCommand.cs`; paginada:
   `Application/Queries/Users/ListUsers/`.
3. **Validator**: `XCommandValidator : AbstractValidator<XCommand>` en la misma carpeta. Ref.:
   `CreateUserCommandValidator.cs`.
4. **Puertos**: si hace falta un método de repositorio nuevo, añádelo a la interfaz de `Application/Abstractions/`.
   Ref.: `Application/Abstractions/IUserRepository.cs`.
5. **Handler**: `XCommandHandler : IRequestHandler<XCommand, TDto>`; repositorios, entidades,
   `I<Modulo>ExecutionContext.CommitAsync(ct)` al final, DTO con el mapper. Errores con excepciones de SharedKernel
   (mensajes sin datos personales). Ref.: `CreateUserCommandHandler.cs`.
6. **DTO y mapper**: `Application/Dtos/` y `Application/Mapping/` (Mapperly). Ref.: `Dtos/UserDto.cs`,
   `Mapping/UserMapper.cs`.
7. **Infraestructura**: implementa el método en el repositorio (`AsNoTracking` en lecturas) y, si cambia el esquema,
   configuración EF + migración con la skill `migracion-ef`. Ref.:
   `Camilo.OrderFlow.Modules.Identity.Infrastructure/Persistence/Repositories/UserRepository.cs`.
8. **Endpoint**: con la skill `nuevo-endpoint` (Minimal API). Si solo amplías un controlador existente, sigue
   `Camilo.OrderFlow.WebApi/Controllers/UsersController.cs`.
9. **Permiso** (si el endpoint necesita uno nuevo): skill `nuevo-permiso`.
10. **Tests**:
    - Unitario del handler (caso feliz + cada error, verifica `CommitAsync`). Ref.:
      `tests/Camilo.OrderFlow.Identity.UnitTests/Application/Users/CreateUserCommandHandlerTests.cs`.
    - Dominio si cambió. Ref.: `tests/Camilo.OrderFlow.Identity.UnitTests/Domain/UserTests.cs`.
    - Integración del endpoint (feliz + 401/403/404/400 según aplique). Ref.:
      `tests/Camilo.OrderFlow.WebApi.IntegrationTests/UsersTests.cs`.
11. **Telemetría**: di qué señal observa el flujo; si hace falta una propia, skill `instrumentar`.
12. **Verificación**: skill `verificar-cambio`. Si la funcionalidad cambia algo de `docs/architecture.md` o de un ADR,
    actualízalo en el mismo cambio.
