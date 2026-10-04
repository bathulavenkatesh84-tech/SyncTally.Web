using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SyncTally.Web.Services.Interfaces;

namespace SyncTally.Web.Controllers;

/// <summary>
/// This public site ends at the login page: Sign in / Start free trial send the visitor to the
/// tallywebapp application (configured in appsettings → ExternalApp).
/// </summary>
public class AccountController : Controller
{
    private readonly IAuthService _auth;
    public AccountController(IAuthService auth) => _auth = auth;

    [HttpGet]
    public IActionResult Login() => Redirect(_auth.LoginUrl);

    [HttpGet]
    public IActionResult Register() => RedirectToAction("Index", "Trial");

    /// <summary>Clears this site's cookie (if any) and returns home.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied() => RedirectToAction("StatusCodePage", "Home", new { code = 403 });
}
