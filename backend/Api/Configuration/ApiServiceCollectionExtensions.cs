using IRoute.Api.Middleware;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IRoute.Api.Configuration;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApi(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddControllers();
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddApiDocumentation();
        services.AddJwtAuthentication(configuration, environment);
        services.AddLoginRateLimiting();
        services.AddFrontendCors(configuration);

        return services;
    }
}
