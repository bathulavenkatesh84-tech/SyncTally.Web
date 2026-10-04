namespace SyncTally.Web.Models;

/// <summary>Bound from the "DatabaseSettings" section of appsettings.json.</summary>
public class DatabaseSettings
{
    public string ConnectionString { get; set; } = string.Empty;
}
