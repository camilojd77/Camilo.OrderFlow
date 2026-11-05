---
paths:
  - "Camilo.OrderFlow.Modules.*.Domain/**"
---

# Dominio de los módulos

- Solo puede referenciar `BuildingBlocks.SharedKernel` (excepciones, tipos base). Nada de EF Core, ASP.NET Core,
  FluentValidation, Mapperly, `SharedKernel.Messaging`, MediatR ni AutoMapper: lo comprueba
  `CleanArchitectureTests`.
- Entidades con propiedades `private set` y cambios de estado por métodos con nombre de negocio
  (`user.Deactivate()`, no `user.IsActive = false`).
- Constructor privado sin parámetros para EF + constructor público que crea una instancia válida. `default!` solo en
  propiedades que rellena EF.
- Las reglas que protegen la consistencia de la entidad viven en la entidad o en su value object, no en el handler.
- Errores de dominio: excepciones de `BuildingBlocks.SharedKernel.Exceptions` (`ConflictException`,
  `ValidationException`…); nunca códigos HTTP.
- La hora llega como parámetro (`DateTime utcNow`) desde el `TimeProvider` del handler; nunca `DateTime.UtcNow`.
  Lo que depende de la hora es un método (`IsLockedOut(utcNow)`), no una propiedad.
- Sin servicios inyectados ni `IConfiguration`.

Referencia: `Camilo.OrderFlow.Modules.Identity.Domain/Entities/User.cs`,
`Camilo.OrderFlow.Modules.Identity.Domain/ValueObjects/UserProfile.cs`.

Fuente: [CLAUDE.md](../../CLAUDE.md) §2 · [ADR-0002](../../docs/adr/0002-monolito-modular-clean-architecture.md) ·
[ADR-0025](../../docs/adr/0025-timeprovider-en-todo-el-codigo.md)
