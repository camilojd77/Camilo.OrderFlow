---
name: nuevo-modulo
description: "Crea un módulo de negocio nuevo en Camilo.OrderFlow: tres proyectos, referencias permitidas, DbContext y esquema propios, execution context, registro en el host, tests de arquitectura, primera migración y documentación."
argument-hint: "<Modulo>"
disable-model-invocation: true
---

# Nuevo módulo

Módulo = dominio de negocio con frontera propia ([ADR-0002](../../../docs/adr/0002-monolito-modular-clean-architecture.md)).
Toma Identity como referencia de estructura en cada paso; el código nuevo usa constructores primarios, `TimeProvider` y
`[LoggerMessage]` (ADR-0025/0018/0020). Pide confirmación antes de crear proyectos.

1. **Proyectos** `Camilo.OrderFlow.Modules.<M>.Domain`, `.Application`, `.Infrastructure` en la raíz del repo, con los
   `.csproj` copiados en forma de los de Identity (sin `Version=`). Referencias: Domain → SharedKernel; Application →
   Domain + SharedKernel; Infrastructure → Application + Domain + SharedKernel + BuildingBlocks.Infrastructure.
   **Ninguna referencia a otro módulo.**
2. **Solución**: añádelos a `Camilo.OrderFlow.slnx` en `/src/Modules/` (`dotnet sln Camilo.OrderFlow.slnx add …`).
3. **Persistencia**: `<M>DbContext` en `Infrastructure/Persistence/Context`, tablas en el esquema `<m>` con
   `ToTable(..., "<m>")`, historial en `<m>.__EFMigrationsHistory`. Ref.:
   `Camilo.OrderFlow.Modules.Identity.Infrastructure/Persistence/Context/IdentityDbContext.cs` y
   `DependencyInjection/ServiceCollectionExtensions.cs` (`UseSqlServer` + `MigrationsHistoryTable`).
4. **Commit**: `I<M>ExecutionContext : IExecutionContext` en `Application/Abstractions` + implementación en
   `Infrastructure/Persistence`, registrada como `I<M>ExecutionContext` (scoped). Nunca `IExecutionContext`. Ref.:
   `IIdentityExecutionContext.cs`, `IdentityExecutionContext.cs`.
5. **Superficie** ([ADR-0028](../../../docs/adr/0028-superficie-publica-de-los-modulos.md)): `internal` por defecto; `InternalsVisibleTo` en los `.csproj`
   solo hacia los proyectos de tests del módulo. `ModuleSurfaceTests` lo comprueba sin registrar nada.
   **DI**: `Add<M>Application()` (handlers por reflexión, validators con `includeInternalTypes: true` y mappers; el
   behavior de validación ya lo registra `AddBuildingBlocks`, no lo registres otra vez) y
   `Add<M>Infrastructure(IConfiguration)`. Ref.: los `ServiceCollectionExtensions.cs` de Identity. Ojo: si cada módulo
   registra `IPipelineBehavior<,>` genérico, el behavior se ejecutará varias veces; pregunta cómo centralizarlo antes
   de copiarlo.
6. **Host**: dos líneas en `Camilo.OrderFlow.WebApi/Program.cs` y la referencia de proyecto a `<M>.Infrastructure`
   (y `<M>.Application` si el host usa sus requests).
7. **Tests de arquitectura**: `ModuleCatalog` lo descubre solo si el host lo referencia; añade a mano en
   `tests/Camilo.OrderFlow.ArchitectureTests/ModuleRegistrationTests.cs` su `Add<M>Infrastructure` y la comprobación de
   `I<M>ExecutionContext`. Crea su proyecto de tests unitarios en `/tests/Unit/` si hace falta.
8. **Primera migración** con la skill `migracion-ef` (`Initial<M>`) y añádela al target `migrator` del `Dockerfile` si
   hace falta (hoy solo migra Identity: avisa, es un cambio de despliegue).
9. **Integración con otros módulos**: puerto en el consumidor + adaptador en el host
   ([ADR-0006](../../../docs/adr/0006-integracion-modulos-adaptador-en-host.md)).
10. **Telemetría**: `ActivitySource`/`Meter` `Camilo.OrderFlow.<M>` cuando haga falta (skill `instrumentar`).
11. **Documentación**: tabla de proyectos y módulos en `docs/architecture.md`, `CLAUDE.md` §2, `README.md` si cambia el
    arranque, y ADR si se decidió algo con alternativas.
12. **Verificación**: skill `verificar-cambio` completa.
