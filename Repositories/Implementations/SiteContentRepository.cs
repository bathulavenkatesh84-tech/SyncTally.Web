using System.Data;
using SyncTally.Web.Repositories.Interfaces;
using SyncTally.Web.Services.Interfaces;

namespace SyncTally.Web.Repositories.Implementations;

public class SiteContentRepository : ISiteContentRepository
{
    private readonly IFlexidb _db;
    public SiteContentRepository(IFlexidb db) => _db = db;

    public Task<DataTable> GetJourneyAsync() => _db.GetdtAsync(SqlHelper.Cmd(
        "SELECT icon, title, description FROM cmr_admin.site_journey_step ORDER BY sort_order"));

    public Task<DataTable> GetFaqsAsync() => _db.GetdtAsync(SqlHelper.Cmd(
        "SELECT question, answer FROM cmr_admin.site_faq WHERE is_active ORDER BY sort_order"));

    public async Task<string?> GetSettingAsync(string key)
    {
        var o = await _db.GetScalarAsync(SqlHelper.Cmd(
            "SELECT setting_value FROM cmr_admin.site_setting WHERE setting_key = @k", ("k", key)));
        return o == null || o == DBNull.Value ? null : Convert.ToString(o);
    }

    public Task<DataTable> GetSettingsAsync() => _db.GetdtAsync(SqlHelper.Cmd(
        "SELECT setting_key, setting_value FROM cmr_admin.site_setting"));
}
