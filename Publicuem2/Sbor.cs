using System.Xml.Linq;

using Factograph.Data;
using Factograph.Data.r;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Publicuem2
{
    public class Sbor
    {
        public static HtmlResult CreateHtml(Factograph.Data.IFDataService db, HttpRequest request)
        {
            var id = request.Query["id"];
            string? searchstring = request.Query["searchstring"];

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
            XElement xhtml = new XElement("html", 
                new XElement("head", new XElement("meta", new XAttribute("charset", "utf-8"))),
                new XElement("body", 
                    new XElement("a", new XAttribute("href", "?id=syp2001-p-marchuk_a"), "syp2001-p-marchuk_a"),
                    new XElement("a", new XAttribute("href", "?id=collection_PA_videoOfficial-old"), "Ошибка"),
                    new XElement("h1", "Hello Привет!"),
                    new XElement("form", new XAttribute("method", "get"), new XAttribute("action", ""),
                        new XElement("input", new XAttribute("name", "searchstring"), new XAttribute("value", searchstring??"")),
                        null),
                    searchvariants.Select(variant => new XElement("div", 
                        db.ontology.LabelOfOnto(variant.Tp),
                        new XText(" "),
                        new XElement("a", new XAttribute("href", "?id="+variant.Id), variant.GetName())
                        )),
                    xportrait,
                    null));
            return new HtmlResult(xhtml.ToString());
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
                if ( uri != null && docmetainfo != null && documenttype != null)
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
                //        string? docmetainfo = ((Str?)tree.Props.FirstOrDefault(p => p.Pred == "http://fogid.net/o/docmetainfo"))?.Value;
                //        if (docmetainfo != null && docmetainfo.Contains("application/pdf"))
                //        {   // Это PDF - его надо напечатать
                //            hasdocimage = true;
                //            string sr = "docs/GetPdf?u=" + uri;
                //    < embed src = "@sr" width = "100%" height = "800" />
                //        }
                //        else if (docmetainfo != null && docmetainfo.Contains("application/fog"))
                //        {   // Это FOG - он недоступен
                //        }
                //        else if (docmetainfo != null) // Все типы документов кроме .fog
                //        {   // Другие документы с docmetainfo
                //            string sr = "docs/GetDoc?u=" + uri;
                //            var mime = docmetainfo.Split(';')
                //                    .FirstOrDefault(s => s.StartsWith("documenttype:"))?.Substring("documenttype:".Length);
                //< div > Документ[@(mime)] получить копию: </ div >
                //< div style = "margin-bottom:10px;" >< a href = "@sr" >< img src = "icons/document_m.jpg" /></ a ></ div >
                //        }
                    }
                }
            }
            var shablon = Rec.GetUniShablon(rrec.Tp, 2, null, db.ontology);
            Rec tree = Rec.Build(rrec, shablon, db.ontology, idd => db.GetRRecord(idd, false));
            //Pro[] dtable = tree.Props.Where(prop => !(prop is Inv)).ToArray();
            XElement? xtable = OneTable(db, new Rec[] { tree }, null);

            var q1 = tree.Props.Where(prop => prop is Inv).Cast<Inv>()
                    .Where(iprop => iprop.Sources.Length > 0).ToArray();
            foreach (var prop in q1)
            {
                var qq2 = OneTable(db, prop.Sources, prop.Pred);
            }
            return new XElement("div", 
                prolog,
                new XElement("table", new XAttribute("border", "1"),
                new XElement("tr", new XElement("td", new XAttribute("colspan", "2"), xtable)),
                tree.Props.Where(prop => prop is Inv).Cast<Inv>()
                    .Where(iprop => iprop.Sources.Length > 0)
                    .Select(prop => new XElement("tr", new XElement("td", db.ontology.LabelOfOnto(prop.Pred), new XAttribute("valign", "top")), new XElement("td", OneTable(db, prop.Sources, prop.Pred)))))); //TODO: Надо вставить полотно
               
        }

        private static XElement? OneTable(IFDataService db, Rec[] recs, string? forbidden)
        {
            if (recs.Length == 0 || recs[0] == null) return null; 
            Pro[] dtable = recs[0].Props.Where(prop => !(prop is Inv)).ToArray();
            var query = dtable.Where(prop => !(prop is Dir && prop.Pred == forbidden));
            return new XElement("table", new XAttribute("border", "0"),
                new XElement("tr",
                query.Select(prop => new XElement("td", db.ontology.LabelOfOnto(prop.Pred)))),
                recs.Where(rec => rec != null).Select(rec => new XElement("tr",
                rec.Props.Where(prop => !(prop is Inv)).Select(prop =>
                {
                    if (prop is Str)
                    {
                        Str s = (Str)prop;
                        return new XElement("td", s.Value);
                    }
                    else if (prop is Tex)
                    {
                        Tex t = (Tex)prop;
                        return new XElement("td", t.Values.Select(tl => new XElement("span", tl.Text + "^" + tl.Lang)));
                    }
                    else if (prop is Dir && ((Dir)prop).Resources.Length > 0)
                    {
                        Rec resource = ((Dir)prop).Resources.First();
                        if (resource == null) 
                        {
                            return new XElement("td");
                        }
                        return new XElement("td", new XElement("a", new XAttribute("href", "?id=" + resource.Id),
                            resource.GetText("http://fogid.net/o/name")));
                    }
                    return new XElement("td", "");
                }))));
        }
    }
}
