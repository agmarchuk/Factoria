using Factograph.Data;
using Factograph.Data.r;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OpenA.Pages;

public class IndexModel : PageModel
{
    public string? ss { get; set; } = null;
    string? tv { get; set; } = null;
    string? bw { get; set; } = null;
    
    public string? id { get; private set; } = null;
    public string? eid { get; set; } = null;
    public Rec? tree { get; set; } = null;
    public string bas { get; set; } = "cassetterootcollection";
    public string? start_page { get; set; } = "cassetterootcollection";
    public string? funds_id { get; set; } = "cassetterootcollection";

    private readonly ILogger<IndexModel> _logger;
    public string TestMessage { get; set; } = "";
    private IConfiguration configuration;

    public Factograph.Data.IFDataService db { get; set; }

    public IndexModel(ILogger<IndexModel> logger, Factograph.Data.IFDataService db, IConfiguration configuration)
    {
        _logger = logger;
        this.db = db;
        // Здесь будут производиться предвычисления
        //if (this.db.percalculated == null) this.db.precalculated = что-то вычисленное;
        this.configuration = configuration;

        start_page = configuration.GetSection("App:start_page")?.Value ?? start_page;
        var fid = db.SearchRRecords("Фонды", false)?.FirstOrDefault(r => r.GetName() == "Фонды")?.Id;
        if (fid != null)
        {
            funds_id = fid;
        }
    }

    // Модели для передачи в View
    public RRecord[] search_results = new RRecord[0];

    public void OnGet()
    {
        this.ss = Request.Query["ss"].ToString();
        this.tv = Request.Query["tv"].ToString();
        this.bw = Request.Query["bw"].ToString();
     
        if (!string.IsNullOrEmpty(ss))
        {
            var qu = db.SearchRRecords(ss, bw == "on");
            search_results = qu.ToArray();
        }
        else
        {
            this.id = Request.Query["id"].ToString();
            this.eid = Request.Query["eid"].ToString();
            if (string.IsNullOrEmpty(this.id)) { this.id = funds_id; }
            RRecord? rr = db.GetRRecord(id, true);
            if (rr == null) return;
            string tp = rr.Tp;
            var shablon = Rec.GetUniShablon(tp, 2, null, db.ontology);
            var tre = Rec.Build(rr, shablon, db.ontology, ident => db.GetRRecord(ident, false));
            tree = tre;
        }
    }
}
