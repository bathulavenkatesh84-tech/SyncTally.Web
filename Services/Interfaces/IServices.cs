using SyncTally.Web.Models;
using SyncTally.Web.ViewModels;

namespace SyncTally.Web.Services.Interfaces;

public interface IFeatureService
{
    Task<NavMenuViewModel> GetMenuAsync(string activeController);
    Task<FeatureListViewModel> GetListAsync(string? q, string? status);
    Task<FeatureDetailViewModel?> GetDetailAsync(string slug);
}

public interface IBlogService
{
    Task<BlogListViewModel> GetListAsync(string? q, string? tag, string? sort, int page);
    Task<BlogDetailViewModel?> GetDetailAsync(string slug);
}

public interface IPartnerService
{
    Task<PartnerListViewModel> GetListAsync(string? q, string? kind, string? sort, int page);
    Task SubmitApplicationAsync(PartnerApplyViewModel model);
}

public interface IPricingService
{
    Task<List<PlanViewModel>> GetPlansAsync();
}

public interface ISiteContentService
{
    Task<HomeViewModel> GetHomeAsync();
    Task<ContactPageViewModel> GetContactPageAsync(ContactViewModel? form = null);
    Task<string> BuildWhatsAppUrlAsync(string text);
}

public interface IContactService
{
    Task SubmitContactAsync(ContactViewModel model, string? ip);
    /// <summary>Stores the lead and returns the wa.me click-to-chat link.</summary>
    Task<string> SubmitLeadAsync(ChatLeadViewModel model, string? ip);
}

public interface IAuthService
{
    string LoginUrl { get; }
    /// <summary>Login URL of the application with the user name (e-mail) pre-filled as ?username=…</summary>
    string BuildLoginUrl(string username);
}

public interface IPasswordService
{
    /// <summary>PBKDF2$iterations$salt$hash (HMAC-SHA256) – verifiable by any app that knows the format.</summary>
    string Hash(string password);
    bool Verify(string password, string stored);
    /// <summary>BCrypt hash – the format tallywebapp's Login uses (BCrypt.Net.BCrypt.Verify) for user_management.password_hash.</summary>
    string HashForApp(string password);
}

public interface IEmailService
{
    /// <summary>replyTo: optional address replies should go to (e.g. the visitor who filled in the contact form).</summary>
    Task SendAsync(string to, string subject, string htmlBody, string? replyTo = null);
}

public interface ITrialService
{
    Task<TrialResult> StartAsync(TrialStartViewModel model, string? ip);
    Task<TrialVerifyViewModel?> GetVerifyModelAsync(Guid signupRef);
    Task<TrialResult> VerifyAsync(TrialVerifyViewModel model);
    Task<TrialResult> ResendAsync(Guid signupRef);
    Task<TrialWelcomeViewModel?> GetWelcomeAsync(Guid signupRef);
}

public interface IErrorLogService
{
    Task LogAsync(Exception ex, string? requestPath, string? userName);
    Task LogAsync(string level, string source, string message, string? requestPath = null, string? userName = null);
}
