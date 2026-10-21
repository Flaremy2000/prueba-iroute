using System.Text;
using IRoute.Application.Abstractions;
using IRoute.Infrastructure.Csv;
using IRoute.Infrastructure.Database;
using IRoute.Infrastructure.Persistence;
using IRoute.Infrastructure.Repositories;
using IRoute.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IRoute.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                "Database:ConnectionString es obligatorio.")
            .Validate(
                options => Enum.IsDefined(options.Provider),
                "Database:Provider no es válido.")
            .ValidateOnStart();

        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Issuer),
                "Jwt:Issuer es obligatorio.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Audience),
                "Jwt:Audience es obligatorio.")
            .Validate(
                options => Encoding.UTF8.GetByteCount(options.Key) >= 32,
                "Jwt:Key debe tener al menos 32 bytes.")
            .Validate(
                options => options.ExpirationMinutes is >= 1 and <= 1440,
                "Jwt:ExpirationMinutes debe estar entre 1 y 1440.")
            .ValidateOnStart();

        services.AddDbContextPool<CommerceDbContext>((provider, options) =>
        {
            var database = provider
                .GetRequiredService<IOptions<DatabaseOptions>>()
                .Value;

            switch (database.Provider)
            {
                case DatabaseProvider.SqlServer:
                    options.UseSqlServer(database.ConnectionString, sqlServer =>
                    {
                        sqlServer.CommandTimeout(database.CommandTimeoutSeconds);
                        sqlServer.EnableRetryOnFailure(3);
                    });
                    break;

                case DatabaseProvider.PostgreSql:
                    options.UseNpgsql(database.ConnectionString, npgsql =>
                        npgsql.CommandTimeout(database.CommandTimeoutSeconds));
                    break;

                case DatabaseProvider.MySql:
                    options.UseMySql(
                        database.ConnectionString,
                        ServerVersion.AutoDetect(database.ConnectionString),
                        mysql => mysql.CommandTimeout(database.CommandTimeoutSeconds));
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Proveedor de base de datos no soportado: {database.Provider}.");
            }
        });

        services.AddScoped<ICsvCommerceParser, CsvCommerceParser>();
        services.AddScoped<ICommerceRepository, CommerceRepository>();
        services.AddScoped<IQuarantineRepository, QuarantineRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        return services;
    }
}
