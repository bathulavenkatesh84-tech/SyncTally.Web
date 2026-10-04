using System.Data;
using SyncTally.Web.Repositories.Interfaces;
using SyncTally.Web.Services.Interfaces;

namespace SyncTally.Web.Repositories.Implementations;

public class PartnerRepository : IPartnerRepository
{
    private readonly IFlexidb _db;
    public PartnerRepository(IFlexidb db) => _db = db;

    public Task<DataTable> SearchAsync(string? q, string? kind, string sort, int skip, int take)
    {
        string orderBy = sort switch
        {
            "city" => "city ASC, name ASC",
            "kind" => "kind ASC, name ASC",
            _ => "sort_order ASC, name ASC"
        };
        return _db.GetdtAsync(SqlHelper.Cmd(
            $@"SELECT name, kind, city, blurb, COUNT(*) OVER() AS total_count
                 FROM cmr_admin.site_partner
                WHERE is_active
                  AND (@q = '' OR name ILIKE '%' || @q || '%' OR city ILIKE '%' || @q || '%' OR blurb ILIKE '%' || @q || '%')
                  AND (@kind = '' OR kind = @kind)
                ORDER BY {orderBy}
                LIMIT @take OFFSET @skip",
            ("q", (q ?? string.Empty).Trim()), ("kind", (kind ?? string.Empty).Trim()), ("take", take), ("skip", skip)));
    }

    public Task<DataTable> GetKindsAsync() => _db.GetdtAsync(SqlHelper.Cmd(
        "SELECT DISTINCT kind FROM cmr_admin.site_partner WHERE is_active ORDER BY kind"));
}
