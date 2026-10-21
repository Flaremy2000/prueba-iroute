using IRoute.Application.Abstractions;
using IRoute.Application.Commerce;
using IRoute.Application.Quarantine;
using Microsoft.Extensions.DependencyInjection;

namespace IRoute.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommerceCommandService, CommerceCommandService>();
        services.AddScoped<IQuarantineQueryService, QuarantineQueryService>();

        return services;
    }
}
