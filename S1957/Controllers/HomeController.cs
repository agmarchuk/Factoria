using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using S1957.Models;

namespace S1957.Controllers
{
    //[Route("home/")]
    public class HomeController : Controller
    {
        // Это база данных
        private readonly Factograph.Data.IFDataService db;
        // Это модель
        private IndexModel model = new IndexModel();
        public HomeController(Factograph.Data.IFDataService db)
        {
            this.db = db;
        }
        public IActionResult Index()
        {
            model = new IndexModel();
            model.id = Request.Query["id"];
            model.searchstring = Request.Query["searchstring"];
            return View(model);
        }
    }
}
