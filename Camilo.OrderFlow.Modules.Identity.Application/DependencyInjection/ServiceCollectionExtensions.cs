using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Behaviors;
using Camilo.OrderFlow.Modules.Identity.Application.Mapping;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Camilo.OrderFlow.Modules.Identity.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services)
    {
        var assembly = typeof(ServiceCollectionExtensions).Assembly;

        RegisterHandlersFromAssembly(services, assembly);

        // Validators are internal to the module (docs/adr/0028-superficie-publica-de-los-modulos.md).
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>));

        // Mapperly mappers — stateless source-generated classes registered as Singleton
        services.AddSingleton<UserMapper>();
        services.AddSingleton<RoleMapper>();
        services.AddSingleton<PermissionMapper>();

        return services;
    }

    private static void RegisterHandlersFromAssembly(IServiceCollection services, Assembly assembly)
    {
        var handlerInterface = typeof(IRequestHandler<,>);

        var registrations = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterface)
                .Select(i => (Implementation: t, Interface: i)));

        foreach (var (impl, iface) in registrations)
            services.AddScoped(iface, impl);
    }
}
