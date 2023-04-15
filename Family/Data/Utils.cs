using FactographData;
using FactographData.r;
using System;
using System.Text;

namespace Family.Data
{
    public class Utils
    {
        private static string[] months = new[] { "янв", "фев", "мар", "апр", "май", "июн", "июл", "авг", "сен", "окт", "ноя", "дек" };
        public static string DatePrinted(string date)
        {
            if (date == null) return "";
            string[] split = date.Split('-');
            string str = split[0];
            if (split.Length > 1)
            {
                int month;
                if (Int32.TryParse(split[1], out month) && month > 0 && month <= 12)
                {
                    str += months[month - 1];
                    if (split.Length > 2) str += split[2].Substring(0, 2);
                }
            }
            return str;
        }
        public static string? OneText(Tex txts)
        {
            if (txts.Values.Length == 0) return null;
            string tx = txts.Values.Aggregate<TextLan>((t1, t2) =>
            {
                if (t1.Lang == "ru") return t1;
                if (t2.Lang == "ru") return t2;
                return t2;
            }).Text;
            return tx;
        }
        public static string Inline(Rec rec, IOntology ontology)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var v in rec.Props)
            {
                if (v is Tex)
                {
                    var tx = Family.Data.Utils.OneText((Tex)v);
                    sb.Append(" " + tx);
                }
                else if (v is Str)
                {
                    string? s = ((Str)v).Value;
                    if (v.Pred == "http://fogid.net/o/from-date")
                    {
                        string d = Utils.DatePrinted(s);
                        sb.Append(" ");
                        sb.Append(d);
                    }
                    else if (v.Pred == "http://fogid.net/o/to-date")
                    {
                        string d = Utils.DatePrinted(s);
                        sb.Append(" -");
                        sb.Append(d);
                    }
                    else if (ontology.IsEnumeration(v.Pred))
                    {
                        sb.Append(" ");
                        sb.Append(ontology.EnumValue(v.Pred, s, "ru"));
                    }
                    else
                    {
                        sb.Append(" ");
                        sb.Append(s);
                    }
                }
            }
            return sb.ToString();
        }
    }
}
