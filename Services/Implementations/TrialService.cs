using System.Data;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using SyncTally.Web.Models;
using SyncTally.Web.Repositories.Interfaces;
using SyncTally.Web.Services;
using SyncTally.Web.Services.Interfaces;
using SyncTally.Web.ViewModels;

namespace SyncTally.Web.Services.Implementations;

public class TrialService : ITrialService
{
    private readonly ITrialRepository _repo;
    private readonly IEmailService _email;
    private readonly IPasswordService _passwords;
    private readonly IAuthService _auth;
    private readonly IErrorLogService _errors;
    private readonly TrialSettings _cfg;

    public TrialService(ITrialRepository repo, IEmailService email, IPasswordService passwords, IAuthService auth,
        IErrorLogService errors, IOptions<TrialSettings> cfg)
    {
        _repo = repo;
        _email = email;
        _passwords = passwords;
        _auth = auth;
        _errors = errors;
        _cfg = cfg.Value;
    }

    // ---------------------------------------------------------------- start
    public async Task<TrialResult> StartAsync(TrialStartViewModel m, string? ip)
    {
        string email = m.Email.Trim().ToLowerInvariant();

        // Already a user of the application? Say so now – before any OTP is sent.
        if (await _repo.IsEmailInAppAsync(email))
            return TrialResult.EmailTaken();

        // Same e-mail posted again within the resend cooldown → don't send another code, just continue.
        var existing = await _repo.GetByEmailAsync(email);
        if (existing.Rows.Count > 0)
        {
            var r = existing.Rows[0];
            if (r.Str("status") == "active")
                return TrialResult.EmailTaken();
            if (r.Int("seconds_since_sent") < _cfg.ResendSeconds)
                return TrialResult.Success(Guid.Parse(r.Str("signup_ref")), email,
                    "We already sent you a code a moment ago. Check your inbox (and spam folder).");
            if (r.Int("otp_sent_count") >= _cfg.MaxOtpSends)
                return TrialResult.Fail("Too many codes requested for this email. Please contact support.", nameof(TrialStartViewModel.Email));
        }

        string otp = NewOtp();
        var saved = await _repo.UpsertPendingAsync(m.FullName.Trim(), m.CompanyName.Trim(), email, m.Phone.Trim(), m.City.Trim(),
            m.SalesmenCount, string.IsNullOrWhiteSpace(m.Plan) ? null : m.Plan.Trim(), HashOtp(email, otp), _cfg.OtpMinutes, ip);
        if (saved == null)
            return TrialResult.EmailTaken();

        if (!await TrySendOtpAsync(email, m.FullName.Trim(), otp))
            return TrialResult.Fail("We could not send the email right now. Please try again in a minute.", nameof(TrialStartViewModel.Email));

        return TrialResult.Success(saved.Value.Ref, email);
    }

    // --------------------------------------------------------------- verify page
    public async Task<TrialVerifyViewModel?> GetVerifyModelAsync(Guid signupRef)
    {
        var dt = await _repo.GetByRefAsync(signupRef);
        if (dt.Rows.Count == 0) return null;
        var r = dt.Rows[0];
        return new TrialVerifyViewModel
        {
            Ref = signupRef,
            MaskedEmail = Mask(r.Str("email")),
            ResendInSeconds = Math.Max(0, _cfg.ResendSeconds - r.Int("seconds_since_sent"))
        };
    }

    // --------------------------------------------------------------- verify
    public async Task<TrialResult> VerifyAsync(TrialVerifyViewModel m)
    {
        var dt = await _repo.GetByRefAsync(m.Ref);
        if (dt.Rows.Count == 0)
            return TrialResult.Fail("This sign-up link is not valid. Please start again.");
        var r = dt.Rows[0];
        string email = r.Str("email");
        long id = Convert.ToInt64(r["signup_id"], CultureInfo.InvariantCulture);

        if (r.Str("status") == "active")
            return new TrialResult { Ok = true, AlreadyActive = true, Reference = m.Ref, Email = email };

        if (r.Int("otp_attempts") >= _cfg.MaxOtpAttempts)
            return TrialResult.Fail("Too many wrong attempts. Please request a new code.", nameof(TrialVerifyViewModel.Otp));
        if (!r.Bool("otp_valid") || r.Str("otp_hash").Length == 0)
            return TrialResult.Fail("This code has expired. Please request a new one.", nameof(TrialVerifyViewModel.Otp));

        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(HashOtp(email.ToLowerInvariant(), m.Otp.Trim())),
                                                    Encoding.UTF8.GetBytes(r.Str("otp_hash"))))
        {
            await _repo.IncrementAttemptsAsync(id);
            int left = Math.Max(0, _cfg.MaxOtpAttempts - r.Int("otp_attempts") - 1);
            return TrialResult.Fail(left > 0 ? $"That code is not correct. {left} attempt{(left == 1 ? "" : "s")} left."
                                              : "Too many wrong attempts. Please request a new code.", nameof(TrialVerifyViewModel.Otp));
        }

        var status = await _repo.ActivateAsync(id, _passwords.Hash(m.Password), _passwords.HashForApp(m.Password), _cfg.Days);
        if (status == ActivationStatus.EmailAlreadyInApp)
            return TrialResult.EmailTaken();
        if (status == ActivationStatus.AdminRoleMissing)
        {
            await _errors.LogAsync("Error", nameof(TrialService), "No Admin role found in tally_roles – trial user not created.");
            return TrialResult.Fail("We could not set up your account right now. Our team has been notified – please try again shortly.");
        }

        // Welcome mail is a courtesy – never fail the sign-up because of it.
        try
        {
            string end = DateTime.UtcNow.AddDays(_cfg.Days).ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
            await _email.SendAsync(email, "Your SyncTally free trial has started",
                EmailTemplate($"Welcome to SyncTally, {WebUtility.HtmlEncode(r.Str("full_name"))}!",
                    $"<p>Your <b>{_cfg.Days}-day free trial</b> is active until <b>{end}</b>.</p>" +
                    $"<p>Sign in at <a href=\"{WebUtility.HtmlEncode(_auth.LoginUrl)}\">{WebUtility.HtmlEncode(_auth.LoginUrl)}</a> with your email <b>{WebUtility.HtmlEncode(email)}</b> and the password you just set.</p>"));
        }
        catch (Exception ex)
        {
            await _errors.LogAsync("Warning", nameof(TrialService), "Welcome email failed: " + ex.Message);
        }

        return TrialResult.Success(m.Ref, email);
    }

    // --------------------------------------------------------------- resend
    public async Task<TrialResult> ResendAsync(Guid signupRef)
    {
        var dt = await _repo.GetByRefAsync(signupRef);
        if (dt.Rows.Count == 0) return TrialResult.Fail("This sign-up link is not valid. Please start again.");
        var r = dt.Rows[0];
        if (r.Str("status") == "active") return new TrialResult { Ok = true, AlreadyActive = true, Reference = signupRef, Email = r.Str("email") };

        int wait = _cfg.ResendSeconds - r.Int("seconds_since_sent");
        if (wait > 0) return TrialResult.Fail($"Please wait {wait} seconds before requesting another code.");
        if (r.Int("otp_sent_count") >= _cfg.MaxOtpSends)
            return TrialResult.Fail("Too many codes requested. Please contact support.");

        string otp = NewOtp();
        await _repo.SetOtpAsync(Convert.ToInt64(r["signup_id"], CultureInfo.InvariantCulture), HashOtp(r.Str("email").ToLowerInvariant(), otp), _cfg.OtpMinutes);
        if (!await TrySendOtpAsync(r.Str("email"), r.Str("full_name"), otp))
            return TrialResult.Fail("We could not send the email right now. Please try again in a minute.");
        return TrialResult.Success(signupRef, r.Str("email"), "A new code is on its way.");
    }

    // --------------------------------------------------------------- welcome
    public async Task<TrialWelcomeViewModel?> GetWelcomeAsync(Guid signupRef)
    {
        var dt = await _repo.GetByRefAsync(signupRef);
        if (dt.Rows.Count == 0 || dt.Rows[0].Str("status") != "active") return null;
        var r = dt.Rows[0];
        return new TrialWelcomeViewModel
        {
            Ref = signupRef,
            Email = r.Str("email"),
            FullName = r.Str("full_name"),
            CompanyName = r.Str("company_name"),
            TrialEnd = r.Date("trial_end").ToUniversalTime(),
            Days = _cfg.Days,
            LoginUrl = _auth.BuildLoginUrl(r.Str("email"))
        };
    }

    // --------------------------------------------------------------- helpers
    private async Task<bool> TrySendOtpAsync(string to, string name, string otp)
    {
        try
        {
            await _email.SendAsync(to, $"{otp} is your SyncTally verification code",
                EmailTemplate($"Hi {WebUtility.HtmlEncode(name)}, verify your email",
                    $"<p>Use this code to finish starting your free trial:</p>" +
                    $"<p style=\"font-size:32px;letter-spacing:8px;font-weight:700;margin:16px 0\">{otp}</p>" +
                    $"<p>It is valid for {_cfg.OtpMinutes} minutes. If you did not ask for it, you can ignore this email.</p>"));
            return true;
        }
        catch (Exception ex)
        {
            await _errors.LogAsync("Error", nameof(TrialService), "OTP email failed: " + ex);
            return false;
        }
    }

    private static string NewOtp() => RandomNumberGenerator.GetInt32(100000, 1000000).ToString(CultureInfo.InvariantCulture);

    private static string HashOtp(string emailSalt, string otp)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(emailSalt + ":" + otp));
        return Convert.ToHexString(bytes);
    }

    private static string Mask(string email)
    {
        int at = email.IndexOf('@');
        if (at < 2) return email;
        return email[0] + new string('*', Math.Min(at - 1, 6)) + email[at..];
    }

    private static string EmailTemplate(string heading, string bodyHtml) =>
        "<div style=\"font-family:Segoe UI,Arial,sans-serif;max-width:520px;margin:auto;padding:24px;border:1px solid #e3e8f2;border-radius:12px\">" +
        "<h2 style=\"margin-top:0;color:#0f1f3d\">SyncTally</h2>" +
        $"<h3 style=\"color:#0f1f3d\">{heading}</h3>{bodyHtml}" +
        "<p style=\"color:#6b7a99;font-size:12px;margin-top:24px\">SyncTally – order taking and billing for Indian distributors, synced with Tally.</p></div>";
}
