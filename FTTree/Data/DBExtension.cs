using FactographData;
using FactographData.r;
using FTTree.Components;
using FTTree.Shared;
using OAData.Adapters;
using System.Linq;
using System.Security.AccessControl;
using System.Xml.Linq;

namespace FTTree.Data
{
    public static class DBExtension
    {
        private static bool showLabels = true;
        public static string GetOntLabel(this IFDataService db, string input)
        {
            return showLabels ? db.ontology.LabelOfOnto(input) : input;
        }

        public static string? GetLangText(this IFDataService db, Pro input, string? enumSpecificatior = null) // TODO:add fallback lang
        {
            if (input == null)
            {
                return null;
            }
            if (input is Str)
            {
                var text = ((Str)input).Value;
                if (enumSpecificatior is not null && showLabels) // for enumerations
                {

                    return String.IsNullOrEmpty(text) ? "" : db.ontology.EnumValue(enumSpecificatior, text, MainLayout.currentLanguage);
                }
                else
                {
                    return String.IsNullOrEmpty(text) ? "" : text;
                }
            }
            if (input is Tex)
            {
                var langText = ((Tex)input).Values
                    .FirstOrDefault(val => val.Lang == MainLayout.currentLanguage || String.IsNullOrEmpty(val.Lang)); // 1) Try user or empty lang
                if (langText == null)
                {
                    langText = langText = ((Tex)input).Values
                    .FirstOrDefault(val => val.Lang == MainLayout.defaultLanguage); // 2) Try default lang
                }
                if (langText == null)
                {
                    langText = langText = ((Tex)input).Values.FirstOrDefault(); // 3) Try first available lang
                }
                return langText?.Text;
            }

            return null;
        }

        public static string? GetName(this IFDataService db, Rec rec)
        {
            return db.GetLangText(rec.Props.FirstOrDefault(gr => gr.Pred == "http://fogid.net/o/name"));
        }

        public static bool IsObject(this IFDataService db, string pred)
        {
            return db.ontology.DescendantsAndSelf(MainLayout.defaultObjectProp).Contains(pred);
        }
        public static Rec GenerateRecordById(this IFDataService db, string id)
        {
            object[] obj = (object[])db.GetAdapter().GetRecord(id);
            return Rec.BuildByObj(obj, db.GenereateTempalate(obj[1].ToString(), 2, id), db.GetAdapter().GetRecord);
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

        public static void SaveRec(this IFDataService db, Rec rec)
        {
            var objectRec = Rec.RecToObject(rec);
            var xmlRec = Rec.RecToXML(rec, "Sergey");
            ((UpiAdapter)db.GetAdapter()).PutItem(objectRec);
            db.UpdateItem(xmlRec);
        }
        public static void DeleteRec(this IFDataService db, Rec rec)
        {
            ((UpiAdapter)db.GetAdapter()).PutItem(new object[] { rec.Id, "delete", new object[0] });
            db.UpdateItem(new XElement("delete", new XAttribute("owner", "Sergey"), new XAttribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about", rec.Id)));
        }

        public static Type GetComponentType(this IFDataService db, Pro field)
        {
            var compType = typeof(StrCell);

            if (field is Tex)
            {
                compType = typeof(TexCell);
            }

            if (field is Dir)
            {
                compType = typeof(DirCell);
            }
            return compType;
        }

        public static string GetOntologyLabel(this IFDataService db, string prop)
        {
            return db.ontology.LabelOfOnto(prop, MainLayout.currentLanguage);
        }
    }
}
