namespace SyncTally.Web.Models;

/// <summary>Bound from the "Trial" section of appsettings.json.</summary>
public class TrialSettings
{
    public int Days { get; set; } = 15;
    public int OtpMinutes { get; set; } = 10;
    public int MaxOtpAttempts { get; set; } = 5;
    public int MaxOtpSends { get; set; } = 5;
    public int ResendSeconds { get; set; } = 60;
}
