using Factograph.Data.r;
using Factograph.Data;
using System.Xml.Linq;
using System;
using static System.Net.WebRequestMethods;

namespace Publicuem2
{
    public class Components
    {
    }
    /// <summary>
    /// Создает полотно с фотками и иконками, соответствующими обр(prop) - collection-element - inv(prop) - документ
    /// </summary>
    public class Canvas
    {
        public Canvas() {  }
        public XElement? Build(IFDataService db, Rec[] recs, string proppred)
        {
            string? altprop = proppred == "http://fogid.net/o/in-collection" ? "http://fogid.net/o/collection-item" :
                (proppred == "http://fogid.net/o/reflected" ? "http://fogid.net/o/in-doc" : 
                (proppred == "http://fogid.net/o/inDocument" ? "http://fogid.net/o/partItem" : null));
            return new XElement("div", new XAttribute("style", "display: flex; flex-wrap: wrap;"),
                recs.Select((r, nom) => 
                {
                    // Находим внешнюю ссылку на документ
                    if (r == null || altprop == null) 
                        return (new Brick()).Build("icons/medium/default_m.jpg", "r == null || altprop == null", "", "");
                    var propdir = r.GetDirect(altprop);
                    //string text = propdir==null? "null" : " " + propdir.Tp + " " + propdir.Id;
                    string photoURL = "nourl";
                    string refURL = "norefurl";
                    string? name = "noname";
                    string? date = null;
                    string? uri = null;
                    string? docmetainfo = null;
                    if (propdir == null)
                        return (new Brick()).Build("icons/medium/default_m.jpg", "ropdir == null", "", "");
                    RRecord? rrec = db.GetRRecord(propdir.Id, false);
                    if (rrec != null)
                    {
                        uri = rrec.GetField("http://fogid.net/o/uri");
                        docmetainfo = rrec.GetField("http://fogid.net/o/docmetainfo");
                        refURL = "?id=" + propdir.Id;
                        name = rrec.GetField("http://fogid.net/o/name");
                        if (name == null) name = "noname";
                        date = rrec.GetField("http://fogid.net/o/from-date");
                    }
                    if (uri != null && propdir != null && propdir.Tp == "http://fogid.net/o/photo-doc") 
                    {
                        photoURL = "docs?u=" + uri + "&s=small";
                    }
                    else if (uri != null && propdir != null && propdir.Tp == "http://fogid.net/o/video-doc")
                    {
                        photoURL = "icons/medium/sq200x200vi.jpg";
                    }
                    else if (uri != null && propdir != null && propdir.Tp == "http://fogid.net/o/audio-doc")
                    {
                        photoURL = "icons/medium/sq200x200au.jpg";
                    }
                    else if (propdir != null && db.ontology.DescendantsAndSelf("http://fogid.net/o/collection").Contains(propdir.Tp))
                    {
                        photoURL = "icons/medium/sq200x200co.jpg";
                    }
                    else if (uri != null && propdir != null && propdir.Tp == "http://fogid.net/o/document"
                        && docmetainfo != null && docmetainfo.Split(';').Any(part => part.StartsWith("documenttype:application/pdf")))
                    {
                        photoURL = "icons/medium/sq200x200pd.jpg";
                    }
                    else
                    {
                        photoURL = "icons/medium/sq200x200do.jpg";
                    }
                    return (new Brick()).Build(photoURL, refURL, name, date ?? "");
                }));
        }
    }
    public class Brick
    {
        public Brick() { }
        public XElement? Build(string photoURL, string refURL, string name, string epilog)
        {
            string nam = name ?? "noname";
            if(nam.Length > 20) { nam = nam.Substring(0, 20) + "..."; }
            string reference = refURL;
            return new XElement("div",
                new XAttribute("style",
                    "width:200px;height:200px; display:flex;flex-direction: column; background:lightgrey;margin:4px;text-align:center;" +
                    "background: url('" + photoURL + "') center no-repeat;" +
                    "background-color:#d9dabb; background-size:contain;"
                    ),
                new XElement("div", new XAttribute("style", "margin: auto;height:100%;"),
                    new XElement("a", new XAttribute("href", reference),
                        new XAttribute("style", "align-content:center; color:black; background-color: #d9dabb; background-size:contain; "),
                        new XAttribute("title", name),
                        nam)),
                new XElement("div", new XAttribute("style", "margin: auto;height:min-content;font-size:smaller;"),
                    new XElement("span", new XAttribute("style", "text-align: center; color:black; background-color: #d9dabb; background-size:contain; "),
                        epilog))
                );
        }
    }
    

}
