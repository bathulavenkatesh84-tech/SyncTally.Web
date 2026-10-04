using Npgsql;

namespace SyncTally.Web.Repositories;

/// <summary>Tiny helper that builds parameterised commands (null becomes DBNull).</summary>
internal static class SqlHelper
{
    public static NpgsqlCommand Cmd(string sql, params (string Name, object? Value)[] parameters)
    {
        var cmd = new NpgsqlCommand(sql);
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return cmd;
    }

    public static NpgsqlCommand Cmd(string sql, NpgsqlConnection connection, NpgsqlTransaction tx, params (string Name, object? Value)[] parameters)
    {
        var cmd = new NpgsqlCommand(sql, connection, tx);
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return cmd;
    }
}
