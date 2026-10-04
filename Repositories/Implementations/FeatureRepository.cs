using System.Data;
using SyncTally.Web.Repositories.Interfaces;
using SyncTally.Web.Services.Interfaces;

namespace SyncTally.Web.Repositories.Implementations;

public class FeatureRepository : IFeatureRepository
{
    private readonly IFlexidb _db;
    public FeatureRepository(IFlexidb db) => _db = db;

    private const string Cols = "slug, title, icon, live_status, lead, group_title";

    public Task<DataTable> GetMenuAsync() => _db.GetdtAsync(SqlHelper.Cmd(
        @"SELECT slug, title, menu_column, group_title, group_order, sort_order
            FROM cmr_admin.site_feature
           WHERE is_active
           ORDER BY menu_column, group_order, sort_order"));

    public Task<DataTable> SearchAsync(string? q, string? status) => _db.GetdtAsync(SqlHelper.Cmd(
        $@"SELECT {Cols}, menu_column, group_order, sort_order
             FROM cmr_admin.site_feature
            WHERE is_active
              AND (@q = '' OR title ILIKE '%' || @q || '%' OR lead ILIKE '%' || @q || '%')
              AND (@status = '' OR live_status = @status)
            ORDER BY menu_column, group_order, sort_order",
        ("q", (q ?? string.Empty).Trim()), ("status", (status ?? string.Empty).Trim())));

    public Task<DataTable> GetBySlugAsync(string slug) => _db.GetdtAsync(SqlHelper.Cmd(
        $@"SELECT {Cols}, points, steps, has_app, app_label, problem, solution, benefits, stats, faqs
             FROM cmr_admin.site_feature
            WHERE is_active AND slug = @slug",
        ("slug", slug)));

    public Task<DataTable> GetHomeHighlightsAsync() => _db.GetdtAsync(SqlHelper.Cmd(
        $@"SELECT {Cols}
             FROM cmr_admin.site_feature
            WHERE is_active AND is_home_highlight
            ORDER BY menu_column, group_order, sort_order
            LIMIT 6"));

    public Task<DataTable> GetRelatedAsync(string groupTitle, string excludeSlug) => _db.GetdtAsync(SqlHelper.Cmd(
        $@"SELECT {Cols}
             FROM cmr_admin.site_feature
            WHERE is_active AND group_title = @g AND slug <> @slug
            ORDER BY sort_order",
        ("g", groupTitle), ("slug", excludeSlug)));
}
