using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSystem.BuildingBlocks.Auth;

public static class AuthExtensions
{
    public static IServiceCollection AddSignatureVerification(this IServiceCollection services)
    {
        // Note: The consuming application must register an implementation of IApiClientRepository
        return services;
    }

    public static IApplicationBuilder UseSignatureVerification(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SignatureVerificationMiddleware>();
    }
}
