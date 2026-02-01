using System.Xml.Linq;

namespace OpenAr
{
    public class StaticObjects
    {
        // Статические методы выявления узлов и полей
        public static string GetDates(XElement item)
        {
            var fd_el = item.Elements("field").FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/from-date");
            var td_el = item.Elements("field").FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/to-date");
            string res = (fd_el == null ? "" : fd_el.Value) + (td_el == null ? "" : "—" + td_el.Value);
            return res;
        }
        public static string GetField(XElement item, string prop)
        {
            var el = item.Elements("field").FirstOrDefault(f => f.Attribute("prop").Value == prop);
            return el == null ? "" : el.Value;
        }
        public static IEnumerable<XElement> GetCollectionPath(string id, Func<string, bool, XElement> getitembyidbasic, Func<string, XElement, XElement> getitembyid)
        {
            //return _getCollectionPath(id);
            System.Collections.Generic.Stack<XElement> stack = new Stack<XElement>();
            XElement node = getitembyidbasic(id, false);
            if (node == null) return Enumerable.Empty<XElement>();
            stack.Push(node);
            bool ok = GetCP(id, stack, getitembyid);
            if (!ok) return Enumerable.Empty<XElement>();
            int n = stack.Count();
            // Уберем первый и последний
            var query = stack.Skip(1).Take(n - 2).ToArray();
            return query;
        }
        public static string funds_id = "funds_id";
        private static XElement formattoparentcollection =
        new XElement("record",
            new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/collection-item"),
                new XElement("record",
                    new XElement("direct", new XAttribute("prop", "http://fogid.net/o/in-collection"),
                        new XElement("record", new XAttribute("type", "http://fogid.net/o/collection"),
                            new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")))))));
        // В стеке накоплены элементы пути, следующие за id. Последним является узел с id
        private static bool GetCP(string id, Stack<XElement> stack, Func<string, XElement, XElement> getitembyid)
        {
            if (id == funds_id) return true; 
            XElement tree = getitembyid(id, formattoparentcollection);
            if (tree == null) return false;
            foreach (var n1 in tree.Elements("inverse"))
            {
                var n2 = n1.Element("record"); if (n2 == null) return false;
                var n3 = n2.Element("direct"); if (n3 == null) return false;
                var node = n3.Element("record"); if (node == null) return false;
                string nid = node.Attribute("id").Value;
                stack.Push(node);
                bool ok = GetCP(nid, stack, getitembyid);
                if (ok) return true;
                stack.Pop();
            }
            return false;
        }
        private static IEnumerable<XElement> _getCollectionPath(string id, Func<string, bool, XElement> getitembyidbasic, Func<string, XElement, XElement> getitembyid)
        {
            if (id == funds_id) return Enumerable.Empty<XElement>();
            XElement tree = getitembyid(id, formattoparentcollection);
            if (tree == null) return Enumerable.Empty<XElement>();
            var n1 = tree.Element("inverse"); if (n1 == null) return Enumerable.Empty<XElement>();
            var n2 = n1.Element("record"); if (n2 == null) return Enumerable.Empty<XElement>();
            var n3 = n2.Element("direct"); if (n3 == null) return Enumerable.Empty<XElement>();
            var n4 = n3.Element("record"); if (n4 == null) return Enumerable.Empty<XElement>();
            XElement node = n4; // tree.Element("inverse").Element("record").Element("direct").Element("record");
            string nid = node.Attribute("id").Value;
            if (nid == funds_id) return GetCollectionPath(nid, getitembyidbasic, getitembyid);
            return GetCollectionPath(nid, getitembyidbasic, getitembyid).Concat(new XElement[] { node });
        }
    }
}
