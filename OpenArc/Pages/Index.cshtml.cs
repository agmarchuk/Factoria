using Factograph.Data;
using Factograph.Data.r;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Xml.Linq;

namespace OpenArc.Pages
{
    public class IndexModel : PageModel
    {
        public Rec? tree;

        private readonly ILogger<IndexModel> _logger;
        private readonly Factograph.Data.IFDataService db;
        string funds_id = "cassetterootcollection";

        public IndexModel(ILogger<IndexModel> logger, Factograph.Data.IFDataService db)
        {
            _logger = logger;
            this.db = db;
        }

        public void OnGet(string? id, string idd)
        {
            if (id == null) id = funds_id;
            RRecord? rr = db.GetRRecord(id, false);
            if (rr == null) return;
            string tp = rr.Tp;
            var shablon = Rec.GetUniShablon(tp, 2, null, db.ontology);
            var tre = Rec.Build(rr, shablon, db.ontology, ident => db.GetRRecord(ident, false));
            tree = tre;
        }
    }
}
