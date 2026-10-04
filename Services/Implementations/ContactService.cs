using System.Globalization;
using System.Net;
using SyncTally.Web.Repositories.Interfaces;
using SyncTally.Web.Services.Interfaces;
using SyncTally.Web.ViewModels;

namespace SyncTally.Web.Services.Implementations;

public class ContactService : IContactService
{
    private readonly IContactRepository _repo;
    private readonly ISiteContentService _content;
    private readonly ISiteContentRepository _settings;
    private readonly IEmailService _email;
    private readonly IErrorLogService _errors;

    public ContactService(IContactRepository repo, ISiteContentService content, ISiteContentRepository settings,
        IEmailService email, IErrorLogService errors)
    {
        _repo = repo;
        _content = content;
        _settings = settings;
        _email = email;
        _errors = errors;
    }

    public async Task SubmitContactAsync(ContactViewModel m, string? ip)
    {
        string name = m.Name.Trim(), email = m.Email.Trim(), message = m.Message.Trim();

        // 1. save first – the enquiry must never be lost, even if mail is down
        var saved = await _repo.SaveContactAsync(name, email, m.Topic, message, ip);

        // 2. notify the support mailbox from site_setting.support_email (failure never breaks the visitor's submit)
        try
        {
            string? support = (await _settings.GetSettingAsync("support_email"))?.Trim();
            if (string.IsNullOrEmpty(support))
            {
                await _errors.LogAsync("Warning", nameof(ContactService), "support_email is empty in site_setting – contact enquiry not e-mailed.");
                return;
            }

            var ist = TimeZoneInfo.ConvertTimeFromUtc(saved.CreatedAt.ToUniversalTime(), IstZone());
            string when = ist.ToString("dd MMM yyyy, hh:mm tt", CultureInfo.InvariantCulture) + " IST";
            string H(string s) => WebUtility.HtmlEncode(s);

            string body =
                "<div style=\"font-family:Segoe UI,Arial,sans-serif;max-width:560px;margin:auto;padding:24px;border:1px solid #e3e8f2;border-radius:12px\">" +
                "<h2 style=\"margin-top:0;color:#0f1f3d\">New website enquiry</h2>" +
                "<table style=\"border-collapse:collapse;width:100%\">" +
                $"<tr><td style=\"padding:6px 0;color:#6b7a99;width:90px\">Topic</td><td><b>{H(m.Topic)}</b></td></tr>" +
                $"<tr><td style=\"padding:6px 0;color:#6b7a99\">Name</td><td>{H(name)}</td></tr>" +
                $"<tr><td style=\"padding:6px 0;color:#6b7a99\">Email</td><td><a href=\"mailto:{H(email)}\">{H(email)}</a></td></tr>" +
                $"<tr><td style=\"padding:6px 0;color:#6b7a99\">Received</td><td>{when}</td></tr>" +
                $"<tr><td style=\"padding:6px 0;color:#6b7a99\">Reference</td><td>#{saved.Id}</td></tr>" +
                "</table>" +
                $"<p style=\"margin:16px 0 4px;color:#6b7a99\">Message</p><div style=\"padding:12px;background:#f5f7fb;border-radius:8px\">{H(message).Replace("\n", "<br>")}</div>" +
                "<p style=\"color:#6b7a99;font-size:12px;margin-top:20px\">Reply to this email to answer the visitor directly.</p></div>";

            await _email.SendAsync(support, $"[SyncTally] {m.Topic} – {name} (#{saved.Id})", body, replyTo: email);
        }
        catch (Exception ex)
        {
            await _errors.LogAsync("Error", nameof(ContactService), $"Contact #{saved.Id} saved but support e-mail failed: {ex.Message}");
        }
    }

    public async Task<string> SubmitLeadAsync(ChatLeadViewModel m, string? ip)
    {
        await _repo.SaveLeadAsync(m.Name.Trim(), m.Phone.Trim(), m.Topic, m.Message?.Trim(), ip);
        string text = $"Hi SyncTally, I'm {m.Name.Trim()}. I'd like to: {m.Topic}." +
                      (string.IsNullOrWhiteSpace(m.Message) ? string.Empty : $" {m.Message.Trim()}") +
                      $" My number: {m.Phone.Trim()}";
        return await _content.BuildWhatsAppUrlAsync(text);
    }

    private static TimeZoneInfo IstZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); }      // Windows
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }  // Linux
    }
}
