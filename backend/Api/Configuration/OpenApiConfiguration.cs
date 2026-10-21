using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace IRoute.Api.Configuration;

public static class OpenApiConfiguration
{
    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi("v1", options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "i-Route Commerce API",
                    Version = "v1",
                    Description = "Autenticación, carga, procesamiento y consulta de comercios desde archivos CSV."
                };

                return Task.CompletedTask;
            });
        });

        return services;
    }
}
