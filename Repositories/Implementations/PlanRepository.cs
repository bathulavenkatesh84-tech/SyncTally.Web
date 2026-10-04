using System.Data;
using SyncTally.Web.Repositories.Interfaces;
using SyncTally.Web.Services.Interfaces;

namespace SyncTally.Web.Repositories.Implementations;

public class PlanRepository : IPlanRepository
{
    private readonly IFlexidb _db;
    public PlanRepository(IFlexidb db) => _db = db;

    public Task<DataTable> GetAllAsync() => _db.GetdtAsync(SqlHelper.Cmd(
        "SELECT name, price_1y, features, is_popular FROM cmr_admin.site_plan ORDER BY sort_order"));
}
