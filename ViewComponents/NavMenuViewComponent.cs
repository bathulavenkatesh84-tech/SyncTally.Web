using Microsoft.AspNetCore.Mvc;
using SyncTally.Web.Services.Interfaces;

namespace SyncTally.Web.ViewComponents;

public class NavMenuViewComponent : ViewComponent
{
    private readonly IFeatureService _features;
    public NavMenuViewComponent(IFeatureService features) => _features = features;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        string controller = ViewContext.RouteData.Values["controller"]?.ToString() ?? string.Empty;
        return View(await _features.GetMenuAsync(controller));
    }
}
