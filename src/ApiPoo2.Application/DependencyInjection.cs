using System.Reflection;
using ApiPoo2.Application.Pipeline;
using ApiPoo2.Application.CQRS;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ApiPoo2.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, Assembly? assembly = null)
    {
        var targetAssembly = assembly ?? typeof(ICommand<>).Assembly;

        services.AddScoped<IDispatcher, Dispatcher>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        RegisterHandlers(services, targetAssembly);
        services.AddValidatorsFromAssembly(targetAssembly, includeInternalTypes: true);

        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly)
    {
        var handlerTypes = new[]
        {
            typeof(ICommandHandler<,>),
            typeof(IQueryHandler<,>),
        };

        foreach (var type in assembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false }))
        {
            foreach (var serviceType in type.GetInterfaces())
            {
                if (serviceType.IsGenericType && handlerTypes.Contains(serviceType.GetGenericTypeDefinition()))
                {
                    services.AddTransient(serviceType, type);
                }
            }
        }
    }
}