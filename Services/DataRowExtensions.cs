using System.Data;
using System.Globalization;

namespace SyncTally.Web.Services;

/// <summary>Null-safe readers used when turning DataRows into view models.</summary>
public static class DataRowExtensions
{
    public static string Str(this DataRow r, string col) =>
        r[col] == DBNull.Value ? string.Empty : Convert.ToString(r[col], CultureInfo.InvariantCulture) ?? string.Empty;

    public static int Int(this DataRow r, string col) =>
        r[col] == DBNull.Value ? 0 : Convert.ToInt32(r[col], CultureInfo.InvariantCulture);

    public static decimal Dec(this DataRow r, string col) =>
        r[col] == DBNull.Value ? 0m : Convert.ToDecimal(r[col], CultureInfo.InvariantCulture);

    public static bool Bool(this DataRow r, string col) =>
        r[col] != DBNull.Value && Convert.ToBoolean(r[col], CultureInfo.InvariantCulture);

    public static DateTime Date(this DataRow r, string col) =>
        r[col] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r[col], CultureInfo.InvariantCulture);

    /// <summary>PostgreSQL text[] columns arrive as string[].</summary>
    public static string[] Arr(this DataRow r, string col) =>
        r[col] is string[] a ? a : Array.Empty<string>();
}
