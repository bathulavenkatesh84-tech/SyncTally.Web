using System.Globalization;

namespace SyncTally.Web.Helpers;

/// <summary>Small view helpers (money formatting that survives InvariantGlobalization hosts).</summary>
public static class Fmt
{
    private static readonly CultureInfo Inr = Create();

    private static CultureInfo Create()
    {
        try { return CultureInfo.GetCultureInfo("en-IN"); }
        catch (CultureNotFoundException) { return CultureInfo.InvariantCulture; }
    }

    public static string Money(decimal amount) => "₹" + amount.ToString("N0", Inr);
    public static string Date(DateTime d) => d.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
}
