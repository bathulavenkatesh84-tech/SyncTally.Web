using System.Data;
using SyncTally.Web.Repositories.Interfaces;
using SyncTally.Web.Services.Interfaces;
using SyncTally.Web.ViewModels;

namespace SyncTally.Web.Services.Implementations;

public class PricingService : IPricingService
{
    private readonly IPlanRepository _repo;
    public PricingService(IPlanRepository repo) => _repo = repo;

    public async Task<List<PlanViewModel>> GetPlansAsync()
    {
        var dt = await _repo.GetAllAsync();
        return dt.Rows.Cast<DataRow>().Select(r => new PlanViewModel
        {
            Name = r.Str("name"),
            Price1Y = r.Dec("price_1y"),
            Features = r.Arr("features").ToList(),
            IsPopular = r.Bool("is_popular")
        }).ToList();
    }
}
