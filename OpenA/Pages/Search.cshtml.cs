using Factograph.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OpenA.Pages
{
    public class SearchModel : PageModel
    {
        public string? ss { get; set; } = null;
        string? tv { get; set; } = null;
        string? bw { get; set; } = null;

        public Factograph.Data.IFDataService db { get; set; }

        public SearchModel(Factograph.Data.IFDataService db)
        {
            this.db = db;
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
                IEnumerable<RRecord> query = db.SearchRRecords(ss, bw == "on");
                if (tv != "")
                {
                    if (tv == "person") query = query.Where(r => r.Tp == "http://fogid.net/o/person");
                    else if (tv == "org-sys") query = query.Where(r => r.Tp == "http://fogid.net/o/org-sys");
                    else if (tv == "collection") query = query.Where(r => 
                        db.ontology.DescendantsAndSelf("http://fogid.net/o/collection").Contains(r.Tp));
                    else if (tv == "document") query = query.Where(r =>
                        db.ontology.DescendantsAndSelf("http://fogid.net/o/document").Contains(r.Tp));
                    else if (tv == "geosys") query = query.Where(r =>
                        db.ontology.DescendantsAndSelf("http://fogid.net/o/geosys").Contains(r.Tp));
                }
                search_results = query.ToArray();
            }
        }
    }
}
