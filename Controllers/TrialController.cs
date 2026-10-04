using Microsoft.AspNetCore.Mvc;
using SyncTally.Web.Services.Interfaces;
using SyncTally.Web.ViewModels;

namespace SyncTally.Web.Controllers;

/// <summary>
/// 15-day free trial: 1) contact details → saved in DB, OTP e-mailed  2) enter OTP + choose password
/// 3) hand the visitor over to the application (tallywebapp) with the e-mail as the user name.
/// </summary>
public class TrialController : BaseController
{
    private readonly ITrialService _trial;
    private readonly IAuthService _auth;
    public TrialController(ITrialService trial, IAuthService auth)
    {
        _trial = trial;
        _auth = auth;
    }

    [HttpGet]
    public IActionResult Index(string? plan)
    {
        PrepareStart();
        return View(new TrialStartViewModel { Plan = plan });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(TrialStartViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _trial.StartAsync(model, ClientIp);
            if (result.Ok)
            {
                if (!string.IsNullOrEmpty(result.Message)) Toast(result.Message, "info");
                return RedirectToAction(nameof(Verify), new { id = result.Reference });
            }
            if (result.EmailExists) ViewData["EmailExists"] = true;
            else ModelState.AddModelError(result.Field, result.Message ?? "Something went wrong.");
        }
        PrepareStart();
        return View(model);
    }

    [HttpGet("Trial/Verify/{id:guid}")]
    public async Task<IActionResult> Verify(Guid id)
    {
        var vm = await _trial.GetVerifyModelAsync(id);
        if (vm == null) return NotFound();
        PrepareVerify();
        return View(vm);
    }

    [HttpPost("Trial/Verify/{id:guid}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Verify(Guid id, TrialVerifyViewModel model)
    {
        model.Ref = id;
        if (ModelState.IsValid)
        {
            var result = await _trial.VerifyAsync(model);
            if (result.Ok) return RedirectToAction(nameof(Welcome), new { id });
            if (result.EmailExists)
            {
                // Rare race: the e-mail became a user while the OTP was pending → send them back to the first step.
                Toast("This email is already registered with SyncTally. Please sign in, or start again with a different email.", "error");
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(result.Field, result.Message ?? "Something went wrong.");
        }
        // Re-hydrate display-only fields; never echo passwords or the code back.
        var fresh = await _trial.GetVerifyModelAsync(id);
        if (fresh == null) return NotFound();
        model.MaskedEmail = fresh.MaskedEmail;
        model.ResendInSeconds = fresh.ResendInSeconds;
        model.Password = model.ConfirmPassword = model.Otp = string.Empty;
        foreach (var key in new[] { nameof(model.Password), nameof(model.ConfirmPassword), nameof(model.Otp) })
        {
            if (ModelState.TryGetValue(key, out var entry)) { entry.RawValue = null; entry.AttemptedValue = null; } // keep the error, drop the value
        }
        PrepareVerify();
        return View(model);
    }

    [HttpPost("Trial/Resend/{id:guid}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Resend(Guid id)
    {
        var result = await _trial.ResendAsync(id);
        if (result.Ok && result.AlreadyActive) return RedirectToAction(nameof(Welcome), new { id });
        Toast(result.Message ?? "Done.", result.Ok ? "ok" : "error");
        return RedirectToAction(nameof(Verify), new { id });
    }

    [HttpGet("Trial/Welcome/{id:guid}")]
    public async Task<IActionResult> Welcome(Guid id)
    {
        var vm = await _trial.GetWelcomeAsync(id);
        if (vm == null) return NotFound();
        ViewData["Title"] = "Your trial is active";
        return View(vm);
    }

    private void PrepareStart()
    {
        ViewData["LoginUrl"] = _auth.LoginUrl;
        ViewData["Title"] = "Start your free trial";
        SetCrumbs(new Crumb("Home", Url.Action("Index", "Home")), new Crumb("Free trial"));
    }

    private void PrepareVerify()
    {
        ViewData["Title"] = "Verify your email";
        SetCrumbs(new Crumb("Home", Url.Action("Index", "Home")), new Crumb("Free trial", Url.Action("Index", "Trial")), new Crumb("Verify email"));
    }
}
