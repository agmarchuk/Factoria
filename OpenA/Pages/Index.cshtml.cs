using Factograph.Data.r;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OpenA.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly Factograph.Data.IFDataService db;

        public readonly string[] tips = ["http://fogid.net/o/person", "http://fogid.net/o/org-sys", "http://fogid.net/o/collection",
        "http://fogid.net/o/document"];

        private Dictionary<string, Rec>? shablons = null;

        public string? id { get; set; }
        public string? ss { get; set; }
        public string? tp { get; set; }
        public string? idd { get; set; }

        public bool bywords = false;


        public IndexModel(ILogger<IndexModel> logger, Factograph.Data.IFDataService db)
        {
            _logger = logger;
            this.db = db;
        }

        public void OnGet(string id, string idd)
        {
            //id = this.Request.Query["id"].FirstOrDefault();
            ss = this.Request.Query["ss"].FirstOrDefault();
            tp = this.Request.Query["tp"].FirstOrDefault();
            string? sbw = this.Request.Query["bw"].FirstOrDefault();
            bywords = sbw != null ? true : false;
            //idd = this.Request.Query["idd"].FirstOrDefault();
        }
        public void OnPost()
        {
            ss = this.Request.Form["ss"].FirstOrDefault();
            tp = this.Request.Form["tp"].FirstOrDefault();
            string? sbw = this.Request.Form["bw"].FirstOrDefault();
            bywords = sbw != null ? true : false;
        }
    }
}
