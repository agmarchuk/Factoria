using System.Xml.Linq;

namespace FogCorrections
{
public class Program
{
    public static void Main(string[] pars)
    {
        Console.WriteLine("Start Correct Fogs");
        // последовательно перебираю файлы и корректирую, если есть чего
        foreach (string name in pars)
        {
            if (!name.ToLower().EndsWith(".fog")) continue;
            DoCorrection(name);
        }
        //DoCorrection(@"C:\Home\FactographProjects\t20\meta\t20_current.fog");
    }
    const string rdfns = "{http://www.w3.org/1999/02/22-rdf-syntax-ns#}";
    const string fogi = "{http://fogid.net/o/}";
    const string rdfabout = rdfns + "about";
    const string rdfresource = rdfns + "resource";
    private static void DoCorrection(string fname)
    {
        XElement xfog = XElement.Load(fname);
        // сформируем болванку
        XElement xout = XElement.Parse(
@"<?xml version='1.0' encoding='utf-8'?>
<rdf:RDF 
    xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#'
    xmlns='http://fogid.net/o/'
    > 
 </rdf:RDF>"
        );
        // Добавим нужные атрибуты
        foreach (XAttribute att in xfog.Attributes())
        {
            if (
                att.Name.ToString() == "owner" ||
                att.Name.ToString() == "prefix" ||
                att.Name.ToString() == "counter" ||
                att.Name.ToString() == "uri" 
            ) xout.Add(new XAttribute(att.Name, att.Value));
        }
        bool changed = false;
        // Сканируем элементы и проверяем их
        foreach (XElement element in xfog.Elements())
        {
            xout.Add(ConvertXElement(element));
        }

        File.Move(fname, fname+".b");
        xout.Save(fname);
    }
// C:\Home\dev2024\Factoria\CorrectFogs\bin\Debug\net7.0\CorrectFogs.exe
    private static string ConvertId(string id) { if (id.Contains('|')) return id.Replace("|", ""); else return id; }

    public static Func<XElement, XElement> ConvertXElement = xel =>
    {
        if (xel.Name == "delete" || xel.Name == fogi + "delete") return new XElement(fogi + "delete",
            xel.Attribute("id") != null ?
                new XAttribute(rdfabout, ConvertId(xel.Attribute("id").Value)) :
                new XAttribute(xel.Attribute(rdfabout)),
            xel.Attribute("mT") == null ? null : new XAttribute(xel.Attribute("mT")));
        else if (xel.Name == "substitute" || xel.Name == fogi + "substitute") return new XElement(fogi + "substitute",
            new XAttribute("old-id", ConvertId(xel.Attribute("old-id").Value)),
            new XAttribute("new-id", ConvertId(xel.Attribute("new-id").Value)));
        else
        {
            string id = ConvertId(xel.Attribute(rdfabout).Value);
            XAttribute mt_att = xel.Attribute("mT");
            XElement iisstore = xel.Element("iisstore");
            if (iisstore != null)
            {
                var att_uri = iisstore.Attribute("uri");
                var att_contenttype = iisstore.Attribute("contenttype");
                string docmetainfo = iisstore.Attributes()
                    .Where(at => at.Name != "uri" && at.Name != "contenttype")
                    .Select(at => at.Name + ":" + at.Value.Replace(';', '|') + ";")
                    .Aggregate((sum, s) => sum + s);
                iisstore.Remove();
                if (att_uri != null) xel.Add(new XElement("uri", att_uri.Value));
                if (att_contenttype != null) xel.Add(new XElement("contenttype", att_contenttype.Value));
                if (docmetainfo != "") xel.Add(new XElement("docmetainfo", docmetainfo));
            }
            XElement xel1 = new XElement(fogi + xel.Name.LocalName, 
                new XAttribute(rdfabout, ConvertId(xel.Attribute(rdfabout).Value)),
                mt_att == null ? null : new XAttribute("mT", mt_att.Value),
                xel.Elements()
                .Where(sub => sub.Name.LocalName != "iisstore")
                .Select(sub => new XElement(fogi + sub.Name.LocalName,
                    sub.Value,
                    sub.Attributes()
                    .Select(att => att.Name == rdfresource ?
                        new XAttribute(rdfresource, ConvertId(att.Value)) :
                        new XAttribute(att)))));
            return xel1;
        }
        //return null;
    };

}
}
