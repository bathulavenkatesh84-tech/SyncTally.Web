using System.Data;
using System.Globalization;
using System.Net;
using SyncTally.Web.Repositories.Interfaces;
using SyncTally.Web.Services.Interfaces;
using SyncTally.Web.ViewModels;

namespace SyncTally.Web.Services.Implementations;

public class PartnerService : IPartnerService
{
    private const int PageSize = 6;
    private static readonly string[] Sorts = { "name", "city", "kind" };
    private readonly IPartnerRepository _partners;
    private readonly IContactRepository _contacts;
    private readonly ISiteContentRepository _settings;
    private readonly IEmailService _email;
    private readonly IErrorLogService _errors;

    public PartnerService(IPartnerRepository partners, IContactRepository contacts, ISiteContentRepository settings,
        IEmailService email, IErrorLogService errors)
    {
        _partners = partners;
        _contacts = contacts;
        _settings = settings;
        _email = email;
        _errors = errors;
    }

    public async Task<PartnerListViewModel> GetListAsync(string? q, string? kind, string? sort, int page)
    {
        string s = Sorts.Contains(sort ?? "") ? sort! : "name";
        page = Math.Max(1, page);
        var dt = await _partners.SearchAsync(q, kind, s, (page - 1) * PageSize, PageSize);
        int total = dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["total_count"]) : 0;
        if (dt.Rows.Count == 0 && page > 1)
        {
            var probe = await _partners.SearchAsync(q, kind, s, 0, 1);
            total = probe.Rows.Count > 0 ? Convert.ToInt32(probe.Rows[0]["total_count"]) : 0;
            page = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
            dt = await _partners.SearchAsync(q, kind, s, (page - 1) * PageSize, PageSize);
        }
        var kinds = await _partners.GetKindsAsync();
        return new PartnerListViewModel
        {
            Q = q,
            Kind = kind,
            Sort = s,
            Kinds = kinds.Rows.Cast<DataRow>().Select(r => r.Str("kind")).ToList(),
            Result = new PagedResult<PartnerViewModel>
            {
                Items = dt.Rows.Cast<DataRow>().Select(r => new PartnerViewModel
                {
                    Name = r.Str("name"),
                    Kind = r.Str("kind"),
                    City = r.Str("city"),
                    Blurb = r.Str("blurb")
                }).ToList(),
                Page = page,
                PageSize = PageSize,
                Total = total
            }
        };
    }

    public async Task SubmitApplicationAsync(PartnerApplyViewModel m)
    {
        string name = m.Name.Trim(), email = m.Email.Trim(), phone = m.Phone.Trim(), city = m.City.Trim();

        // 1. save first (id + created_at time stored in site_partner_application)
        var saved = await _contacts.SaveApplicationAsync(name, email, phone, city);

        // 2. notify site_setting.support_email – a mail failure never breaks the visitor's submit
        try
        {
            string? support = (await _settings.GetSettingAsync("support_email"))?.Trim();
            if (string.IsNullOrEmpty(support))
            {
                await _errors.LogAsync("Warning", nameof(PartnerService), "support_email is empty in site_setting – partner application not e-mailed.");
                return;
            }

            var ist = TimeZoneInfo.ConvertTimeFromUtc(saved.CreatedAt.ToUniversalTime(), IstZone());
            string when = ist.ToString("dd MMM yyyy, hh:mm tt", CultureInfo.InvariantCulture) + " IST";
            string H(string s) => WebUtility.HtmlEncode(s);

            string body =
                "<div style=\"font-family:Segoe UI,Arial,sans-serif;max-width:560px;margin:auto;padding:24px;border:1px solid #e3e8f2;border-radius:12px\">" +
                "<h2 style=\"margin-top:0;color:#0f1f3d\">New partner application</h2>" +
                "<table style=\"border-collapse:collapse;width:100%\">" +
                $"<tr><td style=\"padding:6px 0;color:#6b7a99;width:90px\">Name / firm</td><td><b>{H(name)}</b></td></tr>" +
                $"<tr><td style=\"padding:6px 0;color:#6b7a99\">Email</td><td><a href=\"mailto:{H(email)}\">{H(email)}</a></td></tr>" +
                $"<tr><td style=\"padding:6px 0;color:#6b7a99\">Mobile</td><td>{H(phone)}</td></tr>" +
                $"<tr><td style=\"padding:6px 0;color:#6b7a99\">City</td><td>{H(city)}</td></tr>" +
                $"<tr><td style=\"padding:6px 0;color:#6b7a99\">Received</td><td>{when}</td></tr>" +
                $"<tr><td style=\"padding:6px 0;color:#6b7a99\">Reference</td><td>#{saved.Id}</td></tr>" +
                "</table>" +
                "<p style=\"color:#6b7a99;font-size:12px;margin-top:20px\">Reply to this email to contact the applicant directly.</p></div>";

            await _email.SendAsync(support, $"[SyncTally] Partner application – {name}, {city} (#{saved.Id})", body, replyTo: email);
        }
        catch (Exception ex)
        {
            await _errors.LogAsync("Error", nameof(PartnerService), $"Partner application #{saved.Id} saved but support e-mail failed: {ex.Message}");
        }
    }

    private static TimeZoneInfo IstZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }
    }
}
