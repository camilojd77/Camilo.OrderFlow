# ADR-0002 — Monolito modular con Clean Architecture por módulo

- **Estado:** Aceptada
- **Fecha:** 2026-11-05 · **Decide:** Camilo Jaramillo
- **Revisar cuando:** un módulo tenga que desplegarse o escalarse por separado (ver la ruta a microservicios en
  `docs/architecture.md`).

## Contexto

La plantilla nació como backend único que debe poder crecer a varios dominios de negocio y, más adelante, separar
alguno como servicio. Ya estaba construida así: un host (`Camilo.OrderFlow.WebApi`), building blocks compartidos y un
módulo (`Identity`) partido en tres proyectos.

Alternativas: un único proyecto por capas (rápido de empezar, pero sin fronteras entre dominios), microservicios desde
el principio (coste operativo alto sin necesidad demostrada) o *vertical slices* sin capas (menos ceremonia, pero sin
la protección de dependencias que dan los proyectos separados).

## Decisión

- Un **módulo** por dominio de negocio, con tres proyectos: `Camilo.OrderFlow.Modules.<Modulo>.Domain`,
  `.Application` e `.Infrastructure`.
- Dependencias solo hacia dentro: `Domain ← Application ← Infrastructure ← WebApi`.
- Lo transversal y estable vive en `BuildingBlocks.SharedKernel` (abstracciones) y `BuildingBlocks.Infrastructure`
  (implementaciones). Un módulo no referencia a otro (ver [ADR-0006](0006-integracion-modulos-adaptador-en-host.md)).
- Un único host compone todos los módulos en `Program.cs` con `Add<Modulo>Application()` y
  `Add<Modulo>Infrastructure(IConfiguration)`.

## Consecuencias

- Fronteras claras entre dominios y dirección de dependencias verificable
  ([ADR-0003](0003-validar-capas-con-tests-de-arquitectura.md)).
- Más proyectos y más ceremonia por funcionalidad (command, validator, handler, repositorio, endpoint).
- Crear un módulo sigue la skill `nuevo-modulo`; no se crean proyectos con `dotnet new` a mano.

## Referencias

`Camilo.OrderFlow.slnx`, `Camilo.OrderFlow.WebApi/Program.cs`, `*.csproj` de cada capa,
`tests/Camilo.OrderFlow.ArchitectureTests/CleanArchitectureTests.cs`. Antecedente: `docs/archive/01-Arquitectura.md`.
