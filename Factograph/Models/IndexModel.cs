using Factograph.Data;
using Factograph.Data.r;
namespace Factograph.Models
{
    class Precalc
    {
        public string[]? typs = null;
        public Dictionary<string, Rec>? treeShablons = null; 
        
    }
    public class IndexModel
    {
        public string[]? typs = null; // Это набор системных типов, в которых выполняется поиск
        public string? id { get; set; }
        public string? searchstring { get; set; }
        public string? stype { get; set; }
        public string? direction { get; set; }
        public bool bwords { get; set; }
        public Dictionary<string, Rec>? shablons { get; set; }
        public Rec? shablon { get; set; }
        public Rec? tree { get; set; }
        private Factograph.Data.IFDataService db;
        public IndexModel(Factograph.Data.IFDataService db) 
        {
            this.db = db;
            if (db.precalculated == null) 
            {
                db.precalculated = new Precalc();
                ((Precalc)db.precalculated).typs = db.ontology.DescendantsAndSelf("http://fogid.net/o/sys-obj")
                    .Where(t => db.ontology.LabelOfOnto(t) != null)
                    .ToArray();
                ((Precalc)db.precalculated).treeShablons = ((Precalc)db.precalculated).typs?
                    .Select(t => BuildShablon(t, 2, null, db.ontology))
                    .ToDictionary(r => r.Tp, r => r);
            }
            typs = ((Precalc)db.precalculated).typs;
            shablons = ((Precalc)db.precalculated).treeShablons;
        }
        // Генерация универсального шаблона
        public Rec BuildShablon(string ty, int level, string? forbidden, IOntology ontology)
        {
            // Все прямые возможнные свойства
            string[] dprops = ontology.GetDirectPropsByType(ty).ToArray();
            var propsdirect = dprops.Select<string, Pro?>(pid =>
            {
                var os = ontology.OntoSpec
                    .FirstOrDefault(o => o.Id == pid);
                if (os == null) return null;
                if (os.Tp == "DatatypeProperty")
                {
                    var tt = ontology.RangesOfProp(pid).FirstOrDefault();
                    bool istext = tt == "http://fogid.net/o/text" ? true : false;
                    if (istext) return new Tex(pid);
                    else return new Str(pid);
                }
                else if (os.Tp == "ObjectProperty" && level > 0 && os.Id != forbidden)
                {
                    string[] tps = ontology.RangesOfProp(pid)
                        .SelectMany(t => ontology.DescendantsAndSelf(t))
                        .ToArray();
                    if (tps.Length == 0) return null;
                    if (tps.Length > 1) { } // Почему-то не получается >1
                    return new Dir(pid, tps.Select(t => BuildShablon(t, 0, pid, ontology)).ToArray());
                }
                return null;
            }).ToArray();
            string[] iprops = level > 1 ? ontology.GetInversePropsByType(ty).ToArray() : new string[0];
            var propsinverse = iprops.Select<string, Pro?>(pid =>
            {
                var os = ontology.OntoSpec
                    .FirstOrDefault(o => o.Id == pid);
                if (os == null) return null;
                if (os.Tp == "ObjectProperty")
                {
                    string[] tps = ontology.DomainsOfProp(pid).ToArray();
                    if (tps.Length == 0) return null;
                    return new Inv(pid, tps.Select(t => BuildShablon(t, level - 1, pid, ontology)).ToArray());
                }
                return null;
            }).ToArray();
            var shab = new Rec(null, ty,
                propsdirect
                .Concat(propsinverse)
                .Where(p => p != null)
                .Cast<Pro>()
                .ToArray());
            return shab;
        }
        // Создание дерева по шаблону
        public static Rec? BuildTree(RRecord rrec, Rec shablon, Factograph.Data.IFDataService db)
        {
            if (rrec == null) return null;
            // Выстраивается массив arr пустых списков свойств RProperty
            List<RProperty>[] arr = Enumerable.Repeat<int>(-1, shablon.Props.Length)
                .Select(x => new List<RProperty>())
                .ToArray();
            // Заводим три словаря. Это надо вынести в предвычисления!!!
            //fieldDic, directDic, inverseDic
            Dictionary<string, int> fieldDic = shablon.Props
                .Select((p, i) => (i, p))
                .Where(pair => pair.p is Tex || pair.p is Str)
                .ToDictionary(pair => pair.p.Pred, pair => pair.i);
            Dictionary<string, int> directDic = shablon.Props
                .Select((p, i) => (i, p))
                .Where(pair => pair.p is Dir)
                .ToDictionary(pair => pair.p.Pred, pair => pair.i);
            Dictionary<string, int> inverseDic = shablon.Props
                .Select((p, i) => (i, p))
                .Where(pair => pair.p is Inv)
                .ToDictionary(pair => pair.p.Pred, pair => pair.i);
            foreach (var p in rrec.Props)
            {
                string pred = p.Prop;
                if (pred == null) { continue; }
                int ind = -1;
                if (p is RField)
                {
                    if (!fieldDic.TryGetValue(pred, out ind)) ind = -1;
                }
                else if (p is RLink)
                {
                    if (!directDic.TryGetValue(pred, out ind)) ind = -1;
                }
                else if (p is RInverseLink)
                {
                    if (!inverseDic.TryGetValue(pred, out ind)) ind = -1;
                }
                else new Exception("3544");
                if (ind != -1) arr[ind].Add(p);
            }
            
            Pro[] props = new Pro[arr.Length];
            for (int i = 0; i < arr.Length; i++) 
            {
                var list = arr[i];
                var sh = shablon.Props[i];
                if (sh is Tex)
                {
                    props[i] = new Tex(sh.Pred, list
                        .Cast<RField>()
                        .Select(rf => new TextLan(rf.Value, rf.Lang))
                        .ToArray());
                } else if (sh is Str)
                {
                    props[i] = new Str(sh.Pred, list.Count > 0 ? ((RField)(list[0])).Value : null);
                    //TOTHINK: Пока непонятно что делать с другими значениями, если длина списка > 1
                } else if (sh is Dir)
                {
                    var resources = list.Cast<RLink>()
                        .Select(rl =>
                        {
                            string idd = rl.Resource;
                            RRecord? resRrec = db.GetRRecord(idd, false);
                            if (resRrec == null) return null;
                            // Ищем подходящий тип в шаблоне
                            string ty = resRrec.Tp;
                            Rec? sub_sh = ((Dir)sh).Resources.FirstOrDefault(r => r.Tp == ty);
                            if (sub_sh == null) return null;
                            var sub_tree = BuildTree(resRrec, sub_sh, db); 
                            return sub_tree;
                        })
                        .Where(resource => resource != null)
                        .Cast<Rec>()
                        .ToArray();
                    props[i] = new Dir(sh.Pred, resources);
                }
                else if (sh is Inv)
                {
                    var xinv = (Inv)sh;
                    var sources = list.Cast<RInverseLink>()
                        .Select(ri =>
                        {
                            string idd = ri.Source;
                            RRecord? srcRec = db.GetRRecord(idd, false);
                            if (srcRec == null) return null;
                            var ty = srcRec.Tp;
                            Rec? sub_sh = ((Inv)sh).Sources.FirstOrDefault(r => r.Tp == ty);
                            if (sub_sh == null) return null;
                            var sub_tree = BuildTree(srcRec, sub_sh, db);
                            return sub_tree;
                        })
                        .Where(st => st != null)
                        .Cast<Rec>()
                        .ToArray();

                    props[i] = new Inv(sh.Pred, sources);
                }
            }
            Rec result = new(rrec.Id, rrec.Tp, props);
            return result;
        }
    }
}
