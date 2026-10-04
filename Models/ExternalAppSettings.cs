namespace SyncTally.Web.Models;

/// <summary>
/// Where "Sign in" / "Try the demo" and the end of the trial sign-up send the visitor.
/// This website ends at the login page – the application itself lives in tallywebapp.
/// </summary>
public class ExternalAppSettings
{
    public string LoginUrl { get; set; } = "https://localhost:7299/tallywebapp/Login";
}
