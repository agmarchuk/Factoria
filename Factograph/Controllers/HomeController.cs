using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Factograph.Models;

namespace S1957.Controllers
{
    //[Route("home/")]
    public class HomeController : Controller
    {
        // Это база данных
        private Factograph.Data.IFDataService db;
        // Это модель
        //private IndexModel model = new IndexModel(db);
        public HomeController(Factograph.Data.IFDataService db)
        {
            this.db = db;
        }
        // GET: Home
        public ActionResult Index()
        {
            if (true) { return new RedirectResult("~/View/Portrait"); }
            IndexModel model = new IndexModel(db);
            model.id = Request.Query["id"];
            model.searchstring = Request.Query["searchstring"];
            return View(model);
        }
    }
}
