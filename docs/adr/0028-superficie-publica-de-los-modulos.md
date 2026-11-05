# ADR-0028 — Superficie pública mínima por módulo; fachada `Contracts` con el primer consumidor

- **Estado:** Aceptada
- **Fecha:** 2026-11-05 · **Decide:** Camilo Jaramillo
- **Revisar cuando:** un segundo módulo necesite algo de otro (se crea su `Contracts`), o un módulo exponga sus
  endpoints él mismo en lugar del host.

## Contexto

Identity era público entero: 67 tipos en Application y 21 en Infrastructure, aunque el host solo usa una parte. Con la
superficie abierta, nada impide que el host u otro módulo dependan de un handler, de un repositorio o del
`DbContext`. Eso acopla y dificulta extraer el módulo ([ADR-0002](0002-monolito-modular-clean-architecture.md),
[ADR-0006](0006-integracion-modulos-adaptador-en-host.md)).

Alternativas consideradas:
- crear ya un proyecto `Identity.Contracts` con una fachada (no hay consumidor: sería una interfaz especulativa,
  contraria a la segregación de interfaces y a YAGNI);
- mover los endpoints de Identity dentro del módulo para poder hacer `internal` también los requests (refactor grande,
  y [ADR-0019](0019-endpoints-nuevos-minimal-apis.md) mantiene los controladores existentes);
- solo documentarlo (sin nada que lo haga cumplir).

## Decisión

**Por defecto, `internal`.** Cada módulo expone solo:

| Capa | Público | `internal` |
|---|---|---|
| Domain | Entidades y value objects (los usa Application) | — |
| Application | Requests (commands/queries), DTOs, puertos de `Abstractions` (los implementa Infrastructure o el host), `Add<Modulo>Application` | Handlers, validators, pipeline behaviors, mappers |
| Infrastructure | `Add<Modulo>Infrastructure` y extensiones de arranque; lo que el host necesita para componer (en Identity: `AuthorizePermission`, `IdentityPermissions`, `IAuthService` y sus DTOs, `JwtOptions`) | `DbContext`, configuraciones EF, repositorios, implementaciones de puertos, servicios, opciones internas, seeders |

- Los tests del módulo ven lo interno con `InternalsVisibleTo`, declarado en el `.csproj` y **solo** para sus
  proyectos de tests. Nunca para el host ni para otro módulo.
- Los validators internos se registran con `AddValidatorsFromAssembly(assembly, includeInternalTypes: true)`.
- `ModuleSurfaceTests` lo comprueba en todos los módulos: falla si una handler, un validator, un behavior o un mapper
  es público, o si lo son el `DbContext` o una implementación de un puerto.
- **Fachada para otros módulos:** cuando el primer módulo necesite algo de otro, el proveedor crea
  `Camilo.OrderFlow.Modules.<M>.Contracts`. Ese proyecto lleva interfaces pequeñas orientadas al caso de uso del
  consumidor y DTOs propios, sin entidades ni tipos de EF. Lo implementa la Infrastructure del proveedor y solo lo
  referencia el host, cuyo adaptador implementa el puerto del consumidor (ADR-0006). No se crea antes de tener ese
  consumidor.

## Consecuencias

- El compilador y `ModuleSurfaceTests` impiden depender de detalles internos; el host solo ve lo que necesita.
- Los tipos internos se pueden cambiar sin romper a nadie fuera del módulo.
- Un tipo nuevo de Application o Infrastructure nace `internal` salvo que esté en la columna «Público».
- Mientras los endpoints de Identity vivan en el host, sus requests y DTOs siguen siendo públicos.

## Referencias

`Camilo.OrderFlow.Modules.Identity.Application/*.csproj`, `Camilo.OrderFlow.Modules.Identity.Infrastructure/*.csproj`
(`InternalsVisibleTo`), `Identity.Application/DependencyInjection/ServiceCollectionExtensions.cs`,
`tests/Camilo.OrderFlow.ArchitectureTests/ModuleSurfaceTests.cs`, skills `nuevo-modulo` y `nueva-funcionalidad`.
