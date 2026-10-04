using Microsoft.AspNetCore.Mvc;
using SyncTally.Web.ViewModels;

namespace SyncTally.Web.Controllers;

/// <summary>
/// Guided product demo. Everything runs in the visitor's browser on a fictional, built-in dataset –
/// there is no login and no database access, so no customer data can ever appear here.
/// </summary>
public class DemoController : BaseController
{
    [HttpGet]
    public IActionResult Index()
    {
        ViewData["Title"] = "Try the demo";
        ViewData["Description"] = "Try SyncTally in your browser: place an order, approve it, send the invoice on WhatsApp and see it post to Tally – with sample data, no sign-up.";
        SetCrumbs(new Crumb("Home", Url.Action("Index", "Home")), new Crumb("Try the demo"));
        return View();
    }
}
