using System.Data;
using IRoute.Application.Abstractions;
using IRoute.Application.Commerce;
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
    public async Task<UploadInsertResult> CreateAsync(
        IReadOnlyCollection<CommerceRecord> records,
        string fileName,
        string contentHash,
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
            command.CommandText = "dbo.sp_create_commerce";
            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = dbContext.Database.GetCommandTimeout() ?? 60;
            command.Parameters.Add(BuildTvp(records));
            command.Parameters.Add(new SqlParameter("@file_name", SqlDbType.NVarChar, 255)
            {
                Value = fileName
            });
            command.Parameters.Add(new SqlParameter("@content_hash", SqlDbType.Char, 64)
            {
                Value = contentHash
            });
            command.Parameters.Add(new SqlParameter("@process_date", SqlDbType.Date)
            {
                Value = processDate.ToDateTime(TimeOnly.MinValue)
            });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException(
                    "sp_create_commerce no devolvió el resultado de la carga.");
            }

            var duplicate = reader.FieldCount > 1 && reader.GetBoolean(1);

            return new UploadInsertResult(reader.GetInt32(0), duplicate);
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
        var merchantName = table.Columns.Add("pc_nomcomred", typeof(string));
        merchantName.MaxLength = 200;
        var documentNumber = table.Columns.Add("pc_numdoc", typeof(string));
        documentNumber.MaxLength = 50;
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
