using Microsoft.AspNetCore.Mvc;

namespace Family.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("/Gene");
        }
    }
}
