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
        public string id { get; set; }
        public string tp { get; set; }
        public string[] table; // Первый ряд - заголовки, остальные - данные

        public void OnGet()
        {
            if (string.IsNullOrEmpty(searchsample))
            {
                var tt = ((FDataService)_context).ttreebuilder.GetTTree("famwf1233_1001");
                TTree tree;
                if (tt != null)
                {
                    tree = (TTree)tt;
                    id = tt.Id;
                    tp = tt.Tp;
                    table = tt.Groups
                        .Where(g => g != null && (g is TString || g is TTexts || g is TDTree))
                        .Select<TGroup, string>(g =>
                        {
                            if (g is TString) return ((TString)g).Value;
                            if (g is TTexts) return ((TTexts)g).Values
                                .Select<TextLan, string>(tl => tl.Text + "^" + tl.Lang)
                                .Aggregate((s, t) => s + " " + t);
                            if (g is TDTree) 
                            {
                                var tdt = (TDTree)g;
                                return tdt.Resource.ToString();
                            }
                            return "uknown";
                        }).ToArray();

                }
            }
            else
            {
                var query = _context.SearchByName(searchsample);
                Title = "Hello from OnGetAsync: " + query.Count();
            }
        }
    }
}