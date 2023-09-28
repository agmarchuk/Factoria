using FactographData;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using OAData.Adapters;
using System.Net;
using System.Xml.Linq;
using static System.Net.WebRequestMethods;

namespace SypBlazor.Controllers
{
    public class DocsController : Controller
    {
        private readonly FactographData.IFDataService db;
        public DocsController(FactographData.IFDataService db) 
        { 
            this.db = db; 
        }

        /// <summary>
        /// Сюда приходит запрос от выбранных обратных отношений, которые помещаются в буфер.
        /// Формат такой: команда entityId - (кодированный) идентификатор опорной сущности
        /// команда inv_pred=строка - иденификатор обратного предиката для данной группу обратных отношений
        /// команды кодированный(идент)=on - помеченные псевдосущности отношений.
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet("docs/cpycut")]
        public IActionResult cpycut()
        {
            // Прием параметров запроса
            string entityId = WebUtility.UrlDecode(Request.Query["entityId"]);
            string dir_pred = WebUtility.UrlDecode(Request.Query["dir_pred"]);
            string? user = Request.Query["user"];
            if (user == null) goto Out; //{ user = WebUtility.UrlDecode(user); }
            bool tocut = false;
            bool tocopy = false;
            List<string> relationIds = new List<string>();
            var query = this.HttpContext.Request.Query;
            foreach (var item in query)
            {
                if      (item.Key == "copy") tocopy = true;
                else if (item.Key == "cut") tocut = true;
                else if (item.Value == "on") relationIds.Add(WebUtility.UrlDecode(item.Key));
            }
            // Вычисление множества ИДЕНТИФИКАТОРОВ объектов (как правило, документов), которые помещаются в буфер
            List<string?> set = relationIds.Select<string, string?>(rid => 
            {
                // Вычислим узел (запись)
                var node = db.GetRRecord(rid, false);
                if (node == null) return null; 
                // Поищем прямые ссылки
                foreach (RProperty pro in node.Props) 
                {
                    if (pro is RLink && pro.Prop == dir_pred)
                    {
                        var rlnk = (RLink)pro;
                        return rlnk.Resource;
                    }
                }
                return null;
            })
            .Where(r => r != null)  
            .ToList();
            // Буфер является индивидуальным для каждого пользователя.
            // Если у пользователя нет user, то и буфера нет. Буфер - это коллекция,
            // элементами которой являются сохраняемые элементы. Идентификатор буферной 
            // коллекции формируется как buffer_{user}. Построим
            string buff_id = "buffer_" + user;
            // Выясняем есть ли такой элемент
            RRecord? rRecord = db.GetRRecord(buff_id, true);
            // Если есть, то чистим, если нет, то заводим
            if (rRecord != null)
            {
                // выделяем членов
                string[] members = rRecord.Props.Select<RProperty, string?>(p =>
                {
                    if (p is RInverseLink && p.Prop == "http://fogid.net/o/in-collection") 
                        return ((RInverseLink)p).Source;
                    return null;
                })
                .Where(r => r != null)
                .Cast<string>()
                .ToArray();
                // чистим
                foreach (var member in members)
                {
                    db.PutItem(new XElement("delete",
                        new XAttribute("owner", user),
                        new XAttribute(ONames.rdfabout, member)));
                }
            }
            else
            {
                // А здесь создаем
                db.PutItem(new XElement("{http://fogid.net/o/}collection",
                        new XAttribute("owner", user),
                        new XAttribute(ONames.rdfabout, buff_id)));
            }
            // Теперь добавим элементы из set
            foreach (string? item_id in set)
            {
                db.PutItem(new XElement("{http://fogid.net/o/}collection-member",
                    new XAttribute("owner", user),
                    new XElement("{http://fogid.net/o/}in-collection", 
                        new XAttribute(ONames.rdfresource, buff_id)),
                    new XElement("{http://fogid.net/o/}collection-item",
                        new XAttribute(ONames.rdfresource, item_id))));
            }
            // Теперь уничтожим собранные в relationIds отношения, если cut
            if (tocut) foreach (string? item_id in relationIds)
            {
                    db.PutItem(new XElement("delete",
                        new XAttribute(ONames.rdfabout, item_id),
                        new XAttribute("owner", user)));
            }

        Out: { }
            return new RedirectResult("~/Index");
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
            return PhysicalFile(path + ".jpg", "image/jpg");
        }

        [HttpGet("docs/GetVideo")]
        public IActionResult GetVideo(string u)
        {
            string path = db.GetFilePath(u, "medium");
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
        public IActionResult GetAudio(string u)
        {
            string path = db.GetFilePath(u, null);
            if (path == null) return NotFound();
            var q = path.Replace("documents/normal", "originals");
            return PhysicalFile(q + ".mp3", "audio/mp3");
        }
        [HttpGet("docs/GetPdf")]
        public IActionResult GetPdf(string u)
        {
            string path = db.GetFilePath(u, null);
            if (path == null) return NotFound();
            var q = path.Replace("documents/normal", "originals");
            return PhysicalFile(q + ".pdf", "application/pdf");
        }
        [HttpGet("docs/GetDoc")]
        public IActionResult GetDoc(string u)
        {
            string? path = db.GetOriginalPath(u);
            if (path == null) return NotFound();
            int pos = path.LastIndexOf(".");
            int pos1 = path.LastIndexOfAny(new char[] { '/', '\\' });
            string filename = path.Substring(pos1 + 1);
            return PhysicalFile(path, 
                "application/" + path.Substring(pos+1),
                filename);
        }

    }

}
