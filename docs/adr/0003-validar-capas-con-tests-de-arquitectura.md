# ADR-0003 — Validar las capas con referencias de proyecto y tests de arquitectura

- **Estado:** Aceptada
- **Fecha:** 2026-11-05 · **Decide:** Camilo Jaramillo
- **Revisar cuando:** se cree un proyecto `*.Contracts` por módulo o se quiera restringir la superficie pública con
  `internal`.

## Contexto

Las reglas de [ADR-0002](0002-monolito-modular-clean-architecture.md) solo valen si algo las comprueba. Las
referencias entre `.csproj` impiden algunas dependencias, pero no que Domain use EF Core a través de un paquete, ni que
un módulo dependa de otro por un paquete compartido.

Alternativas: confiar en la revisión de código; analizadores Roslyn propios; ArchUnitNET; un proyecto `Contracts` por
módulo con todo lo demás `internal` (hoy Application expone todos sus tipos como `public` y el host los consume).

## Decisión

Las reglas se validan en dos niveles:

1. **Referencias de proyecto**: cada capa solo referencia lo permitido (ver tabla en `docs/architecture.md`).
2. **Tests de arquitectura con NetArchTest** (`tests/Camilo.OrderFlow.ArchitectureTests`): dirección de dependencias
   por módulo, Domain y Application sin EF Core, ASP.NET Core, MediatR ni AutoMapper (Domain además sin Mapperly,
   FluentValidation ni la mensajería), aislamiento entre módulos, y registro de `I<Modulo>ExecutionContext`.

Los módulos se descubren por convención de nombre de ensamblado (`ModuleCatalog`); el registro de DI de cada módulo
nuevo se añade a mano en `ModuleRegistrationTests`.

## Consecuencias

- Una violación rompe `dotnet test` (no `dotnet build`), en local y en CI.
- La superficie pública de un módulo no está restringida: hoy todo Application es `public`. Restringirla es una
  propuesta abierta, no una regla.
- No se modifican ni se relajan los tests de arquitectura para que pase un cambio; si una regla debe cambiar, se hace
  un ADR nuevo.

## Referencias

`tests/Camilo.OrderFlow.ArchitectureTests/CleanArchitectureTests.cs`, `ModuleCatalog.cs`,
`ModuleRegistrationTests.cs`, `Directory.Packages.props` (`NetArchTest.Rules`).
