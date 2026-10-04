using System.Data;
using Npgsql;

namespace SyncTally.Web.Services.Interfaces;

/// <summary>
/// Data-access service used by every repository (plain ADO.NET, no ORM).
/// Method names keep the original Flexidb conventions (Getdt, Getdtcmd, GetScalarcmd, InsertDbRowcmd)
/// and add async/transaction variants.
/// </summary>
public interface IFlexidb
{
    string ConnectionString { get; }

    // ---- synchronous API (original Flexidb names) ----
    DataTable Getdt(string sql);
    DataTable Getdtcmd(NpgsqlCommand cmd);
    string GetScalarcmd(NpgsqlCommand cmd);
    int InsertDbRowcmd(NpgsqlCommand cmd);

    // ---- asynchronous API ----
    Task<DataTable> GetdtAsync(NpgsqlCommand cmd, CancellationToken ct = default);
    Task<object?> GetScalarAsync(NpgsqlCommand cmd, CancellationToken ct = default);
    Task<int> ExecuteAsync(NpgsqlCommand cmd, CancellationToken ct = default);

    /// <summary>Runs several statements on one connection inside a single transaction (commit/rollback handled here).</summary>
    Task<T> ExecuteTransactionAsync<T>(Func<NpgsqlConnection, NpgsqlTransaction, Task<T>> work, CancellationToken ct = default);
}
