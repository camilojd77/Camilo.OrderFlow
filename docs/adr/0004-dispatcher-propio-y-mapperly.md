# ADR-0004 — Dispatcher CQRS propio y Mapperly, sin librerías de licencia comercial

- **Estado:** Aceptada
- **Fecha:** 2026-11-05 · **Decide:** Camilo Jaramillo
- **Revisar cuando:** el dispatcher necesite capacidades que no tiene (notificaciones, streaming) o cambie la licencia
  de Mapperly, FluentValidation, Shouldly o NSubstitute.

## Contexto

La plantilla usaba MediatR, AutoMapper y FluentAssertions, que pasaron a licencia comercial en versiones recientes. El
plan de construcción (D1-D3, hoy en `docs/archive/05-Plan-Definitivo.md`) los sustituyó. Alternativas consideradas:
fijar versiones antiguas con licencia libre (sin parches futuros), pagar licencias, o llamar a los handlers
directamente sin pipeline (sin punto único para la validación).

## Decisión

- **CQRS con dispatcher propio**: `ISender`, `IRequest<T>`, `IRequestHandler<,>` e `IPipelineBehavior<,>` en
  `BuildingBlocks.SharedKernel/Messaging`; implementación `Sender` en `BuildingBlocks.Infrastructure`. Los handlers se
  registran por reflexión en `Add<Modulo>Application()`.
- **Mapeo con Mapperly** (generador de código) o a mano; nunca AutoMapper.
- **Aserciones con Shouldly**; nunca FluentAssertions.
- Antes de añadir cualquier librería se revisa su licencia (skill `actualizar-dependencias`).

## Consecuencias

- Sin dependencias de licencia comercial; el dispatcher son ~60 líneas que el equipo mantiene.
- MediatR y AutoMapper están **prohibidos por test** en Domain y Application.
- El dispatcher no tiene notificaciones ni publicación de eventos; si hacen falta, ADR nuevo.

## Referencias

`Camilo.OrderFlow.BuildingBlocks.SharedKernel/Messaging/*`, `Camilo.OrderFlow.BuildingBlocks.Infrastructure/Messaging/Sender.cs`,
`Camilo.OrderFlow.Modules.Identity.Application/DependencyInjection/ServiceCollectionExtensions.cs`,
`Camilo.OrderFlow.Modules.Identity.Application/Mapping/*`, `CleanArchitectureTests.cs` (listas de dependencias prohibidas).
