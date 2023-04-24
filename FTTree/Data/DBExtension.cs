using FactographData;
using FactographData.r;
using FTTree.Shared;

namespace FTTree.Data
{
    public static class DBExtension
    {
        private static bool showLabels = true;
        private static string lang = MainLayout.defaultLanguage;
        public static string GetOntLabel(this IFDataService db, string input)
        {
            return showLabels ? db.ontology.LabelOfOnto(input) : input;
        }

        public static string? GetLangText(this IFDataService db, Pro input, string? enumSpecificatior = null) // TODO:add fallback lang
        {
            if (enumSpecificatior is not null && showLabels) // for enumerations
            {
                var text = ((Str)input).Value;
                return String.IsNullOrEmpty(text) ? "" : db.ontology.EnumValue(enumSpecificatior, text, lang);
            }
            if (input == null)
            {
                return null;
            }
            //if (langText == null)
            //{
            //    langText = ((Texts)input).Values.FirstOrDefault(); // 1) Try default user lang
            //}
            var langText = ((Tex)input).Values.FirstOrDefault(val => val.Lang == lang || String.IsNullOrEmpty(val.Lang)); // 2) Try default or empty lang
            if (langText == null)
            {
                langText = ((Tex)input).Values.FirstOrDefault(); // 3) First available lang
            }
            return langText?.Text;
        }

        public static string? GetName(this IFDataService db, Rec rec)
        {
            return db.GetLangText(rec.Props.FirstOrDefault(gr => gr.Pred == "http://fogid.net/o/name"));
        }

        public static bool IsObject(this IFDataService db, string pred)
        {
            return db.ontology.DescendantsAndSelf(MainLayout.defaultObjectProp).Contains(pred);
        }

        public static Rec GenereateTempalate(this IFDataService db, string ty, int level, string? forbidden)
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
                    return new Dir(pid, new Rec[] { GenereateTempalate(db, tt, 0, null) }); // Укорачивает развертку шаблона
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
                    return new Inv(pid, tps.Select(t => GenereateTempalate(db, t, level - 1, pid)).ToArray());
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
