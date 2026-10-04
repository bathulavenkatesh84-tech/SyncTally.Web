using Microsoft.AspNetCore.Mvc;
using SyncTally.Web.Services.Interfaces;
using SyncTally.Web.ViewModels;

namespace SyncTally.Web.Controllers;

public class FeaturesController : BaseController
{
    private readonly IFeatureService _features;
    public FeaturesController(IFeatureService features) => _features = features;

    public async Task<IActionResult> Index(string? q, string? status)
    {
        ViewData["Title"] = "Features";
        SetCrumbs(new Crumb("Home", Url.Action("Index", "Home")), new Crumb("Features"));
        return View(await _features.GetListAsync(q, status));
    }

    public async Task<IActionResult> Details(string id)
    {
        var vm = await _features.GetDetailAsync(id ?? string.Empty);
        if (vm == null) return NotFound();
        ViewData["Title"] = vm.Feature.Title;
        SetCrumbs(new Crumb("Home", Url.Action("Index", "Home")),
                  new Crumb("Features", Url.Action("Index", "Features")),
                  new Crumb(vm.Feature.Title));
        return View(vm);
    }
}
