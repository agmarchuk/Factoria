//using Factograph.Data;
//using Factograph.Data.Adapters;
//using Microsoft.AspNetCore.Components;
//using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Mvc;
//using System.Collections.Generic;
//using System.Net;
//using System.Xml.Linq;
//using static System.Net.WebRequestMethods;

namespace Controllers
{
    public class DocsController : Controller
    {
        private readonly Factograph.Data.IFDataService db;
        public DocsController(Factograph.Data.IFDataService db)
        {
            this.db = db;
        }

        [HttpGet("docs/GetImage")]
        public IActionResult GetImage(string u, string s)
        {
            string path = db.GetFilePath(u, s);
            if (!System.IO.File.Exists(path + ".jpg"))
            {
                s = s == "medium" ? "normal" : "medium";
                path = db.GetFilePath(u, s);
            }
            if (string.IsNullOrEmpty(path))
            {
                return new EmptyResult();
            }
            return PhysicalFile(path + ".jpg", "image/jpg");
        }
    }
}