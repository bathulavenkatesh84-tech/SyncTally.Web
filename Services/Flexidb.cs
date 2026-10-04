using System.Data;
using System.Globalization;
using Microsoft.Extensions.Options;
using Npgsql;
using SyncTally.Web.Models;
using SyncTally.Web.Services.Interfaces;

namespace SyncTally.Web.Services;

/// <summary>
/// Flexidb – the single place that opens PostgreSQL connections.
/// Every connection/command is wrapped in <c>using</c>; every query is parameterised by the caller.
/// Unlike the legacy version, errors are logged AND re-thrown so the central exception middleware
/// can show the friendly error page instead of silently returning empty tables.
/// </summary>
public class Flexidb : IFlexidb
{
    private readonly string _connectionString;

    public Flexidb(IOptions<DatabaseSettings> dbSettings)
    {
        _connectionString = dbSettings.Value.ConnectionString;
        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new InvalidOperationException("DatabaseSettings:ConnectionString is missing in appsettings.json.");
    }

    public string ConnectionString => _connectionString;

    // ------------------------------------------------------------------
    // Synchronous API (kept for parity with the original Flexidb)
    // ------------------------------------------------------------------
    public DataTable Getdt(string sql)
    {
        using var cmd = new NpgsqlCommand(sql);
        return Getdtcmd(cmd);
    }

    public DataTable Getdtcmd(NpgsqlCommand cmd)
    {
        var dt = new DataTable();
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            cmd.Connection = connection;
            connection.Open();
            using var reader = cmd.ExecuteReader();
            dt.Load(reader);
            return dt;
        }
        catch (Exception ex)
        {
            WriteError(nameof(Getdtcmd), cmd.CommandText + " :: " + ex);
            throw;
        }
    }

    public string GetScalarcmd(NpgsqlCommand cmd)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            cmd.Connection = connection;
            connection.Open();
            return Convert.ToString(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) ?? string.Empty;
        }
        catch (Exception ex)
        {
            WriteError(nameof(GetScalarcmd), cmd.CommandText + " :: " + ex);
            throw;
        }
    }

    public int InsertDbRowcmd(NpgsqlCommand cmd)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            cmd.Connection = connection;
            connection.Open();
            return cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            WriteError(nameof(InsertDbRowcmd), cmd.CommandText + " :: " + ex);
            throw;
        }
    }

    // ------------------------------------------------------------------
    // Asynchronous API
    // ------------------------------------------------------------------
    public async Task<DataTable> GetdtAsync(NpgsqlCommand cmd, CancellationToken ct = default)
    {
        var dt = new DataTable();
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            cmd.Connection = connection;
            await connection.OpenAsync(ct);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            dt.Load(reader);
            return dt;
        }
        catch (Exception ex)
        {
            WriteError(nameof(GetdtAsync), cmd.CommandText + " :: " + ex);
            throw;
        }
        finally
        {
            cmd.Dispose();
        }
    }

    public async Task<object?> GetScalarAsync(NpgsqlCommand cmd, CancellationToken ct = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            cmd.Connection = connection;
            await connection.OpenAsync(ct);
            var value = await cmd.ExecuteScalarAsync(ct);
            return value == DBNull.Value ? null : value;
        }
        catch (Exception ex)
        {
            WriteError(nameof(GetScalarAsync), cmd.CommandText + " :: " + ex);
            throw;
        }
        finally
        {
            cmd.Dispose();
        }
    }

    public async Task<int> ExecuteAsync(NpgsqlCommand cmd, CancellationToken ct = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            cmd.Connection = connection;
            await connection.OpenAsync(ct);
            return await cmd.ExecuteNonQueryAsync(ct);
        }
        catch (Exception ex)
        {
            WriteError(nameof(ExecuteAsync), cmd.CommandText + " :: " + ex);
            throw;
        }
        finally
        {
            cmd.Dispose();
        }
    }

    public async Task<T> ExecuteTransactionAsync<T>(Func<NpgsqlConnection, NpgsqlTransaction, Task<T>> work, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        await using var tx = await connection.BeginTransactionAsync(ct);
        try
        {
            var result = await work(connection, tx);
            await tx.CommitAsync(ct);
            return result;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            WriteError(nameof(ExecuteTransactionAsync), ex.ToString());
            throw;
        }
    }

    // ------------------------------------------------------------------
    // Low-level file log (same format as the legacy Flexidb.WriteError). The database error
    // log is handled by IErrorLogService – this file log is the fallback if the DB itself is down.
    // ------------------------------------------------------------------
    private static readonly object FileLock = new();

    public static void WriteError(string method, string errorMessage)
    {
        try
        {
            string folder = Path.Combine(Directory.GetCurrentDirectory(), "ErrorLog");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, DateTime.Today.ToString("dd'-'MM'-'yyyy") + ".txt");
            lock (FileLock)
            {
                File.AppendAllText(path,
                    $"\r\nLog Entry : \r\n{DateTime.Now.ToString(CultureInfo.InvariantCulture)}\r\n{method}---{errorMessage}\r\n__________________________\r\n");
            }
        }
        catch
        {
            // logging must never throw
        }
    }
}
