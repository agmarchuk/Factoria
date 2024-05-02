using System.Xml.Linq;

using Factograph.Data;
using Factograph.Data.r;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Publicuem2
{
    public class ShowDocs
    {
        public static MimeResult CreateMime(Factograph.Data.IFDataService db, HttpRequest request)
        {
            string? action = request.Query["action"];
            string? u = request.Query["u"];
            if (string.IsNullOrEmpty(u)) return new MimeResult("", "");
            if (action == null)
            {
                string? s = request.Query["s"];

                string? path = db.GetFilePath(u, s);
                if (!System.IO.File.Exists(path + ".jpg"))
                {
                    s = s == "medium" ? "normal" : "medium";
                    path = db.GetFilePath(u, s);
                }
                if (path == null) path = "";
                return new MimeResult(path + ".jpg", "image/jpg");
            }
            if (action == "getvideo")
            {
                string? s = request.Query["s"];

                string? path = db.GetFilePath(u, s);

                if (path == null) path = "";
                return new MimeResult(path + ".mp4", "video/mp4");
            }
            else if (action == "getaudio")
            {
                string? path = db.GetFilePath(u, null);
                return new MimeResult(path + ".mp3", "audio/mp3");
            }
            else if (action == "getpdf")
            {
                string? path = db.GetFilePath(u, null);
                return new MimeResult(path + ".pdf", "application/pdf");
            }
            return new MimeResult("", "");
        }
    }
}