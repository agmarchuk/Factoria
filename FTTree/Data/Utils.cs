using FactographData;
using Microsoft.AspNetCore.Components;
using System.Runtime.CompilerServices;

namespace FactorgaphyTTree.Data
{
    public static class Utils
    {
        // to utills

        private static bool showLabels = true;
        private static string lang = "ru";

        public static IFDataService db;

        public static IFDataService Db
        {
            get
            {
                return db;
            }
        }

        public static void InitUtils(IFDataService _db)
        {
            db = _db;
        }

        //[Inject]
        //public static IFDataService db { get; set; }

        public static string GetOntLabel(string input) // lang?
        {
            return showLabels == true ? db.ontology.LabelOfOnto(input) : input;
        }

        public static string? GetLangText(TGroup input) // add fallback lang
        {
            if (input == null)
            {
                return null;
            }
            var langText = ((Texts)input).Values.FirstOrDefault(val => val.Lang == lang || val.Lang == "");
            //if (langText == FALLBACKLANG)
            //{
            //    langText = ((Texts)input).Values.FirstOrDefault();
            //}
            if (langText == null)
            {
                langText = ((Texts)input).Values.FirstOrDefault();
            }
            return langText?.Text;
        }

        public static string? GetName(TTree ttree)
        {
            return GetLangText(ttree.Groups.FirstOrDefault(gr => gr.Pred == "http://fogid.net/o/name"));
        }
    }
}
