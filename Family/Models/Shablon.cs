using FactographData;
using FactographData.r;

namespace Family.Models
{
    public class Shablon
    {
        IFDataService db;
        public Shablon(IFDataService db) 
        { 
            this.db = db;
        }
        public Rec GetUniShablon(string ty, int level, string? forbidden)
        {
            // Все прямые возможнные свойства
            string[] dprops = db.ontology.GetDirectPropsByType(ty).ToArray();
            var propsdirect = dprops.Select<string, Pro?>(pid =>
            {
                var os = db.ontology.OntoSpec
                    .FirstOrDefault(o => o.Id == pid);
                if (os == null) return null;
                if (os.Tp == "DatatypeProperty")
                {
                    var tt = db.ontology.RangesOfProp(pid).FirstOrDefault();
                    bool istext = tt == "http://fogid.net/o/text" ? true : false;
                    if (istext) return new Tex(pid);
                    else return new Str(pid);
                }
                else if (os.Tp == "ObjectProperty" && level > 0 && os.Id != forbidden)
                {
                    var tt = db.ontology.RangesOfProp(pid).FirstOrDefault();
                    if (tt == null) return null;
                    return new Dir(pid, new Rec[] { GetUniShablon(tt, 0, null) }); // Укорачивает развертку шаблона
                }
                return null;
            }).ToArray();
            string[] iprops = level > 1 ? db.ontology.GetInversePropsByType(ty).ToArray() : new string[0];
            var propsinverse = iprops.Select<string, Pro?>(pid =>
            {
                var os = db.ontology.OntoSpec
                    .FirstOrDefault(o => o.Id == pid);
                if (os == null) return null;
                if (os.Tp == "ObjectProperty")
                {
                    string[] tps = db.ontology.DomainsOfProp(pid).ToArray();
                    if (tps.Length == 0) return null;
                    return new Inv(pid, tps.Select(t => GetUniShablon(t, level - 1, pid)).ToArray());
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

    }
}
