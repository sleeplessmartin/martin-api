using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ProductsApi.Application.Common.Behaviors;
using ProductsApi.Application.Common.Interfaces;
using ProductsApi.Application.Common.Services;

namespace ProductsApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            // Order matters: Logging wraps Validation which wraps the handler
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        return services;
    }
}
