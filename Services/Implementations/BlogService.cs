using System.Data;
using SyncTally.Web.Repositories.Interfaces;
using SyncTally.Web.Services.Interfaces;
using SyncTally.Web.ViewModels;

namespace SyncTally.Web.Services.Implementations;

public class BlogService : IBlogService
{
    private const int PageSize = 9;
    private static readonly string[] Sorts = { "newest", "oldest", "popular", "title" };
    private readonly IBlogRepository _repo;

    public BlogService(IBlogRepository repo) => _repo = repo;

    public async Task<BlogListViewModel> GetListAsync(string? q, string? tag, string? sort, int page)
    {
        string s = Sorts.Contains(sort ?? "") ? sort! : "newest";
        page = Math.Max(1, page);
        var dt = await _repo.SearchAsync(q, tag, s, (page - 1) * PageSize, PageSize);
        int total = dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["total_count"]) : 0;

        // Page requested is beyond the last page → fall back to the last one.
        if (dt.Rows.Count == 0 && page > 1)
        {
            var probe = await _repo.SearchAsync(q, tag, s, 0, 1);
            total = probe.Rows.Count > 0 ? Convert.ToInt32(probe.Rows[0]["total_count"]) : 0;
            page = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
            dt = await _repo.SearchAsync(q, tag, s, (page - 1) * PageSize, PageSize);
        }

        var cats = (await _repo.GetCategoriesAsync()).Rows.Cast<DataRow>()
            .Select(r => new BlogCategoryViewModel { Name = r.Str("tag"), Count = r.Int("cnt") }).ToList();
        bool filtered = !string.IsNullOrWhiteSpace(q) || !string.IsNullOrWhiteSpace(tag);
        BlogCardViewModel? featured = null;
        if (!filtered && page == 1)
        {
            var f = await _repo.GetFeaturedAsync();
            if (f.Rows.Count > 0) featured = ToCard(f.Rows[0]);
        }
        return new BlogListViewModel
        {
            Q = q,
            Tag = tag,
            Sort = s,
            Tags = cats.Select(c => c.Name).ToList(),
            Categories = cats,
            AllCount = cats.Sum(c => c.Count),
            Featured = featured,
            Result = new PagedResult<BlogCardViewModel>
            {
                Items = dt.Rows.Cast<DataRow>().Select(ToCard).ToList(),
                Page = page,
                PageSize = PageSize,
                Total = total
            }
        };
    }

    public async Task<BlogDetailViewModel?> GetDetailAsync(string slug)
    {
        var dt = await _repo.GetBySlugAsync(slug);
        if (dt.Rows.Count == 0) return null;
        await _repo.IncrementViewAsync(slug);
        var r = dt.Rows[0];
        return new BlogDetailViewModel
        {
            Post = ToCard(r),
            AuthorRole = r.Str("author_role"),
            Related = (await _repo.GetRelatedAsync(r.Str("tag"), slug, 3)).Rows.Cast<DataRow>().Select(ToCard).ToList(),
            Body = r.Arr("body").ToList(),
            Views = r.Int("view_count") + 1
        };
    }

    private static BlogCardViewModel ToCard(DataRow r) => new()
    {
        Slug = r.Str("slug"),
        Title = r.Str("title"),
        Tag = r.Str("tag"),
        PublishedOn = r.Date("published_on"),
        ReadMinutes = r.Int("read_minutes"),
        Excerpt = r.Str("excerpt"),
        Icon = r.Str("icon"),
        Author = r.Str("author"),
        IsFeatured = r.Bool("is_featured")
    };
}
