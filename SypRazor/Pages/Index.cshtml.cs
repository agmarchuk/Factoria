using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SypRazor;

namespace SypRazor.Pages
{
    public class IndexModel : PageModel
    {
        internal readonly Factograph.Data.IFDataService db;

        public IndexModel(Factograph.Data.IFDataService db)
        {
            this.db = db;
            if (typs.Length == 0) typs = db.ontology.DescendantsAndSelf("http://fogid.net/o/sys-obj").ToArray();
        }

        [BindProperty(SupportsGet = true)]
        public string? searchstring { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? stype { get; set; }
        [BindProperty(SupportsGet = true)]
        public bool bywords { get; set; } = false;

        internal string[] typs = new string[0];


        public void OnGet()
        {
            Console.WriteLine($"== controller == {searchstring}");
        }
    }
}