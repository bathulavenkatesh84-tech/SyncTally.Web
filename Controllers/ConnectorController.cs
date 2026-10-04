using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using SyncTally.Web.ViewModels;

namespace SyncTally.Web.Controllers;

public class ConnectorController : BaseController
{
    private readonly IWebHostEnvironment _env;
    public ConnectorController(IWebHostEnvironment env) => _env = env;

    /// <summary>Windows connector (Tally Sync Config .exe + its DLLs) packaged in wwwroot/downloads.</summary>
    [HttpGet]
    public IActionResult Download()
    {
        var file = _env.WebRootFileProvider.GetFileInfo("downloads/SyncTally-Connector.zip");
        if (!file.Exists || file.PhysicalPath == null)
        {
            Toast("The connector package is not available yet. Please contact support.", "error");
            return RedirectToAction(nameof(Index));
        }
        return PhysicalFile(file.PhysicalPath, "application/zip", "SyncTally-Connector.zip");
    }

    public IActionResult Index()
    {
        ViewData["Title"] = "Tally Connector";
        SetCrumbs(new Crumb("Home", Url.Action("Index", "Home")), new Crumb("Tally Connector"));
        return View();
    }

    [HttpGet]
    public IActionResult Config()
    {
        string json = System.Text.Json.JsonSerializer.Serialize(new
        {
            product = "SyncTally Connector",
            version = "1.0.0",
            server = "https://api.synctally.example (placeholder)",
            pairingCode = "generate-in-app",
            tally = new { host = "localhost", port = 9000, company = "Your Company Name" },
            mode = "outbound-only",
            note = "Demo configuration file."
        }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        return File(Encoding.UTF8.GetBytes(json), "application/json", "synctally-connector-config.json");
    }

    [HttpGet]
    public IActionResult Guide()
    {
        string text = string.Join("\n", new[]
        {
            "SyncTally Connector – setup guide", "",
            "1. In Tally Prime: F1 (Help) > Settings > Connectivity > Client/Server configuration. Set \"Tally.ERP 9 is acting as\" to Both and the port to 9000.",
            "2. Install the connector on the same PC as Tally.",
            "3. Open SyncTally > Tally Connector and generate a pairing code.",
            "4. Enter the code in the connector, then press Test & connect.",
            "5. Use Sync now, or export XML and import in Tally via Import Data > Vouchers.", "",
            "The connector only makes outbound connections. No remote desktop is required."
        });
        return File(Encoding.UTF8.GetBytes(text), "text/plain", "synctally-connector-setup.txt");
    }

    [HttpGet]
    public IActionResult SampleItems() => File(Encoding.UTF8.GetBytes(
        "name,category,price,gst,stock,unit\nTea Premium 250g,Beverages,135,5,120,Box of 24\nGlucose Biscuits 100g,Biscuits & Snacks,10,18,500,Carton of 100"),
        "text/csv", "sample-items.csv");

    [HttpGet]
    public IActionResult SampleLedgers() => File(Encoding.UTF8.GetBytes(
        "name,owner,phone,city,gstin,creditLimit\nGreen Valley Stores,Ajay Nair,9822012345,Pune,,40000\nOm Sai Traders,Kiran More,9822012346,Satara,27AAACO1234F1Z5,60000"),
        "text/csv", "sample-ledgers.csv");
}
