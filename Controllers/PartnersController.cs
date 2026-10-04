using Microsoft.AspNetCore.Mvc;
using SyncTally.Web.Services.Interfaces;
using SyncTally.Web.ViewModels;

namespace SyncTally.Web.Controllers;

public class PartnersController : BaseController
{
    private readonly IPartnerService _partners;
    public PartnersController(IPartnerService partners) => _partners = partners;

    [HttpGet]
    public async Task<IActionResult> Index(string? q, string? kind, string? sort, int page = 1)
    {
        var vm = await _partners.GetListAsync(q, kind, sort, page);
        return PartnersView(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply([Bind(Prefix = "Apply")] PartnerApplyViewModel apply)
    {
        if (!ModelState.IsValid)
        {
            var vm = await _partners.GetListAsync(null, null, null, 1);
            vm.Apply = apply;
            return PartnersView(vm, "Index");
        }

        await _partners.SubmitApplicationAsync(apply);
        Toast("Application received – our partner team will call you within 2 working days.");
        return RedirectToAction(nameof(Index));
    }

    private IActionResult PartnersView(PartnerListViewModel vm, string view = "Index")
    {
        ViewData["Title"] = "Partners";
        SetCrumbs(new Crumb("Home", Url.Action("Index", "Home")), new Crumb("Partners"));
        return View(view, vm);
    }
}
