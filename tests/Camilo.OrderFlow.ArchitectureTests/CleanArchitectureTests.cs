using NetArchTest.Rules;
using Shouldly;
using System.Reflection;
using Xunit;

namespace Camilo.OrderFlow.ArchitectureTests;

/// <summary>
/// Verifies that Clean Architecture dependency rules are respected.
///
/// Allowed dependency direction:
///   Domain  ←  Application  ←  Infrastructure  ←  WebApi
///
/// Nothing flows the other way.
///
/// Las reglas se aplican a TODOS los módulos descubiertos por <see cref="ModuleCatalog"/>,
/// no a una lista fija: al agregar un módulo nuevo queda cubierto automáticamente.
/// </summary>
public sealed class CleanArchitectureTests
{
    private static readonly Assembly WebApiAsm = typeof(Program).Assembly;

    public static IEnumerable<object[]> Modules() => ModuleCatalog.ModuleNames();

    // ── Sanity check del descubrimiento ───────────────────────────────────

    [Fact(DisplayName = "Se descubre al menos un módulo con sus tres capas")]
    public void Catalog_Should_Discover_Modules()
    {
        ModuleCatalog.Modules.ShouldNotBeEmpty(
            "No se descubrió ningún ensamblado Camilo.OrderFlow.Modules.*. " +
            "Sin módulos, el resto de reglas de arquitectura pasaría de forma vacía.");

        foreach (var (name, module) in ModuleCatalog.Modules)
        {
            module.Domain.ShouldNotBeNull($"El módulo '{name}' no expone capa Domain.");
            module.Application.ShouldNotBeNull($"El módulo '{name}' no expone capa Application.");
            module.Infrastructure.ShouldNotBeNull($"El módulo '{name}' no expone capa Infrastructure.");
        }
    }

    // ── Dependency direction tests ────────────────────────────────────────

    [Theory(DisplayName = "Domain must NOT depend on Infrastructure")]
    [MemberData(nameof(Modules))]
    public void Domain_ShouldNot_DependOn_Infrastructure(string moduleName)
    {
        var module = ModuleCatalog.Get(moduleName);
        AssertNoDependency(module.Domain!, module.InfrastructureAssemblyName,
            $"Domain es la capa más interna del módulo '{moduleName}' y no puede referenciar su Infrastructure.");
    }

    [Theory(DisplayName = "Domain must NOT depend on WebApi")]
    [MemberData(nameof(Modules))]
    public void Domain_ShouldNot_DependOn_WebApi(string moduleName)
    {
        var module = ModuleCatalog.Get(moduleName);
        AssertNoDependency(module.Domain!, ModuleCatalog.WebApiAssembly,
            $"Domain del módulo '{moduleName}' no puede referenciar el host.");
    }

    [Theory(DisplayName = "Application must NOT depend on Infrastructure")]
    [MemberData(nameof(Modules))]
    public void Application_ShouldNot_DependOn_Infrastructure(string moduleName)
    {
        var module = ModuleCatalog.Get(moduleName);
        AssertNoDependency(module.Application!, module.InfrastructureAssemblyName,
            $"Application del módulo '{moduleName}' define casos de uso y no puede referenciar su Infrastructure.");
    }

    [Theory(DisplayName = "Application must NOT depend on WebApi")]
    [MemberData(nameof(Modules))]
    public void Application_ShouldNot_DependOn_WebApi(string moduleName)
    {
        var module = ModuleCatalog.Get(moduleName);
        AssertNoDependency(module.Application!, ModuleCatalog.WebApiAssembly,
            $"Application del módulo '{moduleName}' no puede referenciar el host.");
    }

    [Theory(DisplayName = "Infrastructure must NOT depend on WebApi")]
    [MemberData(nameof(Modules))]
    public void Infrastructure_ShouldNot_DependOn_WebApi(string moduleName)
    {
        var module = ModuleCatalog.Get(moduleName);
        AssertNoDependency(module.Infrastructure!, ModuleCatalog.WebApiAssembly,
            $"Infrastructure del módulo '{moduleName}' no puede referenciar el host.");
    }

    // ── Defensive rules: Domain purity ──────────────────────────────────
    // Domain debe permanecer libre de infraestructura: EF, ASP.NET, los source
    // generators de mapping/validación que usa Application, el dispatcher, y las
    // librerías comerciales que la plantilla descartó (AutoMapper, MediatR).
    // Estas reglas protegen la extracción futura del módulo a microservicio.

    public static IEnumerable<object[]> DomainBannedDependencies() =>
        from module in ModuleCatalog.Modules.Keys.OrderBy(n => n, StringComparer.Ordinal)
        from banned in new (string Package, string Why)[]
        {
            ("AutoMapper", "licencia comercial; la plantilla usa Mapperly"),
            ("MediatR", "licencia comercial; la plantilla usa su propio dispatcher"),
            ("Microsoft.EntityFrameworkCore", "la persistencia es un detalle de Infrastructure"),
            ("Microsoft.AspNetCore", "HTTP es un detalle del host"),
            ("Riok.Mapperly", "el mapeo es responsabilidad de Application"),
            ("FluentValidation", "la validación de entrada pertenece a Application"),
            ("Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging",
                "commands y queries viven en Application, no en Domain"),
        }
        select new object[] { module, banned.Package, banned.Why };

    [Theory(DisplayName = "Domain must stay free of infrastructure concerns")]
    [MemberData(nameof(DomainBannedDependencies))]
    public void Domain_ShouldNot_Reference_BannedDependency(string moduleName, string banned, string why)
    {
        var module = ModuleCatalog.Get(moduleName);
        AssertNoDependency(module.Domain!, banned,
            $"Domain del módulo '{moduleName}' no puede referenciar {banned}: {why}.");
    }

    // ── Defensive rules: Application purity ─────────────────────────────

    public static IEnumerable<object[]> ApplicationBannedDependencies() =>
        from module in ModuleCatalog.Modules.Keys.OrderBy(n => n, StringComparer.Ordinal)
        from banned in new (string Package, string Why)[]
        {
            ("AutoMapper", "licencia comercial; la plantilla usa Mapperly"),
            ("MediatR", "licencia comercial; la plantilla usa su propio dispatcher"),
            ("Microsoft.EntityFrameworkCore", "las queries de EF viven en Infrastructure"),
            ("Microsoft.AspNetCore", "HTTP es un detalle del host"),
        }
        select new object[] { module, banned.Package, banned.Why };

    [Theory(DisplayName = "Application must stay free of infrastructure concerns")]
    [MemberData(nameof(ApplicationBannedDependencies))]
    public void Application_ShouldNot_Reference_BannedDependency(string moduleName, string banned, string why)
    {
        var module = ModuleCatalog.Get(moduleName);
        AssertNoDependency(module.Application!, banned,
            $"Application del módulo '{moduleName}' no puede referenciar {banned}: {why}.");
    }

    // ── Module isolation ────────────────────────────────────────────────
    // Each module is self-contained: its code must NOT depend on another module's
    // namespace. Modules integrate through a port defined in the consumer's
    // Application layer, implemented by an adapter in the host that calls the
    // provider's public facade (docs/adr/0006-integracion-modulos-adaptador-en-host.md).
    //
    // Con un solo módulo el test pasa de forma vacía; en cuanto se agregue un segundo
    // (p. ej. Catalog), la regla se aplica automáticamente.

    [Fact(DisplayName = "Modules must not reference other modules directly")]
    public void Modules_ShouldNot_Reference_OtherModules()
    {
        var moduleNames = ModuleCatalog.Modules.Keys.ToList();
        moduleNames.ShouldNotBeEmpty("expected at least one module assembly to be discovered");

        foreach (var (ownName, module) in ModuleCatalog.Modules)
        {
            var assemblies = new[] { module.Domain, module.Application, module.Infrastructure }
                .Where(a => a is not null)
                .Select(a => a!);

            var otherNamespaces = moduleNames
                .Where(m => !string.Equals(m, ownName, StringComparison.Ordinal))
                .Select(m => $"Camilo.OrderFlow.Modules.{m}");

            foreach (var assembly in assemblies)
            {
                foreach (var otherNs in otherNamespaces)
                {
                    AssertNoDependency(assembly, otherNs,
                        $"{assembly.GetName().Name} no puede depender de {otherNs}. " +
                        "Define un puerto en tu capa Application e impleméntalo con un adaptador en el host (ADR-0006).");
                }
            }
        }
    }

    // ── Helper ──────────────────────────────────────────────────────────

    private static void AssertNoDependency(Assembly assembly, string dependency, string because)
    {
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOn(dependency)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"{because} Tipos infractores: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
