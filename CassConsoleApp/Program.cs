using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Linq;
using static System.Net.WebRequestMethods;

namespace CassConsoleApp
{
    public class Program
    {
        /// <summary>
        /// Первый аргумент расположение кассеты, параметры превью находятся в finfo-файле
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            Console.WriteLine("Usage: CassConsoleApp cassette_path");
            string casspath = args.Length > 0 ? args[0] : @"D:\Home\FactographProjects\t2";
            string cass = casspath.Split('/', '\\').Last();
            string cassname = casspath + "/meta/" + cass + "_current.fog";
            string backupname = cassname + ".fog";
            if (!System.IO.File.Exists(backupname)) System.IO.File.Move(cassname, backupname);
            // Заведем командный файл преобразований
            if (System.IO.File.Exists(casspath + "/convert.bat")) 
                System.IO.File.Delete(casspath + "/convert.bat");
            FileStream convert = new FileStream(casspath + "/convert.bat", FileMode.Append, FileAccess.Write);
            System.IO.TextWriter writer = new System.IO.StreamWriter(convert);
            // Типоразмеры
            XElement finfo = XElement.Parse(
@"<?xml version='1.0' encoding='utf-8'?>
<finfo>
  <image>
    <small previewBase='200' qualityLevel='90' />
    <medium previewBase='480' qualityLevel='90' />
    <normal previewBase='900' qualityLevel='90' />
  </image>
  <video>
    <medium videoBitrate='400K' audioBitrate='22050' rate='10' framesize='384x288' previewBase='600' />
  </video>
</finfo>");

            Func<XmlReader, bool> GetProperty = reader =>
{ 
    string ns = reader.NamespaceURI;
    string locname = reader.LocalName;
    Console.WriteLine($"{ns}//{locname}");
    if (reader.HasAttributes) 
    {
        while (reader.MoveToNextAttribute()) {
            Console.WriteLine("Att {0}={1} {2}", reader.Name, reader.Value, reader.NamespaceURI);
        }
    // Move the reader back to the element node.
    //reader.MoveToElement();
    while (reader.Read())
    {
        if (reader.NodeType == XmlNodeType.Text) 
        {
    Console.WriteLine("text {0}={1} {2} {3}", reader.Name, reader.Value, reader.NamespaceURI, reader.Depth);
        }
        else if (reader.NodeType == XmlNodeType.EndElement) 
        {
            break;
        }
    }

    }
    return true;
};
Func<XmlReader, bool> GetRecord = reader =>
{ 
    string ns = reader.NamespaceURI;
    string locname = reader.LocalName;
    Console.WriteLine($"{ns}//{locname}");
    if (reader.HasAttributes) 
    {
        while (reader.MoveToNextAttribute()) {
            Console.WriteLine("Att {0}={1} {2}", reader.Name, reader.Value, reader.NamespaceURI);
        }
    // Move the reader back to the element node.
    //reader.MoveToElement();
    while (reader.Read())
    {
        if (reader.NodeType == XmlNodeType.Element) 
        {
            var ok = GetProperty(reader);
        }
        else if (reader.NodeType == XmlNodeType.EndElement) 
        {
            break;
        }
    }

    }
    return true;
};

 OneFog fog = new OneFog(backupname);
 Dictionary<string, string> attributes = new Dictionary<string, string>();
 foreach (var pair in fog.FogAttributes())
 {
    Console.WriteLine($"Fog attribute: {pair.Key}=>{pair.Value}");
    attributes.Add(pair.Key, pair.Value);
 }

  foreach (XElement rec in fog.Records())
  {
    Console.WriteLine(rec.ToString());
  }
    fog.Close();

            Console.WriteLine("================================================");

            string rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
            string xdocroot = @"<?xml version='1.0' encoding='utf-8'?>
<rdf:RDF xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#' 
xmlns='http://fogid.net/o/'></rdf:RDF>";
            OneFog fog2 = new OneFog(backupname);
            //foreach (var pair in fog.FogAttributes())
            //{
            //    //Console.WriteLine($"{pair.Key}=>{pair.Value}");
            //}

            XElement xdoc = XElement.Parse(xdocroot);
            // Добавлю атрибуты uri, owner, prefix, counter
            xdoc.Add(
                new XAttribute("uri", attributes["uri"]),
                new XAttribute("owner", attributes["owner"]),
                new XAttribute("prefix", attributes["prefix"]),
                new XAttribute("counter", attributes["counter"]),
                null);
            // Теперь добавим преобразованные записи
            foreach (XElement rec in fog2.Records())
            {
                //xdoc.Add(rec);
                string localname = rec.Name.LocalName;
                string? id = rec.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about")?.Value;
                int width = 1440, height = 1080;
                if (id == null) { continue; }
                string? documenttype = null;

                // Если localname это photo-doc - надо для каждого типоразмера вычислять коэффициент
                // масштабирования и помещать команду вычисления превьюшки в поток команд.  
                if (localname == "photo-doc") 
                { 
                    string? uri = rec.Element(XName.Get("uri", "http://fogid.net/o/"))?.Value;
                    if (uri != null) 
                    {
                        string? docmetainfo = rec.Element(XName.Get("docmetainfo", "http://fogid.net/o/"))?.Value;
                        if (docmetainfo != null)
                        {
                            string[] infos = docmetainfo.Split(';');
                            foreach (string info in infos) 
                            {
                                if (info.StartsWith("width:"))
                                    width = Int32.Parse(info.Substring("width:".Length));
                                else if (info.StartsWith("height:"))
                                    height = Int32.Parse(info.Substring("height:".Length));
                                else if (info.StartsWith("documenttype:"))
                                    documenttype = info.Substring("documenttype:".Length);
                            }
                            // Вычислим фактор "увеличения" для каждого типоразмера
                            int more = width > height ? width : height;
                            string? pBs = finfo.Element("image")?.Element("small")?
                                .Attribute("previewBase")?.Value;
                            string? pBm = finfo.Element("image")?.Element("medium")?
                                .Attribute("previewBase")?.Value;
                            string? pBn = finfo.Element("image")?.Element("normal")?
                                .Attribute("previewBase")?.Value;
                            if (pBs != null && pBm != null && pBn != null)
                            {
                                double factor_small = (double)Int32.Parse(pBs) / (double)more;
                                double factor_medium = (double)Int32.Parse(pBm) / (double)more;
                                double factor_normal = (double)Int32.Parse(pBn) / (double)more;
                                // используем uri для вычисления позиций 
                                // <uri>iiss://t2@iis.nsk.su/0001/0001/0003</uri>
                                // Берем casspath, к этому мы добавляем последние 10 символов и
                                // добавляем файловое расширение. Где его взять - непонятно. Попробую
                                // взять в списке атрибутов documenttype 
                                string ext = documenttype.Substring(documenttype.LastIndexOf('/') + 1);
                                // Документ-оригинал:
                                string original = casspath + "/originals" + uri.Substring(uri.Length - 10) + "." + ext;
                                // Превьюшки:
                                string small = casspath + "/documents/small" + uri.Substring(uri.Length - 10) + ".jpg";
                                string medium = casspath + "/documents/medium" + uri.Substring(uri.Length - 10) + ".jpg";
                                string normal = casspath + "/documents/normal" + uri.Substring(uri.Length - 10) + ".jpg";

                                // Пишем в командный файл
                                writer.WriteLine("PATH/magick.exe " + original + 
                                    " -resize " + (factor_small * 100) + "% " +
                                    small);
                            }

                        }
                    }
                }
                xdoc.Add(new XElement(XName.Get(localname, "http://fogid.net/o/"),
                    new XAttribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about", id),
                    rec.Elements().Select(el =>
                    {
                        string pred = el.Name.LocalName;
                        var att = el.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}resource");
                        if (att == null)
                        {
                            return new XElement(XName.Get(pred, "http://fogid.net/o/"), new XText(el.Value));
                        }
                        else
                        {
                            return new XElement(XName.Get(pred, "http://fogid.net/o/"),
                                new XAttribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}resource", att.Value));
                        }
                    })));
            }
            Console.WriteLine(xdoc.ToString());

            xdoc.Save(cassname);
            convert.Close();
        }



    }




}

