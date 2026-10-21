using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IRoute.Api.Configuration;

public static class CorsConfiguration
{
    private const string PolicyName = "Frontend";

    public static IServiceCollection AddFrontendCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? ["http://localhost:4200"];

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod());
        });

        return services;
    }

    public static string FrontendPolicy => PolicyName;
}
