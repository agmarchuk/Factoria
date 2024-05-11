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
        public static void Main()
        {
            Console.WriteLine("Start CassConsoleApp");
            //Process();

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

Console.WriteLine("Products");
Console.WriteLine("----------------------");

 OneFog fog = new OneFog(@"D:\Home\FactographProjects\t1\meta\t1_current.fog");
 foreach (var pair in fog.FogAttributes())
 {
    //Console.WriteLine($"{pair.Key}=>{pair.Value}");
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
 OneFog fog2 = new OneFog(@"D:\Home\FactographProjects\t1\meta\t1_current.fog");
 foreach (var pair in fog.FogAttributes())
 {
    //Console.WriteLine($"{pair.Key}=>{pair.Value}");
 }

    XElement xdoc = XElement.Parse(xdocroot);
    foreach (XElement rec in fog2.Records())
    {
        //xdoc.Add(rec);
        string localname = rec.Name.LocalName;
        string? id = rec.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about")?.Value;
        if (id == null) { continue; }

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

            xdoc.Save(@"D:\Home\FactographProjects\t1\meta\t1_current_.fog");
 
}    



    }




}

