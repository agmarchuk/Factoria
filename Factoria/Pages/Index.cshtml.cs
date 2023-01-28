using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
//using Factoria.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using FactographData;

namespace RazorPagesMovie.Pages.Movies
{
    public class IndexModel : PageModel
    {
        private readonly FactographData.IFDataService _context;

        public IndexModel(FactographData.IFDataService context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string? Genre { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? searchsample { get; set; }
        [BindProperty(SupportsGet = true)]
        public bool extended { get; set; }

        public SelectList? Genres { get; set; }

        public string? Title { get; set; }
        //public IList<XElement> items { get; set; }

        public void OnGet()
        {
            if (!string.IsNullOrEmpty(searchsample))
            {
                var query = _context.SearchByName(searchsample);
                Title = "Hello from OnGetAsync: " + query.Count();
            }
        }
    }
}