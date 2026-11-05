---
paths:
  - "tests/**"
---

# Pruebas

- xUnit v3 sobre Microsoft.Testing.Platform; aserciones con **Shouldly**; dobles con **NSubstitute** solo para
  puertos de Application. Nada de FluentAssertions, Moq ni EF InMemory.
- Todo método que acepte `CancellationToken` recibe `TestContext.Current.CancellationToken` (analizador `xUnit1051`,
  avisos como errores en CI). `IAsyncLifetime` devuelve `ValueTask`.
- Nombres `Metodo_Escenario_Resultado` (`Handle_DuplicateEmail_ThrowsConflictException`); Arrange/Act/Assert.
- **Unitarios** (`Identity.UnitTests`): dominio y handlers con repositorios sustituidos; verifican el `CommitAsync`.
- **Integración** (`WebApi.IntegrationTests`): `WebApiFactory` con SQLite en memoria; un caso feliz + al menos un
  error por endpoint; datos únicos por test. No prueban reglas de negocio (eso es unitario).
- **RealDb**: `[Trait("Category", "RealDb")]` y Testcontainers; necesitan Docker. Un cambio de persistencia se
  verifica también aquí.
- **Cobertura**: mínimo por proyecto en `eng/coverage-thresholds.json`, comprobado por `eng/coverage-gate.cs` sobre la
  suite completa. No se bajan mínimos; un módulo nuevo añade el suyo. Código nuevo, con sus tests.
- **Arquitectura**: no se relajan ni se borran reglas para que pase un cambio. Al añadir un módulo, registra su
  `Add<Modulo>Infrastructure` y su execution context en `ModuleRegistrationTests`.
- Reloj en tests: `TestTime.UtcNow` y `TestTime.Provider` (`FixedTimeProvider`) de `Identity.UnitTests`, y valores
  exactos en las aserciones; nunca esperar ni comparar con `DateTime.UtcNow`. `SystemClockUsageTests` tiene la lista
  de excepciones vacía: no se añade una entrada sin ADR.
- Filtros: `dotnet test -- --filter-not-trait "Category=RealDb" --ignore-exit-code 8` (rápida). Un filtro que deja
  RealDb sin casos no valida SQL Server.

Referencia: `tests/Camilo.OrderFlow.Identity.UnitTests/Application/Users/CreateUserCommandHandlerTests.cs`,
`tests/Camilo.OrderFlow.WebApi.IntegrationTests/UsersTests.cs`,
`tests/Camilo.OrderFlow.WebApi.IntegrationTests.RealDb/IdentitySmokeTests.cs`,
`tests/Camilo.OrderFlow.ArchitectureTests/ModuleRegistrationTests.cs`.

Fuente: [ADR-0026](../../docs/adr/0026-umbral-de-cobertura-por-proyecto.md) · [ADR-0003](../../docs/adr/0003-validar-capas-con-tests-de-arquitectura.md) ·
[CLAUDE.md](../../CLAUDE.md) §5
