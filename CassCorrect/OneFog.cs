using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Factograph.Docs
{
    public class OneFog
    {
        private XmlReader reader;
        private string filename;
        private Dictionary<string, string> fogAttributes = new Dictionary<string, string>();
        private const string ns0 = "http://fogid.net/o/";

        public OneFog(string filename) 
        {
            this.filename = filename;
            XmlReaderSettings settings = new XmlReaderSettings();
            //settings.DtdProcessing = DtdProcessing.Parse;
            reader = XmlReader.Create(filename, settings);

            bool ok = false;
            while (reader.Read()) {
                switch (reader.NodeType) 
                {
                case XmlNodeType.Element:
                    string ns = reader.NamespaceURI;
                    string locname = reader.LocalName;
                    if (ns != "http://www.w3.org/1999/02/22-rdf-syntax-ns#" ||
                        locname != "RDF") Console.WriteLine($"!!!!Error in {filename}: not fog file");
                    
                    // Display all attributes.
                    if (reader.HasAttributes) 
                    {
                        while (reader.MoveToNextAttribute()) {
                            fogAttributes.Add(reader.Name, reader.Value);
                        }
                    }
                    ok = true;
                    break; 
                    default: break;
                }
                if (ok) break;
            }
        // Move the reader back to the element node.
            //reader.MoveToElement();
        }
        public void Close() { reader.Close(); }
        public Dictionary<string, string> FogAttributes()
        {
            return fogAttributes;
        }
        public IEnumerable<XElement> Records()
        {
            XElement record = new XElement("unknown");
            XElement? property = null;
            // Parse the file
            while (reader.Read()) {
                switch (reader.NodeType) {
                case XmlNodeType.Element:
                    string ns = reader.NamespaceURI;
                        if (ns == "") { ns = ns0; }
                    string locname = reader.LocalName;
                    if (reader.Depth == 1)
                    {
                        record = new XElement(XName.Get(locname, ns));
                        property = null; 
                    }
                    else if (reader.Depth == 2)
                    {
                        property = new XElement(XName.Get(locname, ns));
                    }
                    if (reader.HasAttributes) 
                    {
                        while (reader.MoveToNextAttribute()) 
                        {
                            if (reader.Value == "http://fogid.net/o/") continue;
                            XAttribute att = new XAttribute(XName.Get(reader.LocalName, reader.NamespaceURI), reader.Value);
                            if (reader.Depth == 2) record.Add(att);
                            else if (reader.Depth == 3 && property != null) property.Add(att);
                        }
                    }

                    // Display all attributes.
                    reader.MoveToElement();
                    if (reader.IsEmptyElement)
                    {
                        //Console.WriteLine($"Empty {reader.Name} {reader.Depth}");
                        if (reader.Depth == 1) { yield return record; } 
                        else if (reader.Depth == 2) 
                        {
                            if (property != null && property.Name.LocalName == "iisstore")
                            {
                                    XAttribute? att = property.Attribute("uri");
                                    if (att != null)
                                    {
                                        record.Add(new XElement(XName.Get("uri", "http://fogid.net/o/"),
                                            att.Value));
                                    }
                                    XAttribute? att_transform = property.Attribute("transform");
                                    if (att_transform != null && att_transform.Value != "")
                                    {
                                        record.Add(new XElement("{http://fogid.net/o/}transform",
                                            att_transform.Value));
                                    }
                                }
                                else record.Add(property);
                            property = null;
                        }
                    }
                    break;
                case XmlNodeType.Text:
                    //Console.Write($"{reader.Value}");
                    if (property !=null && reader.Depth == 3) property.Add(reader.Value);
                    break;
                case XmlNodeType.EndElement:
                    
                    if (reader.Depth == 1) { yield return record; } 
                    else if (reader.Depth == 2) 
                    { 
                        record.Add(property);
                        property = null;
                    }
                    else if (reader.Depth == 3)
                    {
                    }
                    break;
                    default: break;
            }

        } 
    }
}
}

