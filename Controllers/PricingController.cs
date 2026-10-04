using Microsoft.AspNetCore.Mvc;
using SyncTally.Web.Services.Interfaces;
using SyncTally.Web.ViewModels;

namespace SyncTally.Web.Controllers;

public class PricingController : BaseController
{
    private readonly IPricingService _pricing;
    public PricingController(IPricingService pricing) => _pricing = pricing;

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Pricing";
        SetCrumbs(new Crumb("Home", Url.Action("Index", "Home")), new Crumb("Pricing"));
        return View(await _pricing.GetPlansAsync());
    }
}
