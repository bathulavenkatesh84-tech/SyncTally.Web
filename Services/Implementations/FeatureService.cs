using System.Data;
using Microsoft.Extensions.Caching.Memory;
using SyncTally.Web.Repositories.Interfaces;
using SyncTally.Web.Services.Interfaces;
using SyncTally.Web.ViewModels;

namespace SyncTally.Web.Services.Implementations;

public class FeatureService : IFeatureService
{
    private const string MenuCacheKey = "synctally.menu";
    private readonly IFeatureRepository _repo;
    private readonly IMemoryCache _cache;

    public FeatureService(IFeatureRepository repo, IMemoryCache cache)
    {
        _repo = repo;
        _cache = cache;
    }

    public async Task<NavMenuViewModel> GetMenuAsync(string activeController)
    {
        var columns = await _cache.GetOrCreateAsync(MenuCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            var dt = await _repo.GetMenuAsync();
            var cols = new List<List<MenuGroup>> { new(), new(), new(), new() };
            foreach (DataRow r in dt.Rows)
            {
                int c = Math.Clamp(r.Int("menu_column"), 1, 4) - 1;
                string group = r.Str("group_title");
                var g = cols[c].FirstOrDefault(x => x.Title == group);
                if (g == null)
                {
                    g = new MenuGroup { Title = group };
                    cols[c].Add(g);
                }
                g.Items.Add(new MenuItem { Slug = r.Str("slug"), Title = r.Str("title") });
            }
            return cols;
        }) ?? new List<List<MenuGroup>>();

        return new NavMenuViewModel { Columns = columns, ActiveController = activeController };
    }

    public async Task<FeatureListViewModel> GetListAsync(string? q, string? status)
    {
        var dt = await _repo.SearchAsync(q, status);
        var vm = new FeatureListViewModel { Q = q, Status = status, Total = dt.Rows.Count };
        foreach (DataRow r in dt.Rows)
        {
            string group = r.Str("group_title");
            var g = vm.Groups.FirstOrDefault(x => x.Title == group);
            if (g == null)
            {
                g = new FeatureGroupViewModel { Title = group };
                vm.Groups.Add(g);
            }
            g.Features.Add(ToCard(r));
        }
        return vm;
    }

    public async Task<FeatureDetailViewModel?> GetDetailAsync(string slug)
    {
        var dt = await _repo.GetBySlugAsync(slug);
        if (dt.Rows.Count == 0) return null;
        var r = dt.Rows[0];
        var card = ToCard(r);
        var related = await _repo.GetRelatedAsync(card.GroupTitle, card.Slug);
        return new FeatureDetailViewModel
        {
            Feature = card,
            Problem = r.Str("problem"),
            Solution = r.Str("solution"),
            Benefits = Pairs(r.Arr("benefits")),
            Stats = Pairs(r.Arr("stats")),
            Faqs = Pairs(r.Arr("faqs")),
            Points = r.Arr("points").ToList(),
            Steps = r.Arr("steps").ToList(),
            HasApp = r.Bool("has_app"),
            AppLabel = r.Str("app_label"),
            Related = related.Rows.Cast<DataRow>().Select(ToCard).ToList()
        };
    }

    /// <summary>"Title|Text" strings → pairs (split on the first pipe only).</summary>
    private static List<TitleTextViewModel> Pairs(IEnumerable<string> items) =>
        items.Select(x =>
        {
            int i = x.IndexOf('|');
            return i < 0 ? new TitleTextViewModel { Title = x } : new TitleTextViewModel { Title = x[..i], Text = x[(i + 1)..] };
        }).ToList();

    internal static FeatureCardViewModel ToCard(DataRow r) => new()
    {
        Slug = r.Str("slug"),
        Title = r.Str("title"),
        Icon = r.Str("icon"),
        LiveStatus = r.Str("live_status"),
        Lead = r.Str("lead"),
        GroupTitle = r.Str("group_title")
    };
}
