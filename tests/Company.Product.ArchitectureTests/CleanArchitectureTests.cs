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

    private const string DomainAssembly         = "Company.Product.Modules.Identity.Domain";
    private const string ApplicationAssembly    = "Company.Product.Modules.Identity.Application";
    private const string InfrastructureAssembly = "Company.Product.Modules.Identity.Infrastructure";
    private const string WebApiAssembly         = "Company.Product.WebApi";

    // Anchor types are used so NetArchTest can locate the assembly on disk.
    private static readonly System.Reflection.Assembly DomainAsm         = typeof(Company.Product.Modules.Identity.Domain.Entities.User).Assembly;
    private static readonly System.Reflection.Assembly ApplicationAsm    = typeof(Company.Product.Modules.Identity.Application.DependencyInjection.ServiceCollectionExtensions).Assembly;
    private static readonly System.Reflection.Assembly InfrastructureAsm = typeof(Company.Product.Modules.Identity.Infrastructure.DependencyInjection.ServiceCollectionExtensions).Assembly;
    private static readonly System.Reflection.Assembly WebApiAsm         = typeof(Program).Assembly;

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
}
