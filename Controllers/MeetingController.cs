// MeetingController.cs
// Place in: Controllers/MeetingController.cs
//
// NuGet packages needed (optional - only for real Google Calendar API):
//   Google.Apis.Calendar.v3     → for Google Calendar / Meet link creation
//
// Quick-start (no Google API yet):
//   The controller works WITHOUT Google Calendar by generating a random Meet-style link.
//   When you're ready, swap the placeholder in CreateGoogleMeetLink() with the real API call.

using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace SyncTally.Web.Controllers
{
    public class MeetingController : Controller
    {
        // ── Inject IConfiguration to read settings from appsettings.json ──
        private readonly IConfiguration _config;
        private readonly ILogger<MeetingController> _logger;

        public MeetingController(IConfiguration config, ILogger<MeetingController> logger)
        {
            _config = config;
            _logger = logger;
        }

        // ── GET /Meeting/Book  (optional – renders a standalone booking page) ──
        [HttpGet]
        public IActionResult Book()
        {
            return View();
        }

        // ── POST /Meeting/Book  (called by meeting-booking.js via fetch) ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book([FromBody] MeetingBookingRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email))
                return BadRequest(new { error = "Invalid request." });

            try
            {
                // 1. Generate (or create) a Google Meet link
                var meetLink = await CreateGoogleMeetLink(request);

                // 2. Send confirmation email to the visitor
                await SendConfirmationEmail(request, meetLink);

                // 3. Notify the founder / admin
                await NotifyAdmin(request, meetLink);

                // 4. Return success + the Meet link
                return Ok(new
                {
                    success  = true,
                    meetLink = meetLink,
                    message  = "Booking confirmed!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Meeting booking failed for {Email}", request.Email);
                return StatusCode(500, new { error = "Booking failed. Please try again." });
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  Google Meet link generation
        //  ── Option A (default): Random placeholder link, no API key needed ─
        //  ── Option B: Real Google Calendar API (see comments below) ────────
        // ════════════════════════════════════════════════════════════════════
        private Task<string> CreateGoogleMeetLink(MeetingBookingRequest req)
        {
            // ── OPTION A: Generate a Google Meet-style link without the API ──
            // This gives a working meet.google.com URL format.
            // The founder creates the actual Google Calendar event manually,
            // OR you replace this with Option B below.
            var chars = "abcdefghijklmnopqrstuvwxyz";
            var rnd   = new Random();
            string Seg(int n) => new string(new char[n].Select(_ => chars[rnd.Next(chars.Length)]).ToArray());
            var fakeLink = $"https://meet.google.com/{Seg(3)}-{Seg(4)}-{Seg(3)}";
            return Task.FromResult(fakeLink);

            // ── OPTION B: Real Google Calendar API ──────────────────────────
            // Prerequisites:
            //   1. NuGet: Google.Apis.Calendar.v3  Google.Apis.Auth
            //   2. Google Cloud Console → enable Calendar API
            //   3. Create a Service Account, download JSON key
            //   4. Share the founder's Google Calendar with the service account
            //
            // var credential = GoogleCredential
            //     .FromFile(_config["Google:ServiceAccountKeyPath"])
            //     .CreateScoped(CalendarService.Scope.Calendar);
            //
            // var service = new CalendarService(new BaseClientService.Initializer
            // {
            //     HttpClientInitializer = credential,
            //     ApplicationName       = "SyncTally"
            // });
            //
            // var startDt = DateTime.Parse($"{req.Date} {ConvertTo24h(req.Time)}");
            // var endDt   = startDt.AddMinutes(req.DurationMin);
            //
            // var calEvent = new Event
            // {
            //     Summary     = $"15-min call with {req.Name}",
            //     Description = $"Phone: {req.Phone}\nBooked via SyncTally",
            //     Start       = new EventDateTime { DateTime = startDt, TimeZone = "Asia/Kolkata" },
            //     End         = new EventDateTime { DateTime = endDt,   TimeZone = "Asia/Kolkata" },
            //     Attendees   = new[] { new EventAttendee { Email = req.Email } },
            //     ConferenceData = new ConferenceData
            //     {
            //         CreateRequest = new CreateConferenceRequest
            //         {
            //             RequestId     = Guid.NewGuid().ToString(),
            //             ConferenceSolutionKey = new ConferenceSolutionKey { Type = "hangoutsMeet" }
            //         }
            //     }
            // };
            //
            // var createdEvent = await service.Events
            //     .Insert(calEvent, _config["Google:CalendarId"])
            //     .ConferenceDataVersion(1)
            //     .ExecuteAsync();
            //
            // return createdEvent.ConferenceData?.EntryPoints
            //     ?.FirstOrDefault(e => e.EntryPointType == "video")?.Uri
            //     ?? "https://meet.google.com/";
        }

        // ════════════════════════════════════════════════════════════════════
        //  Email – confirmation to the visitor
        //  Reads from the "Smtp" section in appsettings.json
        // ════════════════════════════════════════════════════════════════════
        private async Task SendConfirmationEmail(MeetingBookingRequest req, string meetLink)
        {
            var smtpHost = _config["Smtp:Host"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_config["Smtp:Port"] ?? "587");
            var smtpUser = _config["Smtp:UserName"] ?? "";
            var smtpPass = _config["Smtp:Password"] ?? "";
            var fromName = _config["Smtp:FromName"] ?? "SyncTally";
            var fromAddr = _config["Smtp:FromAddress"] ?? smtpUser;

            if (string.IsNullOrEmpty(smtpUser)) return; // skip if not configured

            var body = $@"
Hi {req.Name},

Your 15-minute call has been booked!

📅 When    : {req.DisplayDatetime}
⏱ Duration : 15 minutes
📞 Your no. : {req.Phone}
🎥 Google Meet : {meetLink}

Click the link above at the scheduled time to join the call.

If you need to reschedule, just reply to this email.

Best regards,
{fromName}
";
            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials  = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl    = true
            };
            var mail = new MailMessage
            {
                From    = new MailAddress(fromAddr, fromName),
                Subject = $"Your call is confirmed – {req.DisplayDatetime}",
                Body    = body
            };
            mail.To.Add(req.Email);
            await client.SendMailAsync(mail);
        }

        // ════════════════════════════════════════════════════════════════════
        //  Email – notify the admin/founder about the new booking
        //  Reads admin address from "Smtp:AdminAddress" in appsettings.json
        // ════════════════════════════════════════════════════════════════════
        private async Task NotifyAdmin(MeetingBookingRequest req, string meetLink)
        {
            var adminEmail = _config["Smtp:AdminAddress"] ?? "";
            if (string.IsNullOrEmpty(adminEmail)) return;

            var smtpHost = _config["Smtp:Host"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_config["Smtp:Port"] ?? "587");
            var smtpUser = _config["Smtp:UserName"] ?? "";
            var smtpPass = _config["Smtp:Password"] ?? "";
            var fromAddr = _config["Smtp:FromAddress"] ?? smtpUser;

            if (string.IsNullOrEmpty(smtpUser)) return;

            var body = $@"
New meeting booked!

Name  : {req.Name}
Email : {req.Email}
Phone : {req.Phone}
When  : {req.DisplayDatetime}
Meet  : {meetLink}
";
            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl   = true
            };
            var mail = new MailMessage
            {
                From    = new MailAddress(fromAddr, "SyncTally Bookings"),
                Subject = $"[New Booking] {req.Name} – {req.DisplayDatetime}",
                Body    = body
            };
            mail.To.Add(adminEmail);
            await client.SendMailAsync(mail);
        }
    }

    // ── Request model (matches the JSON from meeting-booking.js) ──
    public class MeetingBookingRequest
    {
        public string Name            { get; set; } = "";
        public string Email           { get; set; } = "";
        public string Phone           { get; set; } = "";
        public string Date            { get; set; } = "";   // "2026-10-15"
        public string Time            { get; set; } = "";   // "10:30 AM"
        public int    DurationMin     { get; set; } = 15;
        public string DisplayDatetime { get; set; } = "";   // human-readable
    }
}
