using Microsoft.AspNetCore.Mvc;
using SyncTally.Web.Services.Interfaces;
using SyncTally.Web.ViewModels;

namespace SyncTally.Web.Controllers;

public class BlogController : BaseController
{
    private readonly IBlogService _blog;
    public BlogController(IBlogService blog) => _blog = blog;

    public async Task<IActionResult> Index(string? q, string? tag, string? sort, int page = 1)
    {
        ViewData["Title"] = "Blog";
        SetCrumbs(new Crumb("Home", Url.Action("Index", "Home")), new Crumb("Blog"));
        return View(await _blog.GetListAsync(q, tag, sort, page));
    }

    public async Task<IActionResult> Details(string id)
    {
        var vm = await _blog.GetDetailAsync(id ?? string.Empty);
        if (vm == null) return NotFound();
        ViewData["Title"] = vm.Post.Title;
        SetCrumbs(new Crumb("Home", Url.Action("Index", "Home")),
                  new Crumb("Blog", Url.Action("Index", "Blog")),
                  new Crumb(vm.Post.Title));
        return View(vm);
    }
}
