using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using SyncTally.Web.Models;
using SyncTally.Web.Services.Interfaces;

namespace SyncTally.Web.Services.Implementations;

/// <summary>
/// Sends mail through the SMTP server in appsettings → Smtp. When Smtp:Host is empty (local development)
/// nothing is sent; the message is written to the application log so the OTP can be read from the console.
///
/// Host must match the mailbox: Gmail account / app password → smtp.gmail.com:587;
/// Microsoft 365 / Outlook mailbox → smtp.office365.com:587 (and SMTP AUTH must be enabled for that mailbox).
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly SmtpSettings _smtp;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<SmtpSettings> options, ILogger<SmtpEmailService> logger)
    {
        _smtp = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, string? replyTo = null)
    {
        if (string.IsNullOrWhiteSpace(_smtp.Host))
        {
            _logger.LogWarning("[DEV EMAIL – Smtp:Host is empty] To: {To} | Subject: {Subject} | Body: {Body}",
                to, subject, System.Text.RegularExpressions.Regex.Replace(htmlBody, "<[^>]+>", " ").Replace("&nbsp;", " "));
            return;
        }

        // Gmail/Office365 reject a From address that differs from the authenticated mailbox.
        string from = string.IsNullOrWhiteSpace(_smtp.UserName) ? _smtp.FromAddress : _smtp.UserName;
        if (!string.Equals(from, _smtp.FromAddress, StringComparison.OrdinalIgnoreCase))
            _logger.LogWarning("Smtp:FromAddress ({From}) differs from Smtp:UserName ({User}); sending as the user name.", _smtp.FromAddress, _smtp.UserName);

        using var message = new MailMessage
        {
            From = new MailAddress(from, _smtp.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(to);
        if (!string.IsNullOrWhiteSpace(replyTo)) message.ReplyToList.Add(new MailAddress(replyTo));

        using var client = new SmtpClient(_smtp.Host, _smtp.Port)
        {
            EnableSsl = _smtp.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,           // must be set BEFORE Credentials
            Timeout = 20_000
        };
        if (!string.IsNullOrWhiteSpace(_smtp.UserName))
            client.Credentials = new NetworkCredential(_smtp.UserName, _smtp.Password.Replace(" ", ""));
        await client.SendMailAsync(message);
    }
}
