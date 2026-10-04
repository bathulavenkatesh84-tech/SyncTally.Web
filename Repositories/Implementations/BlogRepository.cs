using System.Data;
using SyncTally.Web.Repositories.Interfaces;
using SyncTally.Web.Services.Interfaces;

namespace SyncTally.Web.Repositories.Implementations;

public class BlogRepository : IBlogRepository
{
    private readonly IFlexidb _db;
    public BlogRepository(IFlexidb db) => _db = db;

    public Task<DataTable> SearchAsync(string? q, string? tag, string sort, int skip, int take)
    {
        // ORDER BY cannot be parameterised, so it comes from a fixed whitelist – never from user text.
        string orderBy = sort switch
        {
            "oldest" => "published_on ASC, post_id ASC",
            "popular" => "view_count DESC, published_on DESC",
            "title" => "title ASC",
            _ => "published_on DESC, post_id DESC"
        };
        return _db.GetdtAsync(SqlHelper.Cmd(
            $@"SELECT slug, title, tag, icon, author, is_featured, published_on, read_minutes, excerpt,
                      COUNT(*) OVER() AS total_count
                 FROM cmr_admin.site_blog_post
                WHERE is_published
                  AND (@q = '' OR title ILIKE '%' || @q || '%' OR excerpt ILIKE '%' || @q || '%')
                  AND (@tag = '' OR tag = @tag)
                ORDER BY {orderBy}
                LIMIT @take OFFSET @skip",
            ("q", (q ?? string.Empty).Trim()), ("tag", (tag ?? string.Empty).Trim()), ("take", take), ("skip", skip)));
    }

    public Task<DataTable> GetTagsAsync() => _db.GetdtAsync(SqlHelper.Cmd(
        "SELECT DISTINCT tag FROM cmr_admin.site_blog_post WHERE is_published ORDER BY tag"));

    public Task<DataTable> GetCategoriesAsync() => _db.GetdtAsync(SqlHelper.Cmd(
        @"SELECT tag, COUNT(*) AS cnt
            FROM cmr_admin.site_blog_post
           WHERE is_published
           GROUP BY tag
           ORDER BY tag"));

    public Task<DataTable> GetFeaturedAsync() => _db.GetdtAsync(SqlHelper.Cmd(
        @"SELECT slug, title, tag, icon, author, is_featured, published_on, read_minutes, excerpt
            FROM cmr_admin.site_blog_post
           WHERE is_published AND is_featured
           ORDER BY published_on DESC
           LIMIT 1"));

    public Task<DataTable> GetRelatedAsync(string tag, string excludeSlug, int take) => _db.GetdtAsync(SqlHelper.Cmd(
        @"SELECT slug, title, tag, icon, author, is_featured, published_on, read_minutes, excerpt
            FROM cmr_admin.site_blog_post
           WHERE is_published AND slug <> @slug
           ORDER BY (tag = @tag) DESC, published_on DESC
           LIMIT @take",
        ("slug", excludeSlug), ("tag", tag), ("take", take)));

    public Task<DataTable> GetBySlugAsync(string slug) => _db.GetdtAsync(SqlHelper.Cmd(
        @"SELECT slug, title, tag, icon, author, author_role, is_featured, published_on, read_minutes, excerpt, body, view_count
            FROM cmr_admin.site_blog_post
           WHERE is_published AND slug = @slug",
        ("slug", slug)));

    public async Task IncrementViewAsync(string slug) => await _db.ExecuteAsync(SqlHelper.Cmd(
        "UPDATE cmr_admin.site_blog_post SET view_count = view_count + 1 WHERE slug = @slug",
        ("slug", slug)));
}
