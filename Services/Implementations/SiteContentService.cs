using System.Data;
using Microsoft.Extensions.Caching.Memory;
using SyncTally.Web.Repositories.Interfaces;
using SyncTally.Web.Services.Interfaces;
using SyncTally.Web.ViewModels;

namespace SyncTally.Web.Services.Implementations;

public class SiteContentService : ISiteContentService
{
    private const string SettingsKey = "synctally.settings";
    private readonly ISiteContentRepository _content;
    private readonly IFeatureRepository _features;
    private readonly IMemoryCache _cache;

    public SiteContentService(ISiteContentRepository content, IFeatureRepository features, IMemoryCache cache)
    {
        _content = content;
        _features = features;
        _cache = cache;
    }

    private async Task<Dictionary<string, string>> GetSettingsAsync() =>
        await _cache.GetOrCreateAsync(SettingsKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            var dt = await _content.GetSettingsAsync();
            return dt.Rows.Cast<DataRow>().ToDictionary(r => r.Str("setting_key"), r => r.Str("setting_value"));
        }) ?? new Dictionary<string, string>();

    private static string Get(Dictionary<string, string> s, string key) => s.TryGetValue(key, out var v) ? v : string.Empty;

    public async Task<HomeViewModel> GetHomeAsync()
    {
        var settings = await GetSettingsAsync();
        var journey = await _content.GetJourneyAsync();
        var faqs = await _content.GetFaqsAsync();
        var highlights = await _features.GetHomeHighlightsAsync();
        return new HomeViewModel
        {
            Journey = journey.Rows.Cast<DataRow>().Select(r => new JourneyStepViewModel
            {
                Icon = r.Str("icon"), Title = r.Str("title"), Description = r.Str("description")
            }).ToList(),
            Faqs = faqs.Rows.Cast<DataRow>().Select(r => new FaqViewModel
            {
                Question = r.Str("question"), Answer = r.Str("answer")
            }).ToList(),
            Highlights = highlights.Rows.Cast<DataRow>().Select(FeatureService.ToCard).ToList(),
            TestimonialQuote = Get(settings, "testimonial_quote"),
            TestimonialAuthor = Get(settings, "testimonial_author")
        };
    }

    public async Task<ContactPageViewModel> GetContactPageAsync(ContactViewModel? form = null)
    {
        var s = await GetSettingsAsync();
        return new ContactPageViewModel
        {
            Form = form ?? new ContactViewModel(),
            SupportEmail = Get(s, "support_email"),
            SupportPhone = Get(s, "support_phone"),
            SupportHours = Get(s, "support_hours")
        };
    }

    public async Task<string> BuildWhatsAppUrlAsync(string text)
    {
        var s = await GetSettingsAsync();
        return $"https://wa.me/{Get(s, "wa_number")}?text={Uri.EscapeDataString(text)}";
    }
}
