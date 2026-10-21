namespace IRoute.Infrastructure.Database;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public DatabaseProvider Provider { get; set; } = DatabaseProvider.SqlServer;
    public string ConnectionString { get; set; } = string.Empty;
    public int CommandTimeoutSeconds { get; set; } = 60;
}

public enum DatabaseProvider
{
    SqlServer,
    PostgreSql,
    MySql
}
