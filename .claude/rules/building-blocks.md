---
paths:
  - "Camilo.OrderFlow.BuildingBlocks.*/**"
---

# Building blocks (kernel compartido)

- Aquí solo va lo **transversal y estable** que usan varios módulos: excepciones de dominio, contratos CQRS,
  paginación, `IExecutionContext`. Nada que pertenezca a un módulo concreto.
- `SharedKernel` no referencia ningún otro proyecto ni paquetes de infraestructura; `BuildingBlocks.Infrastructure`
  solo referencia `SharedKernel`.
- Un cambio aquí afecta a todos los módulos: pide que se justifique y comprueba los tests de todos los proyectos.
- El dispatcher (`Sender`) es propio a propósito; no se sustituye por MediatR
  ([ADR-0004](../../docs/adr/0004-dispatcher-propio-y-mapperly.md)).
- `IDateTimeProvider` está obsoleto: no se usa en código nuevo ([ADR-0017](../../docs/adr/0017-timeprovider-en-codigo-nuevo.md)).
- Antes de añadir algo aquí, pregúntate si un módulo podría extraerse sin ello: si la respuesta es no, probablemente
  no es kernel.

Referencia: `Camilo.OrderFlow.BuildingBlocks.Infrastructure/Messaging/Sender.cs`,
`Camilo.OrderFlow.BuildingBlocks.SharedKernel/Exceptions/DomainException.cs`.

Fuente: [ADR-0002](../../docs/adr/0002-monolito-modular-clean-architecture.md) · [docs/architecture.md](../../docs/architecture.md) §2-3
