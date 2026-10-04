using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SyncTally.Web.Services.Interfaces;
using SyncTally.Web.ViewModels;

namespace SyncTally.Web.Controllers;

public class HomeController : BaseController
{
    private readonly ISiteContentService _content;
    private readonly IContactService _contact;

    public HomeController(ISiteContentService content, IContactService contact)
    {
        _content = content;
        _contact = contact;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "SyncTally – Mobile order & billing app that syncs with Tally";
        return View(await _content.GetHomeAsync());
    }

    public IActionResult About()
    {
        ViewData["Title"] = "About";
        SetCrumbs(new Crumb("Home", Url.Action("Index", "Home")), new Crumb("About"));
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Contact()
    {
        ViewData["Title"] = "Contact";
        SetCrumbs(new Crumb("Home", Url.Action("Index", "Home")), new Crumb("Contact"));
        return View(await _content.GetContactPageAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact([Bind(Prefix = "Form")] ContactViewModel form)
    {
        if (!ContactViewModel.Topics.Contains(form.Topic))
            ModelState.AddModelError("Form.Topic", "Please choose a valid topic.");

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Contact";
            SetCrumbs(new Crumb("Home", Url.Action("Index", "Home")), new Crumb("Contact"));
            return View(await _content.GetContactPageAsync(form));
        }

        await _contact.SubmitContactAsync(form, ClientIp);
        Toast("Thanks – your message has been sent. We'll reply within one working day.");
        return RedirectToAction(nameof(Contact));
    }

    [Route("Home/Error")]
    public IActionResult Error()
    {
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        return View("Error", new ErrorViewModel
        {
            StatusCode = 500,
            RequestId = HttpContext.Items["RequestId"] as string ?? Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }

    /// <summary>Target of UseStatusCodePagesWithReExecute (404, 403 …).</summary>
    [Route("Home/StatusCodePage/{code:int}")]
    public IActionResult StatusCodePage(int code)
    {
        Response.StatusCode = code;
        var vm = code == 404
            ? new ErrorViewModel { StatusCode = 404, Title = "Page not found", Message = "The page you are looking for does not exist or has moved." }
            : new ErrorViewModel { StatusCode = code, Title = "Request could not be completed", Message = "Please go back and try again." };
        vm.RequestId = HttpContext.TraceIdentifier;
        return View(code == 404 ? "NotFound" : "Error", vm);
    }
}
