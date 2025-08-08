using Factograph.Data;
using Factograph.Data.r;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics.Contracts;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Xml.Linq;

namespace Uno.Controllers
{
    public class ViewController : PageModel
    {
        private Factograph.Data.IFDataService db;
        string[] tips = ["http://fogid.net/o/person", "http://fogid.net/o/org-sys", "http://fogid.net/o/collection",
    "http://fogid.net/o/document"];

        Dictionary<string, Rec>? shablons;
        public ViewController(Factograph.Data.IFDataService db)
        {
            // Использую Url.Content("~/");
            var qu = @Url.Content("~/");
            this.db = db;
            shablons = tips.Select(t => Rec.GetUniShablon(t, 2, null, db.ontology))
                .ToDictionary(rec => rec.Tp);
        }
        public string SborkaPage(HttpRequest request, string? id)
        { 
            string? ss = request.Query["ss"].FirstOrDefault();
            string? tp = request.Query["tp"].FirstOrDefault();
            string? sbw = request.Query["bw"].FirstOrDefault(); // признак bywords "словами"
            bool bywords = sbw != null ? true : false;
            string? idd = request.Query["idd"].FirstOrDefault();

            // Далее, мы вычисляем части результирующей страницы или как XElement или как текст, каждая часть имеет свой идентификатор,
            // в конце будут вставлены в макет. Части:
            string selectsbor = BuildSelectResults(db, ss, tp, bywords); // раздел выдачи результатов поиска
            string portrait = BuildPortrait(db, id, shablons); // раздел портрета

            //<link href='{@Url.Content("~/css/site.css")}' rel='stylesheet' />
            string url1 = @Url.Content("~/css/site.css");
            string url2 = @Url.Content("~/img/logossyp.png");
            string maket = $@"<!DOCTYPE html>
<html>
    <head> 
        <meta charset='utf-8'> 
        <link href='{url1}' rel='stylesheet' />
    </head>
    <body>

    <div class='container' style='width:100%; background-color:lime;'>
        <a href='/view'> <img src='{url2}' style='height:60px;margin-left: 16px;margin-right:16px;'> </a>
        <div style='display: flex; flex-direction: column;font-size:large;align-items:center;'>
            <div style='font-size:x-large;font-weight:bold;color:white;'>Летние школы юных программистов</div>
            <div>Сезон 2025 года: 14-27 июля</div>
        </div>
    </div>

    <div style='margin-top:20px;'>
        <form method='get' action=''>
            <input name ='ss' value='{ss}' />
            <select name ='tp' >
                <option value='' ></option>
                <option value='http://fogid.net/o/person' {(tp == "http://fogid.net/o/person" ? "selected" : "")}>персона</option>
                <option value='http://fogid.net/o/org-sys' {(tp == "http://fogid.net/o/org-sys" ? "selected" : "")}>орг.сист.</option>
                <option value='http://fogid.net/o/collection' {(tp == "http://fogid.net/o/collection" ? "selected" : "")}>коллекция</option>
            </select>
            <span>словами<input type='checkbox' name='bw' {(bywords ? "checked" : "")}  /></span>
            <input type='submit' value='искать' />    
        </form>
    </div>

    <div style='margin: 20px 0px 20px 0px'>
        {selectsbor}
    </div>

    <div> 
        {portrait}
    </div>

    <footer>
        <hr/>
    </footer>
    </body>
</html>";
            return maket;
        }

        // ===================== Процедуры =======================
        static Dictionary<string, string> Doctipnames()
        {
            return new Tuple<string, string>[]
            {
        new Tuple<string, string>("http://fogid.net/o/photo-doc", "фото"),
        new Tuple<string, string>("http://fogid.net/o/video-doc", "видео"),
        new Tuple<string, string>("http://fogid.net/o/audio-doc", "аудио"),
        new Tuple<string, string>("http://fogid.net/o/document", "док."),
        new Tuple<string, string>("http://fogid.net/o/cassette", "касс."),
        new Tuple<string, string>("http://fogid.net/o/collection", "колл."),
        new Tuple<string, string>("http://fogid.net/o/person", "перс."),
        new Tuple<string, string>("http://fogid.net/o/org-sys", "орг."),
        new Tuple<string, string>("http://fogid.net/o/city", "гор."),
            }.ToDictionary(pa => pa.Item1, pa => pa.Item2);
        }

        static string BuildSelectResults(IFDataService db, string? ss, string? tp, bool bywords)
        {
            return !string.IsNullOrEmpty(ss) ? new XElement("div",
                db.SearchRRecords(ss, bywords).Select(rec =>
                {
                    string typ = rec.Tp;
                    if (!string.IsNullOrEmpty(tp) && tp != typ) { return null; }
                    var date = rec.GetField("http://fogid.net/o/from-date");
                    return new XElement("div",
                        new XElement("span", db.ontology.LabelOfOnto(tp)),
                        " ",
                        new XElement("a", new XAttribute("href", "~/view/" + rec.Id), rec.GetName()),
                        (date != null && date.Length > 3 ? new XElement("span", " " + date.Substring(0, 4)) : null),
                        null);
                }),
                new XElement("div", "== поиск завершен =="))
                .ToString() : "";
        }

        static string Kvad(string ttip, string iid, string name, string uri, string dates)
        {
            string url = "";
            if (ttip == "http://fogid.net/o/collection" || ttip == "http://fogid.net/o/cassette") url = "/img/collection_m.jpg";
            else if (ttip == "http://fogid.net/o/photo-doc") url = $"/photo?uri={uri}";
            else if (ttip == "http://fogid.net/o/video-doc") url = "/img/video_m.jpg";
            else if (ttip == "http://fogid.net/o/audio-doc") url = "/img/audio_m.jpg";
            else if (ttip == "http://fogid.net/o/document") url = "/img/document_m.jpg";
            var dic = Doctipnames();
            string tword = dic.ContainsKey(ttip) ? dic[ttip] : ttip;
            return
        @$"<div style='background-color:#CCFFCC; width:200px;height:200px;margin:10px 10px 10px 10px;'>
<div class='square' style='background: url({url}) center no-repeat; background-size:{(ttip == "http://fogid.net/o/photo-doc" ? "contain" : "auto")};margin:15px 20px 0px 10px;'>
    <div style='background-color:white;width:50px;align-self:end;'>{tword}</div>
    <div style='height:84%;'></div>
    <div style='background-color:white;'>
      <a href='/view/{iid}'>{name}</a>  {dates}
    </div>
</div></div>";
        }
        // 97FF97 ffffd8
        static string BuildPortrait(Factograph.Data.IFDataService db, string id, Dictionary<string, Rec>? shablons)
        {
            // Формировани портрета
            StringBuilder portrait = new StringBuilder();
            if (id != null)
            {
                var rrec = db.GetRRecord(id, true);
                while (rrec != null) // Цикл вместо условия для выхода по break
                {
                    string? subtype = shablons.Keys.FirstOrDefault(k => db.ontology.DescendantsAndSelf(k).Any(tt => tt == rrec.Tp));

                    if (subtype == null) break;

                    Factograph.Data.r.Rec? tree = Factograph.Data.r.Rec.Build(rrec, shablons[subtype], db.ontology,
                        idd => db.GetRRecord(idd, false));
                    string tip = tree.Tp;

                    // ======== источники: либо внешний источник (параметр idd), либо внешние коллекции 
                    // Элемент коллекций
                    var member_in_collections = tree.GetInverse("http://fogid.net/o/collection-item")
                        .Select(me =>
                        {
                            var coll = me.GetDirect("http://fogid.net/o/in-collection");
                            if (coll == null) return null;
                            string? name = coll.GetText("http://fogid.net/o/name");
                            string[] idna = new string[] { coll.Id ?? "", name ?? "noname" };
                            return idna;
                        })
                        .Where(x => x != null)
                        .Select(idna =>
                        {
                            return $"<a href='/view/{idna[0]}'>{idna[1]}</a>";
                        })
                        .Aggregate("", (sum, s) => sum + " " + s);
                    if (!string.IsNullOrEmpty(member_in_collections)) portrait.Append($"<div>................ источники: {member_in_collections}</div>");


                    // Выдадим поля: тип и идентификатор, главное поле - name, потом даты, описание
                    portrait.Append($@"<div><span style='background-color:lime;'>{db.ontology.LabelOfOnto(tree.Tp)}</span> {tree.Id}</div>");
                    portrait.Append($@"<div style='margin:10px 0px 10px 0px;'><span style='font-size:large; font-weight:bold; '>{tree.GetText("http://fogid.net/o/name")}</span> {tree.GetDates()} {tree.GetText("http://fogid.net/o/description")}
</div>");

                    // Теперь будут отличия для разных типов
                    if (tip == "http://fogid.net/o/person")
                    {
                        // Участия
                        var particip = tree.GetInverse("http://fogid.net/o/participant")
                            .Select(p =>
                            {
                                var dir = p.GetDirect("http://fogid.net/o/in-org");
                                if (dir == null) return "";
                                string? r = p.GetText("http://fogid.net/o/role");
                                string role = r == null ? "участник" : r;
                                string? orgname = dir.GetText("http://fogid.net/o/name");
                                string? orgcat = dir.GetText("http://fogid.net/o/org-category");
                                string? oc = dir.GetStr("http://fogid.net/o/org-classification");
                                string? oo = oc == null ? null : db.ontology.EnumValue("http://fogid.net/o/org-classificator", oc, "ru");
                                string orgvid = orgcat != null ? orgcat : (oo == null && oc != null ? oc : "орг.");
                                string? dtpart = p.GetDates();
                                string? dates = dtpart != null ? dtpart : dir.GetDates();
                                return $"<div>{role} <span style='background-color:lime;'>{orgvid}</span> <a href='/view/{dir.Id}'> {orgname} </a>  <span style='font-size:smaller;'>{dates ?? ""}</span></div>";
                            }).Aggregate("", (sum, s) => sum + " " + s);
                        portrait.Append($@"<div>{particip}</div>");
                        // Теперь отражения
                        var reflect = tree.GetInverse("http://fogid.net/o/reflected")
                            .Select(p =>
                            {
                                var dir = p.GetDirect("http://fogid.net/o/in-doc");
                                if (dir == null) return new string[] { "", "", "", "" };
                                string doctyp = dir.Tp;
                                string? docname = dir.GetText("http://fogid.net/o/name");
                                string? uri = dir.GetStr("http://fogid.net/o/uri");
                                string? dates = dir.GetDates();
                                return new string[] { doctyp, docname ?? "", uri ?? "", dates ?? "", dir.Id ?? "" };
                            })
                            .OrderBy(dud => dud[3])
                            .Select(dud =>
                            {
                                if (db.ontology.DescendantsAndSelf("http://fogid.net/o/document").Contains(dud[0]))
                                {
                                    //string ttip = (Doctipnames())[dud[0]];
                                    string name = dud[1];
                                    if (string.IsNullOrEmpty(name)) name = "noname";
                                    if (name.Length > 24) name = name.Substring(0, 24);
                                    return Kvad(dud[0], dud[4], name, dud[2], dud[3]);
                                }
                                else return "";
                            })
                            .Aggregate("", (sum, s) => sum + " " + s);
                        portrait.Append($@"<div class='container'>{reflect}</div>");
                    }
                    else if (tip == "http://fogid.net/o/org-sys")
                    {
                        // Есть обратные: члены и отражения
                        var particip = tree.GetInverse("http://fogid.net/o/in-org")
                            .Select(p =>
                            {
                                var dir = p.GetDirect("http://fogid.net/o/participant");
                                if (dir == null) return "";
                                string? r = p.GetText("http://fogid.net/o/role");
                                string role = r == null ? "участник" : r;
                                string? pname = dir.GetText("http://fogid.net/o/name");
                                string? dtpart = p.GetDates();
                                string? dates = dtpart != null ? dtpart : dir.GetDates();
                                return $"<div><a href='/view/{dir.Id}'> {pname} </a>  <span style='font-size:smaller;'>{dates ?? ""}</span></div>";
                            }).Aggregate("", (sum, s) => sum + " " + s);
                        portrait.Append($@"<div>{particip}</div>");
                        // Теперь отражения
                        var reflect = tree.GetInverse("http://fogid.net/o/reflected")
                            .Select(p =>
                            {
                                var dir = p.GetDirect("http://fogid.net/o/in-doc");
                                if (dir == null) return new string[] { "", "", "", "" };
                                string doctyp = dir.Tp;
                                string? docname = dir.GetText("http://fogid.net/o/name");
                                string? uri = dir.GetStr("http://fogid.net/o/uri");
                                string? dates = dir.GetDates();
                                return new string[] { doctyp, docname ?? "", uri ?? "", dates ?? "", dir.Id ?? "" };
                            })
                            .OrderBy(dud => dud[3])
                            .Select(dud =>
                            {
                                if (db.ontology.DescendantsAndSelf("http://fogid.net/o/document").Contains(dud[0]))
                                {
                                    //string ttip = (Doctipnames())[dud[0]];
                                    string name = dud[1];
                                    if (string.IsNullOrEmpty(name)) name = "noname";
                                    if (name.Length > 24) name = name.Substring(0, 24);
                                    return Kvad(dud[0], dud[4], name, dud[2], dud[3]);
                                }
                                else return "";
                            })
                            .Aggregate("", (sum, s) => sum + " " + s);
                        portrait.Append($@"<div class='container'>{reflect}</div>");
                    }
                    else if (tip == "http://fogid.net/o/collection" || tip == "http://fogid.net/o/cassette")
                    {
                        // Элементы этой коллекции
                        var elements_of_collection = tree.GetInverse("http://fogid.net/o/in-collection")
                            .Select(me =>
                            {
                                var ci = me.GetDirect("http://fogid.net/o/collection-item");
                                if (ci == null) return null;
                                string? name = ci.GetText("http://fogid.net/o/name");
                                string? uri = ci.GetStr("http://fogid.net/o/uri");
                                string? dates = ci.GetDates();
                                string url = "";
                                string fill = "auto";

                                string[] idtna = new string[] { ci.Id ?? "", ci.Tp, name ?? "noname", uri, dates ?? "" };
                                return idtna;
                            })
                            .Where(x => x != null)
                            .Select(idtna =>
                            {

                                return idtna == null ? "" :
                                Kvad(idtna[1], idtna[0], idtna[2], idtna[3], idtna[4]);
                            })
                            .Aggregate("", (sum, s) => sum + " " + s);
                        portrait.Append($"<div class='container'>{elements_of_collection}</div>");
                    }
                    else if (db.ontology.DescendantsAndSelf("http://fogid.net/o/document").Contains(tip))
                    {
                        // Здесь будут учитываться поля и следующие обратные отношения: reflection, collection-member

                        //string tip = tree.Tp; // уже было
                        string? name = tree.GetText("http://fogid.net/o/name");
                        string? date = tree.GetStr("http://fogid.net/o/from-date");
                        string? uri = tree.GetStr("http://fogid.net/o/uri");
                        string docview = "<div>" + (tip == "http://fogid.net/o/photo-doc" ?
                            $"<img src='/photo?uri={uri}&size=medium'/>" :
                            (tip == "http://fogid.net/o/video-doc" ?
                                $"<video src='/video?uri={uri}' controls/>" :
                                (tip == "http://fogid.net/o/audio-doc" ?
                                    $"<audio src='/audio?uri={uri}' controls/>" :
                                    (tip == "http://fogid.net/o/document" ?
                                        $"<a href='/document?uri={uri}'>читать документ</a>" :
                            $"Здесь должен быть контент документа {tree.Id} типа {tip}"))));
                        portrait.Append(docview);
                        portrait.Append($"<div><span style='font-size:small;'>{name}</span> {date}</div>");


                        // Отражения
                        var reflections = tree.GetInverse("http://fogid.net/o/in-doc")
                            .Select(x =>
                            {
                                var pers = x.GetDirect("http://fogid.net/o/reflected");
                                if (pers == null) return null;
                                string[] nada = new string[] { pers.Id, pers.GetText("http://fogid.net/o/name") ?? "noname" };
                                return nada;
                            })
                            .Where(na => na != null)
                            //.OrderBy(na => na[1] ?? "")
                            .Select(na =>
                            {
                                return $"<div><a href='/view/{na[0]}'>{na[1]}</a></div>";
                            })
                            .Aggregate("", (sum, te) => sum + te);

                        portrait.Append($"<div>{reflections}</div>");
                    }
                    break;
                }
            }
            return portrait.ToString();
        }

    }
}
