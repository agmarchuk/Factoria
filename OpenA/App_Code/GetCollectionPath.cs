using System.Xml.Linq;
using Factograph.Data;

namespace OpenA.App_Code
{
    public class CollectionPath
    {
        public static IEnumerable<RRecord>? PathToBase(string id, string bas, Factograph.Data.IFDataService db)
        {
            // Если идентификатор есть bas, то возвращаем ПУСТУЮ последовательность!
            if (id == bas) return Enumerable.Empty<RRecord>();

            // Находим расширенную запись по идентификатору
            RRecord? er = db.GetRRecord(id, true);
            if (er ==  null) return null;
            RRecord erec = er; 
            // Смотрим все обратные ссылки "http://fogid.net/o/collection-item"
            var coits = erec?.Props.Where(p => p is RInverseLink && p.Prop == "http://fogid.net/o/collection-item"); 
            // Находим записи отношения collection-member
            var cm_relation = coits?.Cast<RInverseLink>()
                .Select(p => db.GetRRecord(p.Source, false))
                .Where(r => r != null)
                .Cast<RRecord>();
            // Находим ссылки на коллекции
            var c_ref = cm_relation?.Select(r => r.GetDirectResource("http://fogid.net/o/in-collection"))
                .Where(s => s != null)
                .Cast<string>();
            if (c_ref == null) return null;
            // В цикле ищем пути пока не найдем или не переберем все
            foreach (string idd in c_ref)
            {
                var pth = PathToBase(idd, bas, db);
                if (pth == null) continue;
                // Видимо найден, формируем результат добавляя к pth запись erec
                return pth.Append(erec);
            }
            return null;
        } 

        private IFDataService db;
        public CollectionPath(IFDataService db)
        {
            this.db = db;
        }
        // Определяет цепочку коллекций между объектом с идентификатором id и коллекцией с идентификатором funds_id. 
        // Если такой цепочки нет, то выдается null.
        //public IEnumerable<RRecord>? GetCollectionPath(string id, string funds_id)
        //{
        //    System.Collections.Generic.Stack<RRecord> stack = new Stack<RRecord>();
        //    var node =  db.GetRRecord(id, false);
        //    if (node == null) return Enumerable.Empty<RRecord>();
        //    stack.Push(node);
        //    bool ok = GetCP(id, stack);
        //    if (!ok) return Enumerable.Empty<XElement>();
        //    int n = stack.Count();
        //    // Уберем первый и последний
        //    var query = stack.Skip(1).Take(n - 2).ToArray();
        //    return query;
        //}
        //// В стеке накоплены элементы пути, ведущие "вверх" по цепочке "элемент-коллекции - коллекция за id. Последним является узел с id
        //private bool GetCP(string id, string funds_id, Stack<RRecord> stack)
        //{
        //    if (id == funds_id) return true;
        //    XElement tree = engine.GetItemById(id, formattoparentcollection);
        //    if (tree == null) return false;
        //    foreach (var n1 in tree.Elements("inverse"))
        //    {
        //        var n2 = n1.Element("record"); if (n2 == null) return false;
        //        var n3 = n2.Element("direct"); if (n3 == null) return false;
        //        var node = n3.Element("record"); if (node == null) return false;
        //        string nid = node.Attribute("id").Value;
        //        stack.Push(node);
        //        bool ok = GetCP(nid, stack);
        //        if (ok) return true;
        //        stack.Pop();
        //    }
        //    return false;
        //}

    }
}
