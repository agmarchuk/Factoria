using Factograph.Data;
using Factograph.Data.Adapters;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;

namespace Tres.Controllers
{
    public class DocsController : Controller
    {
        private readonly Factograph.Data.IFDataService db;
        public DocsController(Factograph.Data.IFDataService db)
        {
            this.db = db;
        }

        [HttpGet("docs/GetImage")]
        public IActionResult GetImage(string uri, string size)
        {
            string path = db.GetFilePath(uri, size);
            if (!System.IO.File.Exists(path + ".jpg"))
            {
                size = size == "medium" ? "normal" : "medium";
                path = db.GetFilePath(uri, size);
            }
            if (string.IsNullOrEmpty(path))
            {
                return new EmptyResult();
            }
            return PhysicalFile(path + ".jpg", "image/jpg");
        }

        [HttpGet("docs/Room216")]
        public IActionResult Room216()
        {
            db.Reload();
            return Redirect(Url.Content("/"));
        }

        [HttpGet("docs/GetVideo")]
        public IActionResult GetVideo(string uri)
        {
            if (uri == null) return new EmptyResult();
            string path = db.GetFilePath(uri, "medium");
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path + ".mp4"))
            {
                return new EmptyResult();
            }
            return PhysicalFile(path + ".mp4", "video/mp4"); 
        }
        [HttpGet("docs/GetVideo0")]
        public IActionResult GetVideo0(string uri)
        {
            string path = db.GetFilePath(uri, "medium");
            if (path == null) return NotFound();

            string dir_path = path.Substring(0, path.Length - 5);
            string file_num = path.Substring(path.Length - 4);
            System.IO.DirectoryInfo dinfo = new System.IO.DirectoryInfo(dir_path);
            System.IO.FileInfo[] qu = dinfo.GetFiles(file_num + ".*");
            if (qu.Length == 0)
            {
                string beg = path.Substring(0, path.Length - 26);
                string end = path.Substring(path.Length - 10);
                path = beg + "originals" + end;

                dir_path = path.Substring(0, path.Length - 5);
                file_num = path.Substring(path.Length - 4);
                dinfo = new System.IO.DirectoryInfo(dir_path);
                qu = dinfo.GetFiles(file_num + ".*");
                if (qu.Length == 0) return NotFound();
            }
            int pos = qu[0].Name.LastIndexOf('.');
            if (pos == -1) return NotFound();
            string ext = qu[0].Name.Substring(pos + 1);
            return PhysicalFile(path + "." + ext, "video/" + ext);
        }
        [HttpGet("docs/GetAudio")]
        public IActionResult GetAudio(string uri)
        {
            string path = db.GetFilePath(uri, null);
            if (path == null) return NotFound();
            var q = path.Replace("documents/normal", "originals");
            return PhysicalFile(q + ".mp3", "audio/mp3");
        }
        [HttpGet("docs/GetPdf")]
        public IActionResult GetPdf(string uri)
        {
            string path = db.GetFilePath(uri, null);
            if (path == null) return NotFound();
            var q = path.Replace("documents/normal", "originals");
            return PhysicalFile(q + ".pdf", "application/pdf");
        }
        [HttpGet("docs/GetDoc")]
        public IActionResult GetDoc(string uri)
        {
            string? path = db.GetOriginalPath(uri);
            if (path == null) return NotFound();
            int pos = path.LastIndexOf(".");
            int pos1 = path.LastIndexOfAny(new char[] { '/', '\\' });
            string filename = path.Substring(pos1 + 1);
            return PhysicalFile(path,
                "application/" + path.Substring(pos + 1),
                filename);
        }
    }

}
