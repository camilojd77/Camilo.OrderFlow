using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Persistence;
using Camilo.OrderFlow.Modules.Identity.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Camilo.OrderFlow.ArchitectureTests;

/// <summary>
/// Reglas sobre CÓMO registra sus servicios cada módulo.
///
/// A diferencia de <see cref="CleanArchitectureTests"/>, que inspecciona referencias
/// entre ensamblados, estas pruebas ejecutan la extensión de DI del módulo y examinan
/// los <see cref="ServiceDescriptor"/> resultantes.
/// </summary>
public sealed class ModuleRegistrationTests
{
    /// <summary>
    /// Configuración mínima para que <c>AddIdentityInfrastructure</c> no falle.
    /// No se abre ninguna conexión: <c>AddDbContext</c> solo guarda las opciones.
    /// </summary>
    private static IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Server=(localdb)\\mssqllocaldb;Database=ArchTestDb;Trusted_Connection=true",
            })
            .Build();

    /// <summary>
    /// <see cref="IExecutionContext"/> vive en SharedKernel y es global: si dos módulos
    /// registraran esa misma interfaz, la última registración ganaría y los handlers del
    /// otro módulo recibirían el DbContext equivocado — sin excepción, simplemente sin
    /// persistir. Cada módulo debe registrar su propia interfaz derivada
    /// (p. ej. <c>IIdentityExecutionContext</c>).
    /// </summary>
    [Fact(DisplayName = "Ningún módulo registra IExecutionContext directamente")]
    public void Modules_ShouldNot_Register_BareExecutionContext()
    {
        var services = new ServiceCollection();

        // Al añadir un módulo nuevo, registrar aquí también su Add<Modulo>Infrastructure.
        services.AddIdentityInfrastructure(BuildConfiguration());

        var offenders = services
            .Where(d => d.ServiceType == typeof(IExecutionContext))
            .Select(d => d.ImplementationType?.FullName ?? "<factory>")
            .ToList();

        offenders.ShouldBeEmpty(
            "Registrar IExecutionContext directamente hace que el último módulo registrado " +
            "gane y rompe silenciosamente el commit de los demás. Define " +
            "I<Modulo>ExecutionContext : IExecutionContext en la capa Application del módulo " +
            "y registra esa interfaz. Registros infractores: " +
            string.Join(", ", offenders));
    }

    /// <summary>
    /// Contrapartida del test anterior: el módulo Identity sí debe dejar resoluble su
    /// propio punto de commit, o los handlers no podrían persistir nada.
    /// </summary>
    [Fact(DisplayName = "Identity registra su propio execution context")]
    public void Identity_Should_Register_ItsOwn_ExecutionContext()
    {
        var services = new ServiceCollection();
        services.AddIdentityInfrastructure(BuildConfiguration());

        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(Modules.Identity.Application.Abstractions.IIdentityExecutionContext));

        descriptor.ShouldNotBeNull("Identity debe registrar IIdentityExecutionContext en AddIdentityInfrastructure.");
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }
}
