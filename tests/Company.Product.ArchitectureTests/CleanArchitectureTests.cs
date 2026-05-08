using NetArchTest.Rules;
using Shouldly;
using Xunit;

namespace Company.Product.ArchitectureTests;

/// <summary>
/// Verifies that Clean Architecture dependency rules are respected.
///
/// Allowed dependency direction:
///   Domain  ←  Application  ←  Infrastructure  ←  WebApi
///
/// Nothing flows the other way.
/// </summary>
public sealed class CleanArchitectureTests
{
    // ── Assembly name roots ───────────────────────────────────────────────

    private const string DomainAssembly = "Company.Product.Modules.Identity.Domain";
    private const string ApplicationAssembly = "Company.Product.Modules.Identity.Application";
    private const string InfrastructureAssembly = "Company.Product.Modules.Identity.Infrastructure";
    private const string WebApiAssembly = "Company.Product.WebApi";

    // Anchor types are used so NetArchTest can locate the assembly on disk.
    private static readonly System.Reflection.Assembly DomainAsm = typeof(Company.Product.Modules.Identity.Domain.Entities.User).Assembly;
    private static readonly System.Reflection.Assembly ApplicationAsm = typeof(Company.Product.Modules.Identity.Application.DependencyInjection.ServiceCollectionExtensions).Assembly;
    private static readonly System.Reflection.Assembly InfrastructureAsm = typeof(Company.Product.Modules.Identity.Infrastructure.DependencyInjection.ServiceCollectionExtensions).Assembly;
    private static readonly System.Reflection.Assembly WebApiAsm = typeof(Program).Assembly;

    // ── Dependency direction tests ────────────────────────────────────────

    [Fact(DisplayName = "Domain must NOT depend on Infrastructure")]
    public void Domain_ShouldNot_DependOn_Infrastructure()
    {
        var result = Types.InAssembly(DomainAsm)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureAssembly)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Domain is the innermost layer and must not reference {InfrastructureAssembly}. " +
            $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact(DisplayName = "Domain must NOT depend on WebApi")]
    public void Domain_ShouldNot_DependOn_WebApi()
    {
        var result = Types.InAssembly(DomainAsm)
            .ShouldNot()
            .HaveDependencyOn(WebApiAssembly)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Domain must not reference {WebApiAssembly}. " +
            $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact(DisplayName = "Application must NOT depend on Infrastructure")]
    public void Application_ShouldNot_DependOn_Infrastructure()
    {
        var result = Types.InAssembly(ApplicationAsm)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureAssembly)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Application defines use-cases and must not reference {InfrastructureAssembly}. " +
            $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact(DisplayName = "Application must NOT depend on WebApi")]
    public void Application_ShouldNot_DependOn_WebApi()
    {
        var result = Types.InAssembly(ApplicationAsm)
            .ShouldNot()
            .HaveDependencyOn(WebApiAssembly)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Application must not reference {WebApiAssembly}. " +
            $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact(DisplayName = "Infrastructure must NOT depend on WebApi")]
    public void Infrastructure_ShouldNot_DependOn_WebApi()
    {
        var result = Types.InAssembly(InfrastructureAsm)
            .ShouldNot()
            .HaveDependencyOn(WebApiAssembly)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Infrastructure must not reference {WebApiAssembly}. " +
            $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // ── Defensive rules: banned packages ────────────────────────────────
    // These prevent re-introducing commercial or architecturally wrong dependencies.

    [Fact(DisplayName = "Domain must NOT reference AutoMapper")]
    public void Domain_ShouldNot_Reference_AutoMapper()
    {
        var result = Types.InAssembly(DomainAsm)
            .ShouldNot().HaveDependencyOn("AutoMapper")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue("Domain must not reference AutoMapper (commercial license).");
    }

    [Fact(DisplayName = "Domain must NOT reference MediatR")]
    public void Domain_ShouldNot_Reference_MediatR()
    {
        var result = Types.InAssembly(DomainAsm)
            .ShouldNot().HaveDependencyOn("MediatR")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue("Domain must not reference MediatR (commercial license).");
    }

    [Fact(DisplayName = "Domain must NOT reference EntityFrameworkCore")]
    public void Domain_ShouldNot_Reference_EntityFrameworkCore()
    {
        var result = Types.InAssembly(DomainAsm)
            .ShouldNot().HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Domain must not reference EF Core. Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact(DisplayName = "Application must NOT reference AutoMapper")]
    public void Application_ShouldNot_Reference_AutoMapper()
    {
        var result = Types.InAssembly(ApplicationAsm)
            .ShouldNot().HaveDependencyOn("AutoMapper")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue("Application must not reference AutoMapper (commercial license).");
    }

    [Fact(DisplayName = "Application must NOT reference MediatR")]
    public void Application_ShouldNot_Reference_MediatR()
    {
        var result = Types.InAssembly(ApplicationAsm)
            .ShouldNot().HaveDependencyOn("MediatR")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue("Application must not reference MediatR (commercial license).");
    }

    [Fact(DisplayName = "Application must NOT reference EntityFrameworkCore")]
    public void Application_ShouldNot_Reference_EntityFrameworkCore()
    {
        var result = Types.InAssembly(ApplicationAsm)
            .ShouldNot().HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Application must not reference EF Core. Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // ── Defensive rules: Domain purity (Phase 9.3) ──────────────────────
    // Domain must remain free of infrastructure concerns: ASP.NET, EF, the
    // mapping/validation source generators we use in Application, and the
    // dispatcher (Messaging) abstractions. These rules protect Domain from
    // accidental coupling that would break the modular monolith → microservices
    // extraction story.

    [Fact(DisplayName = "Domain must NOT reference ASP.NET Core")]
    public void Domain_ShouldNot_Reference_AspNetCore()
    {
        var result = Types.InAssembly(DomainAsm)
            .ShouldNot().HaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Domain must not reference ASP.NET Core. Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact(DisplayName = "Domain must NOT reference Mapperly")]
    public void Domain_ShouldNot_Reference_Mapperly()
    {
        var result = Types.InAssembly(DomainAsm)
            .ShouldNot().HaveDependencyOn("Riok.Mapperly")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Domain must not reference Mapperly — mapping is an Application concern. " +
            $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact(DisplayName = "Domain must NOT reference FluentValidation")]
    public void Domain_ShouldNot_Reference_FluentValidation()
    {
        var result = Types.InAssembly(DomainAsm)
            .ShouldNot().HaveDependencyOn("FluentValidation")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Domain must not reference FluentValidation — input validation belongs in Application. " +
            $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact(DisplayName = "Domain must NOT depend on the Messaging dispatcher")]
    public void Domain_ShouldNot_DependOn_Messaging()
    {
        var result = Types.InAssembly(DomainAsm)
            .ShouldNot().HaveDependencyOn("Company.Product.BuildingBlocks.SharedKernel.Messaging")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Domain must not depend on the Messaging dispatcher (ISender / IRequest). " +
            $"Commands and queries live in Application, not Domain. " +
            $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // ── Defensive rules: Application purity (Phase 9.3) ─────────────────

    [Fact(DisplayName = "Application must NOT reference ASP.NET Core")]
    public void Application_ShouldNot_Reference_AspNetCore()
    {
        var result = Types.InAssembly(ApplicationAsm)
            .ShouldNot().HaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Application must not reference ASP.NET Core — HTTP concerns belong in WebApi. " +
            $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    // ── Module isolation (Phase 9.3) ────────────────────────────────────
    // Each module must be self-contained: a module's code may NOT depend on
    // another module's namespace. Cross-module integration is done by an
    // interface defined in the consumer's Application layer + an implementation
    // in the consumer's Infrastructure that calls a public surface or HTTP.
    //
    // Today only Identity exists; this test still passes (vacuously). When a
    // second module is added (e.g. Catalog), the rule is enforced automatically.

    [Fact(DisplayName = "Modules must not reference other modules directly")]
    public void Modules_ShouldNot_Reference_OtherModules()
    {
        var modulePrefix = "Company.Product.Modules.";

        // Discover loaded module assemblies (Domain/Application/Infrastructure for each module).
        var moduleAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith(modulePrefix, StringComparison.Ordinal) == true)
            .ToList();

        var moduleNames = moduleAssemblies
            .Select(a => a.GetName().Name!.Substring(modulePrefix.Length).Split('.')[0])
            .Distinct()
            .ToList();

        moduleNames.ShouldNotBeEmpty("expected at least one module assembly to be loaded");

        foreach (var moduleAsm in moduleAssemblies)
        {
            var ownModule = moduleAsm.GetName().Name!.Substring(modulePrefix.Length).Split('.')[0];
            var otherModuleNamespaces = moduleNames
                .Where(m => !string.Equals(m, ownModule, StringComparison.Ordinal))
                .Select(m => $"{modulePrefix}{m}")
                .ToList();

            foreach (var otherNs in otherModuleNamespaces)
            {
                var result = Types.InAssembly(moduleAsm)
                    .ShouldNot().HaveDependencyOn(otherNs)
                    .GetResult();

                result.IsSuccessful.ShouldBeTrue(
                    $"{moduleAsm.GetName().Name} must not depend on {otherNs}. " +
                    $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
            }
        }
    }
}
