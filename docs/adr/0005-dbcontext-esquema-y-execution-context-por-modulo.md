# ADR-0005 — Un DbContext, un esquema SQL y un execution context por módulo

- **Estado:** Aceptada
- **Fecha:** 2026-11-05 · **Decide:** Camilo Jaramillo
- **Revisar cuando:** un caso de uso necesite una transacción que abarque dos módulos, o se extraiga un módulo a su
  propia base de datos.

## Contexto

Los módulos comparten servidor SQL Server pero deben poder separarse. Un `DbContext` único haría trivial unir tablas
de módulos distintos y acoplaría sus migraciones. Además, `IExecutionContext` (el punto de commit) vive en el
SharedKernel: si dos módulos registraran esa misma interfaz, el último registro ganaría y los handlers del otro
módulo confirmarían contra el `DbContext` equivocado, sin error.

Alternativas: un `DbContext` compartido; `IUnitOfWork` genérico (descartado en el plan de construcción, D4); que los
repositorios llamen a `SaveChangesAsync` (también descartado: oculta el límite transaccional).

## Decisión

- Cada módulo tiene su `<Modulo>DbContext`, sus tablas en su esquema (`identity.*`) y su historial de migraciones en
  `<esquema>.__EFMigrationsHistory`.
- Los repositorios no confirman. El handler confirma al final con `I<Modulo>ExecutionContext.CommitAsync()`, interfaz
  propia del módulo que hereda de `IExecutionContext`. **Nunca** se registra ni se inyecta `IExecutionContext` a secas.
- Ninguna consulta une tablas de esquemas de módulos distintos.

## Consecuencias

- Límite transaccional explícito por caso de uso y por módulo.
- No hay transacciones entre módulos: si un flujo las necesita, se rediseña o se decide en un ADR nuevo.
- `ModuleRegistrationTests` comprueba el registro del execution context de cada módulo (registro manual por módulo).

## Referencias

`Camilo.OrderFlow.Modules.Identity.Infrastructure/Persistence/Context/IdentityDbContext.cs`,
`Persistence/IdentityExecutionContext.cs`, `Persistence/Configurations/*` (`ToTable(..., "identity")`),
`DependencyInjection/ServiceCollectionExtensions.cs`, `Application/Abstractions/IIdentityExecutionContext.cs`,
`tests/Camilo.OrderFlow.ArchitectureTests/ModuleRegistrationTests.cs`.
