using System.Data;

namespace SyncTally.Web.Repositories.Interfaces;

public interface IFeatureRepository
{
    Task<DataTable> GetMenuAsync();
    Task<DataTable> SearchAsync(string? q, string? status);
    Task<DataTable> GetBySlugAsync(string slug);
    Task<DataTable> GetHomeHighlightsAsync();
    Task<DataTable> GetRelatedAsync(string groupTitle, string excludeSlug);
}

public interface IBlogRepository
{
    /// <summary>Filtered, sorted, paged in SQL (LIMIT/OFFSET). Each row carries total_count.</summary>
    Task<DataTable> SearchAsync(string? q, string? tag, string sort, int skip, int take);
    Task<DataTable> GetTagsAsync();
    /// <summary>Category name + published article count, for the filter chips.</summary>
    Task<DataTable> GetCategoriesAsync();
    Task<DataTable> GetFeaturedAsync();
    Task<DataTable> GetRelatedAsync(string tag, string excludeSlug, int take);
    Task<DataTable> GetBySlugAsync(string slug);
    Task IncrementViewAsync(string slug);
}

public interface IPartnerRepository
{
    Task<DataTable> SearchAsync(string? q, string? kind, string sort, int skip, int take);
    Task<DataTable> GetKindsAsync();
}

public interface IPlanRepository
{
    Task<DataTable> GetAllAsync();
}

public interface ISiteContentRepository
{
    Task<DataTable> GetJourneyAsync();
    Task<DataTable> GetFaqsAsync();
    Task<DataTable> GetSettingsAsync();
    Task<string?> GetSettingAsync(string key);
}

public interface IContactRepository
{
    /// <summary>Saves the message (topic + created_at timestamp) and returns its id and the saved time.</summary>
    Task<(long Id, DateTime CreatedAt)> SaveContactAsync(string name, string email, string topic, string message, string? ip);
    Task<long> SaveLeadAsync(string name, string phone, string topic, string? message, string? ip);
    /// <summary>Saves the partner application and returns its id and the saved time (created_at).</summary>
    Task<(long Id, DateTime CreatedAt)> SaveApplicationAsync(string name, string email, string phone, string city);
}

public interface IErrorLogRepository
{
    Task InsertAsync(string level, string? source, string message, string? stackTrace, string? requestPath, string? userName);
}

public enum ActivationStatus { Activated, EmailAlreadyInApp, AdminRoleMissing }

public interface ITrialRepository
{
    Task<DataTable> GetByEmailAsync(string email);
    Task<DataTable> GetByRefAsync(Guid signupRef);

    /// <summary>
    /// Creates the pending sign-up (or refreshes an existing PENDING one) and stores the OTP hash, in one
    /// transaction together with a site_activity row. Returns null when the e-mail already has an ACTIVE account.
    /// </summary>
    Task<(long Id, Guid Ref)?> UpsertPendingAsync(string fullName, string company, string email, string phone, string city,
        int salesmen, string? plan, string otpHash, int otpMinutes, string? ip);

    Task SetOtpAsync(long signupId, string otpHash, int otpMinutes);
    Task IncrementAttemptsAsync(long signupId);

    /// <summary>True when the e-mail already belongs to a user (user_management) or a subscription of the application.</summary>
    Task<bool> IsEmailInAppAsync(string email);

    /// <summary>
    /// One transaction: marks the sign-up active (password hash + trial window) AND provisions the application side –
    /// tally_sync_subscription (trial dates, 1 company), tally_sync_subscription_dtl (the company typed on the form),
    /// user_management (Admin role, status Active) and user_companies (user linked to that company).
    /// </summary>
    Task<ActivationStatus> ActivateAsync(long signupId, string passwordHash, string appPasswordHash, int trialDays);
}
