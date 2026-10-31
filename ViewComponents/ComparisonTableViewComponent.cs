using Microsoft.AspNetCore.Mvc;

namespace SyncTally.Web.ViewComponents
{
    public class ComparisonTableViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(ComparisonTableParams p) => View(p);
    }

    public class ComparisonTableParams
    {
        public string Title        { get; set; } = "";
        public string YourProduct  { get; set; } = "SyncTally";
        public string Competitor   { get; set; } = "Other Tally users";
        public string FootnoteDate { get; set; } = "";
        public List<SyncTally.Web.ViewModels.ComparisonRow> Rows { get; set; } = new();
    }

    //public class ComparisonRow
    //{
    //    public string Feature { get; set; } = "";
    //    public string Yours   { get; set; } = "";
    //    public string Theirs  { get; set; } = "";
    //}
}
