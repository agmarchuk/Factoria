using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Factograph.Data.Adapters
{
    class ScanFogfiles
    {
        //private Stream stream;
        private string[] fogfile_names;
        private Func<XElement, XElement> transformXElement = null;
        private Func<XElement, bool> useXElement = null; // true - OK, false - break
        private Func<XElement, XElement> revisionXElement = null; // первичная доработка
        public ScanFogfiles(string[] fogfile_names,
            Func<XElement, XElement> transformXElement,
            Func<XElement, bool> useXElement)
        {
            //this.stream = stream;
            this.fogfile_names = fogfile_names;
            this.transformXElement = transformXElement;
            this.useXElement = useXElement;
        }
        public static XElement CreateRevision(XElement xrec)
        {
            Console.WriteLine("=============123==============" + xrec);
            return xrec;
            // Надо определить "стандартную" доработку xdoc'а:
            // <delete id=""/> => <delete rdf:about="" />
            // <substitute oldid="" newid=""/> => <substitute rdf:about="oldid" ><newid rdf:resource="new"/></substitute>
            //XElement revisionXElem =
            if (xrec.Name.LocalName == "delete")
            {
                string? att1 = xrec.Attribute("id")?.Value;
                if (att1 != null)
                {
                    return new XElement(XName.Get("delete", "http://fogid.net/o/"),
                    new XAttribute(XName.Get("about", "http://www.w3.org/1999/02/22-rdf-syntax-ns#"), att1));
                }
                else return xrec;
            }
            else if (xrec.Name.LocalName == "substitute")
            {
                string? att2 = xrec.Attribute("oldid")?.Value;
                if (att2 != null)
                {
                    return new XElement(XName.Get("substitute", "http://fogid.net/o/"),
                        new XAttribute(XName.Get("about", "http://www.w3.org/1999/02/22-rdf-syntax-ns#"), att2),
                        new XElement(XName.Get("resource", "http://www.w3.org/1999/02/22-rdf-syntax-ns#"),
                        xrec.Attribute("newid")?.Value));
                }
                else return xrec;
            }
            else return xrec;
            
        }
    
    public void Scan()
        {
            foreach (string ffname in fogfile_names)
            {
                OneFog fog = new OneFog(ffname);
                foreach (XElement xrec in fog.Records().Select(x => revisionXElement(x)))//
                {
                    //Console.WriteLine("=============123==============" + xrec);
                    // Рабочая зона
                    var xx = transformXElement(xrec);
                    if (xx != null) { bool ok = useXElement(xx); if (!ok) break; }
                }
                fog.Close();
            }
       }

        public IEnumerable<XElement> ScanGenerate()
        {
            foreach (string ffname in fogfile_names)
            {
                OneFog fog = new OneFog(ffname);
                foreach (XElement xrec in fog.Records().Select(x => revisionXElement(x))) //
                {
                    yield return xrec;
                }
                fog.Close();
            }
        }

    }
}

