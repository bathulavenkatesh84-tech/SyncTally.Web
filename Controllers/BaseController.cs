using Microsoft.AspNetCore.Mvc;
using SyncTally.Web.ViewModels;

namespace SyncTally.Web.Controllers;

public abstract class BaseController : Controller
{
    /// <summary>Breadcrumb trail shown under the header; the last crumb has no URL.</summary>
    protected void SetCrumbs(params Crumb[] crumbs) => ViewData["Crumbs"] = crumbs.ToList();

    protected void Toast(string message, string kind = "ok") =>
        TempData["Toast"] = kind + "|" + message;

    protected string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();
}
