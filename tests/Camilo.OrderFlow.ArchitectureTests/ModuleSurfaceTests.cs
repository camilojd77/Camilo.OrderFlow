using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Shouldly;
using System.Reflection;
using Xunit;

namespace Camilo.OrderFlow.ArchitectureTests;

/// <summary>
/// docs/adr/0028-superficie-publica-de-los-modulos.md: a module exposes requests, DTOs, the ports its
/// Infrastructure implements and its composition/authorization entry points. Handlers, validators, pipeline
/// behaviors, mappers, port implementations and the DbContext are internal. Applies to every discovered module.
/// </summary>
public sealed class ModuleSurfaceTests
{
    public static IEnumerable<object[]> Modules() => ModuleCatalog.ModuleNames();

    [Theory(DisplayName = "Application keeps handlers, behaviors, validators and mappers internal")]
    [MemberData(nameof(Modules))]
    public void Application_ImplementationTypes_ShouldNotBePublic(string moduleName)
    {
        var application = ModuleCatalog.Get(moduleName).Application!;

        var offenders = ExportedTypes(application)
            .Where(type =>
                ImplementsOpenGeneric(type, typeof(IRequestHandler<,>))
                || ImplementsOpenGeneric(type, typeof(IPipelineBehavior<,>))
                || DerivesFromGenericNamed(type, "AbstractValidator`1")
                || type.GetCustomAttributes(inherit: false).Any(attribute => attribute.GetType().Name == "MapperAttribute"))
            .Select(type => type.FullName)
            .ToList();

        offenders.ShouldBeEmpty(
            $"En el módulo '{moduleName}' estos tipos de Application son detalle interno y deben ser internal " +
            "(los tests del módulo los ven con InternalsVisibleTo): " + string.Join(", ", offenders));
    }

    [Theory(DisplayName = "Infrastructure keeps its DbContext and port implementations internal")]
    [MemberData(nameof(Modules))]
    public void Infrastructure_ImplementationTypes_ShouldNotBePublic(string moduleName)
    {
        var module = ModuleCatalog.Get(moduleName);
        var portsNamespace = $"{module.ApplicationAssemblyName}.Abstractions";

        var offenders = ExportedTypes(module.Infrastructure!)
            .Where(type =>
                DerivesFromNamed(type, "Microsoft.EntityFrameworkCore.DbContext")
                || (type.IsClass && type.GetInterfaces().Any(port => port.Namespace == portsNamespace)))
            .Select(type => type.FullName)
            .ToList();

        offenders.ShouldBeEmpty(
            $"En el módulo '{moduleName}' el DbContext y las implementaciones de los puertos de Application son " +
            "internal; el host las obtiene por DI: " + string.Join(", ", offenders));
    }

    private static IEnumerable<Type> ExportedTypes(Assembly assembly) =>
        assembly.GetExportedTypes().Where(type => !type.IsNested);

    private static bool ImplementsOpenGeneric(Type type, Type openGeneric) =>
        type.GetInterfaces().Any(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == openGeneric);

    private static bool DerivesFromGenericNamed(Type type, string genericName)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition().Name == genericName)
            {
                return true;
            }
        }

        return false;
    }

    private static bool DerivesFromNamed(Type type, string fullName)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.FullName == fullName)
            {
                return true;
            }
        }

        return false;
    }
}
