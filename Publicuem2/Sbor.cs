using System.Collections.Immutable;
using System.Xml.Linq;

using Factograph.Data;
using Factograph.Data.r;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Publicuem2
{
    public class Sbor
    {
        private static string[] typs = new string[0];
        public static HtmlResult CreateHtml(Factograph.Data.IFDataService db, HttpRequest request)
        {
            var id = request.Query["id"];
            string? searchstring = request.Query["searchstring"];
            string? typ = request.Query["typ"];
            bool bywords = string.IsNullOrEmpty(request.Query["bywords"]) ? false : true;
            if (typs.Length == 0) typs = db.ontology.DescendantsAndSelf("http://fogid.net/o/sys-obj").ToArray();

            RRecord[] searchvariants = new RRecord[0];
            if (!string.IsNullOrEmpty(searchstring))
            {
                searchvariants = db.SearchRRecords(searchstring, false).ToArray();
            }
            RRecord? record = null;
            XElement? xportrait = null;
            if (!string.IsNullOrEmpty(id))
            {
                record = db.GetRRecord(id, true);
                if (record != null) { xportrait = BuildXPortrait(record, db); }
            }

            string options = typs.Select(t => "<option value='" + t + "' "+ (t == typ? "selected " : "") +">" + db.ontology.LabelOfOnto(t) + "</option>")
                .Aggregate((s, el) => s + " " + el);
            string chked = bywords ? "checked" : "";
            var variants = new XElement("div", searchvariants.Select(variant => new XElement("div",
                        db.ontology.LabelOfOnto(variant.Tp),
                        new XText(" "),
                        new XElement("a", new XAttribute("href", "?id=" + variant.Id), variant.GetName())
                        )));

            string html =
$@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <link rel='stylesheet' type='text/css' href='StyleSheet.css'/>
</head>
<body>
<h1>Фактограф-просмотр</h1>
<form method='get' action=''>
    <input type='text' name='searchstring' value='{searchstring ?? ""}'/>
    <select name='typ'>
        {options}
    </select>
    <input type='checkbox' name='bywords' {chked}/>
    <input type='submit' value='искать'/>
</form>
{ variants?.ToString() ?? "" }
{ xportrait?.ToString() ?? ""}
</body>
</html>
";
            return new HtmlResult(html);
        }
        private static XElement BuildXPortrait(RRecord rrec, Factograph.Data.IFDataService db)
        {
            XElement? prolog = null;
            // Построение визуализации (пролога) документа, если это документ
            if (db.ontology.AncestorsAndSelf(rrec.Tp).Contains("http://fogid.net/o/document"))
            {
                string? uri = rrec.GetField("http://fogid.net/o/uri");
                string? docmetainfo = rrec.GetField("http://fogid.net/o/docmetainfo");
                string? documenttype = docmetainfo?.Split(';')
                                            .FirstOrDefault(s => s.StartsWith("documenttype:"))?.Substring("documenttype:".Length); ;
                if (uri != null && docmetainfo != null && documenttype != null)
                {
                    if (rrec.Tp == "http://fogid.net/o/photo-doc")
                    {
                        prolog = new XElement("img", new XAttribute("src", "docs?u=" + uri + "&s=normal"));
                    }
                    else if (rrec.Tp == "http://fogid.net/o/video-doc")
                    {
                        string sr = "docs?action=getvideo&u=" + uri + "&s=medium";
                        prolog = new XElement("video", new XAttribute("autoplay", "true"), new XAttribute("controls", "controls"),
                            new XElement("source",
                                new XAttribute("width", "480"), new XAttribute("type", "video/mp4"), new XAttribute("src", sr)));
                    }
                    else if (rrec.Tp == "http://fogid.net/o/audio-doc")
                    {
                        string sr = "docs?action=getaudio&u=" + uri;
                        prolog = new XElement("audio", new XAttribute("controls", "controls"),
                            new XElement("source", new XAttribute("type", "audio/mpeg"), new XAttribute("src", sr)));
                    }
                    else if (rrec.Tp == "http://fogid.net/o/document")
                    {
                        if (documenttype == "application/pdf")
                        {
                            string sr = "docs?action=getpdf&u=" + uri;
                            prolog = new XElement("embed", new XAttribute("width", "900"), new XAttribute("height", "700"),
                                new XAttribute("type", "application/pdf"), new XAttribute("src", sr));
                        }
                        else if (docmetainfo != null && docmetainfo.Contains("application/fog"))
                        {   // Это FOG - он недоступен
                        }
                        else if (docmetainfo != null) // Все типы документов кроме .fog
                        {   // Другие документы с docmetainfo
                            string sr = "docs/GetDoc?u=" + uri;
                            var mime = docmetainfo.Split(';')
                                    .FirstOrDefault(s => s.StartsWith("documenttype:"))?.Substring("documenttype:".Length);
                            //< div > Документ[@(mime)] получить копию: </ div >
                            //< div style = "margin-bottom:10px;" >< a href = "@sr" >< img src = "icons/document_m.jpg" /></ a ></ div >
                            prolog = new XElement("div", "Документ[" + (mime) + "]  получить копию:",
                                new XElement("a", new XAttribute("href", sr), "load"));
                        }
                    }
                }
            }
            var shablon = Rec.GetUniShablon(rrec.Tp, 2, null, db.ontology);
            Rec tree = Rec.Build(rrec, shablon, db.ontology, idd => db.GetRRecord(idd, false));
            //Pro[] dtable = tree.Props.Where(prop => !(prop is Inv)).ToArray();
            XElement? xtable = OneTable(db, new Rec[] { tree }, null);

            return new XElement("div",
                new XElement("div", db.ontology.LabelOfOnto(tree.Tp) + " " + tree.Id),
                prolog,
                new XElement("table",
                new XElement("tr", new XElement("td", new XAttribute("colspan", "2"), xtable)),
                tree.Props.Where(prop => prop is Inv).Cast<Inv>()
                    .Where(iprop => iprop.Sources.Length > 0)
                    .Select(prop => new XElement("tr",
                        new XElement("td", db.ontology.LabelOfOnto(prop.Pred),
                        new XAttribute("valign", "top")),
                        new XElement("td",
                            prop.Pred == "http://fogid.net/o/in-collection" || prop.Pred == "http://fogid.net/o/reflected" ?
                                (new Canvas()).Build(db, prop.Sources, prop.Pred) :
                                OneTable(db, prop.Sources, prop.Pred)
                                ))))); //TODO: Надо вставить полотно

        }
        private static XElement? OneTable(IFDataService db, Rec[] recs, string? forbidden)
        {
            if (recs.Length == 0 || recs[0] == null) return null;
            Pro[] dtable = recs[0].Props.Where(prop => !(prop is Inv)).ToArray();
            // Еще надо не пропускать uri и docmetainfo
            var query = dtable.Where(prop => !(prop is Dir && prop.Pred == forbidden)
                && prop.Pred != "http://fogid.net/o/uri" && prop.Pred != "http://fogid.net/o/docmetainfo");
            return new XElement("table", new XAttribute("class", "s"),
                new XElement("tr",
                query.Select(prop => new XElement("th", new XAttribute("class", "s"),
                db.ontology.LabelOfOnto(prop.Pred)))),
                recs.Where(rec => rec != null).Select(rec => new XElement("tr",
                // Не пропускать uri и docmetainfo
                rec.Props.Where(prop => !(prop is Inv)
                    && prop.Pred != "http://fogid.net/o/uri" && prop.Pred != "http://fogid.net/o/docmetainfo")
                .Select(prop =>
                {
                    if (prop is Str)
                    {
                        Str s = (Str)prop;
                        return new XElement("td", new XAttribute("class", "s"), s.Value);
                    }
                    else if (prop is Tex)
                    {
                        Tex t = (Tex)prop;
                        return new XElement("td", new XAttribute("class", "s"), t.Values.Select(tl => new XElement("span", tl.Text + "^" + tl.Lang)));
                    }
                    else if (prop is Dir && ((Dir)prop).Resources.Length > 0)
                    {
                        Rec resource = ((Dir)prop).Resources.First();
                        if (resource == null)
                        {
                            return new XElement("td", new XAttribute("class", "s"));
                        }
                        return new XElement("td", new XAttribute("class", "s"), new XElement("a", new XAttribute("href", "?id=" + resource.Id),
                            resource.GetText("http://fogid.net/o/name")));
                    }
                    return new XElement("td", new XAttribute("class", "s"), "");
                }))));
        }
        public static HtmlResult Reload(Factograph.Data.IFDataService db, HttpRequest request)
        {
            db.Reload();
            return new HtmlResult(
"<html><head></head><body><h1>Reload OK!</h1><a href='/'>Start</a></body></html>");
        }
    }
}
