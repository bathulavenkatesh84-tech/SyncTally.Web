using Microsoft.AspNetCore.Mvc;
using SyncTally.Web.Services.Interfaces;
using SyncTally.Web.ViewModels;

namespace SyncTally.Web.Controllers;

public class WhatsAppController : Controller
{
    private readonly IContactService _contact;
    public WhatsAppController(IContactService contact) => _contact = contact;

    /// <summary>AJAX endpoint of the "Chat on WhatsApp" dialog. Anti-forgery token comes in the RequestVerificationToken header.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Lead([FromBody] ChatLeadViewModel? model)
    {
        if (model != null && !ChatLeadViewModel.Topics.Contains(model.Topic))
            ModelState.AddModelError(nameof(model.Topic), "Please choose a valid topic.");

        if (model == null || !ModelState.IsValid)
        {
            var errors = ModelState
                .Where(kv => kv.Value?.Errors.Count > 0)
                .ToDictionary(kv => kv.Key.TrimStart('$', '.'), kv => kv.Value!.Errors.Select(e => e.ErrorMessage).ToArray());
            return BadRequest(new { ok = false, errors });
        }

        string url = await _contact.SubmitLeadAsync(model, HttpContext.Connection.RemoteIpAddress?.ToString());
        return Json(new { ok = true, url });
    }
}
