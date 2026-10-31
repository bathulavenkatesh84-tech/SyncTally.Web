// ComparisonTableViewModel.cs
// Place in: ViewModels/ComparisonTableViewModel.cs

namespace SyncTally.Web.ViewModels
{
    /// <summary>
    /// View model for the 3-column feature comparison table.
    /// One instance per scenario section (e-invoice, payment collection, etc.)
    /// </summary>
    public class ComparisonTableViewModel
    {
        /// <summary>Heading above the table. e.g. "What happens after the IRN is generated"</summary>
        public string Title { get; set; } = "";

        /// <summary>Your product column header. e.g. "SyncTally"</summary>
        public string YourProduct { get; set; } = "SyncTally";

        /// <summary>Competitor column header. e.g. "Other Tally users"</summary>
        public string Competitor { get; set; } = "Other Tally users";

        /// <summary>Date shown in the footnote. e.g. "October 2026"</summary>
        public string FootnoteDate { get; set; } = "";

        /// <summary>The comparison rows.</summary>
        public List<ComparisonRow> Rows { get; set; } = new();
    }

    public class ComparisonRow
    {
        /// <summary>The feature / scenario label (left column).</summary>
        public string Feature { get; set; } = "";

        /// <summary>What YOUR product does (middle/highlighted column).</summary>
        public string Yours { get; set; } = "";

        /// <summary>What competitors do (right column, grayed).</summary>
        public string Theirs { get; set; } = "";

        /// <summary>Optional HTML icon for your column (e.g. a checkmark SVG).</summary>
        public string YoursIcon { get; set; } = "";

        /// <summary>Optional HTML icon for competitor column.</summary>
        public string TheirsIcon { get; set; } = "";
    }

    // ────────────────────────────────────────────────────────────────────────
    //  PRE-BUILT COMPARISON DATA — ready to use in your controllers/views
    //  Copy the ones relevant to your product into your controller or a
    //  static data class and pass them to the view.
    // ────────────────────────────────────────────────────────────────────────
    public static class ComparisonData
    {
        private const string CHECK = @"<svg width=""14"" height=""14"" viewBox=""0 0 24 24"" fill=""none"" stroke=""#1e3a8a"" stroke-width=""2.5"" stroke-linecap=""round"" stroke-linejoin=""round""><polyline points=""20 6 9 17 4 12""/></svg>";

        /// <summary>E-invoice / IRN scenario comparison table.</summary>
        public static ComparisonTableViewModel EInvoice(string yourProduct = "SyncTally") => new()
        {
            Title        = "What happens after the IRN is generated",
            YourProduct  = yourProduct,
            Competitor   = "Other Tally users",
            FootnoteDate = DateTime.Now.ToString("MMMM yyyy"),
            Rows = new List<ComparisonRow>
            {
                new() { Feature = "Generating the IRN from a phone",      Yours = "Yes, without a portal login",                        YoursIcon = CHECK, Theirs = "Sometimes, and usually only generation" },
                new() { Feature = "Cancelling an e-invoice from the phone", Yours = "Yes, inside the government window",                YoursIcon = CHECK, Theirs = "Back to the portal on a desktop" },
                new() { Feature = "How it lands in Tally",                 Yours = "Written back onto the same voucher",                YoursIcon = CHECK, Theirs = "Often a second document to match up" },
                new() { Feature = "Your invoice numbering series",          Yours = "Kept as it is",                                    YoursIcon = CHECK, Theirs = "Sometimes replaced by the app's own series" },
                new() { Feature = "Sending the registered invoice on",      Yours = "WhatsApp dispatch with a pay link on the same message", YoursIcon = CHECK, Theirs = "Download and forward it yourself" },
            }
        };

        /// <summary>Payment collection scenario comparison table.</summary>
        public static ComparisonTableViewModel PaymentCollection(string yourProduct = "SyncTally") => new()
        {
            Title        = "What happens when you collect a payment",
            YourProduct  = yourProduct,
            Competitor   = "Other Tally users",
            FootnoteDate = DateTime.Now.ToString("MMMM yyyy"),
            Rows = new List<ComparisonRow>
            {
                new() { Feature = "Sending a payment reminder",             Yours = "Automatic WhatsApp reminder with UPI link",          YoursIcon = CHECK, Theirs = "Manual call or message" },
                new() { Feature = "Collecting payment",                     Yours = "UPI link in WhatsApp, zero MDR",                     YoursIcon = CHECK, Theirs = "NEFT/RTGS or cash, manual follow-up" },
                new() { Feature = "Reconciliation into Tally",              Yours = "Auto-reconciled on T+1",                            YoursIcon = CHECK, Theirs = "Screenshot verification + manual entry" },
                new() { Feature = "Settlement to your bank",                Yours = "T+1 direct to account",                             YoursIcon = CHECK, Theirs = "Depends on payment mode" },
                new() { Feature = "Overdue tracking",                       Yours = "Automated escalating reminders",                    YoursIcon = CHECK, Theirs = "Manual ledger check + follow-up calls" },
            }
        };

        /// <summary>Tally mobile access scenario comparison table.</summary>
        public static ComparisonTableViewModel TallyMobile(string yourProduct = "SyncTally") => new()
        {
            Title        = "What your team can do on the phone",
            YourProduct  = yourProduct,
            Competitor   = "Other Tally users",
            FootnoteDate = DateTime.Now.ToString("MMMM yyyy"),
            Rows = new List<ComparisonRow>
            {
                new() { Feature = "Check outstanding receivables",          Yours = "Live, from the phone",                              YoursIcon = CHECK, Theirs = "Login to Tally desktop" },
                new() { Feature = "Create a sales order",                   Yours = "On the phone, syncs to Tally",                      YoursIcon = CHECK, Theirs = "Only on desktop Tally" },
                new() { Feature = "Share invoice via WhatsApp",             Yours = "One tap from the app",                              YoursIcon = CHECK, Theirs = "Export PDF → share manually" },
                new() { Feature = "View stock on hand",                     Yours = "Real-time from Tally",                              YoursIcon = CHECK, Theirs = "Need desktop access" },
                new() { Feature = "Salesman order app",                     Yours = "Dedicated salesman app with order link",            YoursIcon = CHECK, Theirs = "Not available" },
            }
        };

        /// <summary>GST / compliance scenario comparison table.</summary>
        public static ComparisonTableViewModel GstCompliance(string yourProduct = "SyncTally") => new()
        {
            Title        = "What happens during GST filing",
            YourProduct  = yourProduct,
            Competitor   = "Other Tally users",
            FootnoteDate = DateTime.Now.ToString("MMMM yyyy"),
            Rows = new List<ComparisonRow>
            {
                new() { Feature = "E-way bill from phone",                  Yours = "Generate and share from the app",                   YoursIcon = CHECK, Theirs = "Portal login on desktop" },
                new() { Feature = "GST report access",                      Yours = "On mobile, drill-down ready",                       YoursIcon = CHECK, Theirs = "Tally desktop only" },
                new() { Feature = "Import purchase from PDF",               Yours = "Scan PDF → auto-entries in Tally",                  YoursIcon = CHECK, Theirs = "Manual data entry" },
                new() { Feature = "GSTR reconciliation",                    Yours = "Matched automatically",                             YoursIcon = CHECK, Theirs = "Manual spreadsheet matching" },
            }
        };
    }
}
