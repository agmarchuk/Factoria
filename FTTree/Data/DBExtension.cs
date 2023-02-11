using FactographData;
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

        public static string? GetLangText(this IFDataService db, TGroup input, string? enumSpecificatior = null) // TODO:add fallback lang
        {
            if (enumSpecificatior is not null && showLabels) // for enumerations
            {
                var text = ((TTexts)input).Values.FirstOrDefault()?.Text;
                return String.IsNullOrEmpty(text) ? "" : db.ontology.EnumValue(enumSpecificatior, ((TTexts)input).Values.FirstOrDefault()?.Text, lang);
            }
            if (input == null)
            {
                return null;
            }
            //if (langText == null)
            //{
            //    langText = ((Texts)input).Values.FirstOrDefault(); // 1) Try default user lang
            //}
            var langText = ((TTexts)input).Values.FirstOrDefault(val => val.Lang == lang || String.IsNullOrEmpty(val.Lang)); // 2) Try default or empty lang
            if (langText == null)
            {
                langText = ((TTexts)input).Values.FirstOrDefault(); // 3) First available lang
            }
            return langText?.Text;
        }

        public static string? GetName(this IFDataService db, TTree ttree)
        {
            return db.GetLangText(ttree.Groups.FirstOrDefault(gr => gr.Pred == "http://fogid.net/o/name"));
        }

        public static bool IsObject(this IFDataService db, string pred)
        {
            return db.ontology.DescendantsAndSelf(MainLayout.defaultObjectProp).Contains(pred);
        }

    }
}
