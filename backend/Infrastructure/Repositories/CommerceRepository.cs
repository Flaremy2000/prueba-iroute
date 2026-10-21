using System.Data;
using System.Globalization;
using IRoute.Application.Abstractions;
using IRoute.Domain.Entities;
using IRoute.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace IRoute.Infrastructure.Repositories;

/// <summary>
/// Carga y procesa mediante los stored procedures de SQL Server; la consulta usa EF Core.
/// </summary>
public sealed class CommerceRepository(CommerceDbContext dbContext) : ICommerceRepository
{
    public async Task<int> CreateAsync(
        IReadOnlyCollection<CommerceRecord> records,
        CancellationToken cancellationToken)
    {
        EnsureSqlServer();

        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State == ConnectionState.Closed;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.sp_create_commerce";
            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = dbContext.Database.GetCommandTimeout() ?? 60;
            command.Parameters.Add(BuildTvp(records));

            var result = await command.ExecuteScalarAsync(cancellationToken);

            if (result is null || result == DBNull.Value)
            {
                throw new InvalidOperationException(
                    "sp_create_commerce no devolvió la cantidad de filas insertadas.");
            }

            return Convert.ToInt32(result, CultureInfo.InvariantCulture);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    public async Task<ProcessCounts> ProcessAsync(
        DateOnly processDate,
        CancellationToken cancellationToken)
    {
        EnsureSqlServer();

        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State == ConnectionState.Closed;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.sp_process_commerce";
            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = dbContext.Database.GetCommandTimeout() ?? 60;
            command.Parameters.Add(new SqlParameter("@pc_processdate", SqlDbType.Date)
            {
                Value = processDate.ToDateTime(TimeOnly.MinValue)
            });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException(
                    "sp_process_commerce no devolvió el resultado del proceso.");
            }

            return new ProcessCounts(
                reader.GetInt32(0),
                reader.GetInt32(1));
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private void EnsureSqlServer()
    {
        if (!dbContext.Database.IsSqlServer())
        {
            throw new NotSupportedException(
                "La carga y el procesamiento usan sp_create_commerce y sp_process_commerce, " +
                "que están implementados para SQL Server. Configura Database:Provider=SqlServer " +
                "o implementa los stored procedures equivalentes para el otro motor.");
        }
    }

    private static SqlParameter BuildTvp(IReadOnlyCollection<CommerceRecord> records)
    {
        var table = new DataTable();
        table.Columns.Add("pc_nomcomred", typeof(string));
        table.Columns.Add("pc_numdoc", typeof(string));
        table.Columns.Add("pc_processdate", typeof(DateTime));

        foreach (var record in records)
        {
            table.Rows.Add(
                (object?)record.MerchantName ?? DBNull.Value,
                (object?)record.DocumentNumber ?? DBNull.Value,
                record.ProcessDate.ToDateTime(TimeOnly.MinValue));
        }

        return new SqlParameter("@rows", SqlDbType.Structured)
        {
            TypeName = "dbo.CommerceTvp",
            Value = table
        };
    }
}
