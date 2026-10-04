namespace SyncTally.Web.Models;

/// <summary>Bound from the "Smtp" section. Leave Host empty in development: e-mails are then written to the log.</summary>
public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "no-reply@synctally.example";
    public string FromName { get; set; } = "SyncTally";
}
