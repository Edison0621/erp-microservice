using System.Reflection;
using ErpSystem.BuildingBlocks.Auth;
using ErpSystem.BuildingBlocks.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSystem.BuildingBlocks;

public static class DependencyInjection
{
    public static IServiceCollection AddBuildingBlocks(this IServiceCollection services, Assembly[] assembliesToScan)
    {
        // 1. Register Validators
        foreach (Assembly assembly in assembliesToScan)
        {
            services.AddValidatorsFromAssembly(assembly);
        }

        // 2. Register MediatR Behaviors
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // 3. Register UserContext
        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();

        // 4. Dapr Providers
        services.AddScoped<Dapr.IDaprSecretsProvider, Dapr.DaprSecretsProvider>();
        services.AddScoped<Dapr.IDaprConfigurationProvider, Dapr.DaprConfigurationProvider>();

        return services;
    }
}
