using Microsoft.Extensions.Options;
using SyncTally.Web.Models;
using SyncTally.Web.Services.Interfaces;

namespace SyncTally.Web.Services.Implementations;

/// <summary>Sign-in lives in the tallywebapp application – this site only links to it.</summary>
public class AuthService : IAuthService
{
    private readonly ExternalAppSettings _settings;
    public AuthService(IOptions<ExternalAppSettings> options) => _settings = options.Value;

    public string LoginUrl => _settings.LoginUrl;

    public string BuildLoginUrl(string username) =>
        _settings.LoginUrl + (_settings.LoginUrl.Contains('?') ? "&" : "?") + "username=" + Uri.EscapeDataString(username);
}
