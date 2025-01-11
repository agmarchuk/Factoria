using Factograph.Data.r;
using Factograph.Data;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Encodings.Web;
using Factograph.Models;

namespace Factograph.Controllers
{
    public class ViewController : Controller
    {
        private Factograph.Data.IFDataService db;
        public ViewController(Factograph.Data.IFDataService db)
        {
            this.db = db;
        }
        
        public IActionResult Portrait()
        {
            Factograph.Models.IndexModel model = new Factograph.Models.IndexModel(db);
            var dec = WebUtility.UrlDecode;
            model.id = (string?)Request.RouteValues["id"]; 
            model.direction = Request.Query["direction"];
            model.searchstring = dec(Request.Query["searchstring"]);
            model.bwords = Request.Query["bywords"] == "on";
            model.stype = dec(Request.Query["stype"]);
            if (model.id != null) 
            { 
                var rrec =  db.GetRRecord(model.id, true);
                if (rrec != null)
                {
                    var pp = ((Precalc?)db.precalculated)?.treeShablons;
                    //model.shablon = Rec.GetUniShablon(rrec.Tp, 2, null, db.ontology);
                    if (pp != null) model.shablon = pp[rrec.Tp];
                    else model.shablon = null;
                    model.shablon = pp != null ? pp[rrec.Tp] : null;
                    //model.tree = Rec.Build(rrec, model.shablon, db.ontology, idd => db.GetRRecord(idd, false));
                    if (model.shablon != null)
                    {
                        model.tree = IndexModel.BuildTree(rrec, model.shablon, db);
                    }
                    else model.tree = null;
                }
                else model.tree = null;

            }
            return View(model);
        }

    }
}
