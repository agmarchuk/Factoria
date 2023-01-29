using FactographData;

namespace Family.Data
{
    /// <summary>
    /// TRecord (табличная запись) - структура, предназначенная для специального оформления записи.
    /// TRecord состоит из идентификатора записи, типа записи и организованного набора полей, прямых ссылок
    /// и обратных ссылок. Поля группируются и обратные ссылки группируются. Можно допустить группирование 
    /// и прямых ссылок, хотя сейчас это не актуально.
    /// </summary>
    public class TRecord
    {
        private IFDataService db;
        //private ROntology rontology;
        public TRecord(string recId, IFDataService db, int level = 2, string forbidden = null)
        {
            this.db = db;
            //this.rontology = rontology;
            // Если level = 0 - только поля, 1 - поля и прямые ссылки,  2 - поля, прямые ссылки и обратные ссылки
            RRecord erec = (new RYEngine(db)).GetRRecord(recId, level > 1);
            if (erec == null) return;
            Id = recId;
            Tp = erec.Tp;

            // В зависимости от типа, узнаем количество прямых и обратных свойств и заводим массив t-свойств этого размера  
            int nprops = db.ontology.PropsTotal(Tp);
            // Массив списков свойств для накапливания информации
            List<RProperty>[] lists = new List<RProperty>[nprops];

            // Также заводим массив списков RRecord'ов для накопления сгруппированных полей и обратных свойств
            //List<TRecord>[] reclists = new List<TRecord>[nprops];

            // Сканируем имеющиеся свойства записи и раскладываем их по массиву в соответствии с позицией ind, для обратных свойств пока накапливаем  
            foreach (var p in erec.Props)
            {
                if (p is RLink && p.Prop == forbidden) continue;
                int ind = db.ontology.PropPosition(Tp, p.Prop, p is RInverseLink);
                if (ind == -1) continue;
                if (lists[ind] == null) lists[ind] = new List<RProperty>();
                lists[ind].Add(p);
            }

            // Создаем массив TProperty[] props
            props = new TProperty[nprops];

            // Преобразуем элементы lists в элементы props
            for (int ind = 0; ind < nprops; ind++)
            {
                List<RProperty> p_list = lists[ind];
                if (p_list == null) continue;
                RProperty p0 = p_list[0];
                TProperty t = null;
                if (p0 is RField)
                {
                    t = new TFieldValues()
                    {
                        PropId = p0.Prop,
                        Values = p_list.Select(p =>
                        {
                            RField f = (RField)p;
                            return new LangText { Text = f.Value, Lang = f.Lang };
                        }).ToArray()
                    };
                }
                else if (level > 0 && p0 is RLink)
                {
                    // Буду фиксировать только первое значение p0
                    RLink lnk = (RLink)p0;
                    t = new TDirect { PropId = p0.Prop, Record = new TRecord(lnk.Resource, db, level - 1, null) };
                }
                else if (level > 1 && p0 is RInverseLink)
                {
                    RInverseLink inv = (RInverseLink)p0;
                    t = new TInverse
                    {
                        PropId = p0.Prop,
                        Records = p_list.Cast<RInverseLink>()
                            .Select(ri => new TRecord(ri.Source, db, level - 1, p0.Prop)).ToArray()
                    };
                }
                props[ind] = t;
            }
        }

        public string Id { get; set; }
        public string Tp { get; set; }
        // Наборы свойств, каждый элемент массива соответствует одному или группе свойств с единым f/d/i и
        // именем свойства.  
        // Варианты для TProperty:
        //   TFieldValues { propId, LangText[] }
        //   TDirect { propId, TRecord }
        //   TInverse { propId, TRecord[] } -- В обратных записях нет прямой ссылки с propId  
        private TProperty[] props;
        public TProperty[] Props { get { return props; } }

        public IEnumerable<LangText> GetTexts(string propId)
        {
            int ind = db.ontology.PropPosition(Tp, propId, false);
            if (ind == -1) return new LangText[0];
            var prop = props[ind];
            if (prop == null) return new LangText[0];
            if (prop.PropId != propId) throw new Exception("Err: 28378");
            return ((TFieldValues)prop).Values;
        }
        public LangText GetLangText(string propId, string lang) => GetTexts(propId)?.FirstOrDefault(t => t.Lang == lang);
        public LangText GetText(string propId)
        {
            // Почему-то не работает...
            //var query = GetTexts(propId).Aggregate<LangText, LangText>(null, (sum, tx) =>
            //{
            //    string lan = tx.Lang;
            //    if (sum == null || lan == "ru") return tx;
            //    string sumlan = sum.Lang;
            //    if (sum.Lang == "ru") return sum;
            //    if (lan == "en") return tx;
            //    else return sum;
            //});
            //return query;

            LangText sum = null;
            foreach (var tx in GetTexts(propId))
            {
                string lan = tx.Lang;
                if (sum == null || lan == "ru") return tx;
                string sumlan = sum.Lang;
                if (sum.Lang == "ru") return sum;
                if (lan == "en") return tx;
                else return sum;
            }
            return sum;
        }
        public IEnumerable<LangText> GetNames() => GetTexts("http://fogid.net/o/name");
        public LangText GetName(string lang) => GetLangText("http://fogid.net/o/name", lang);
        public LangText GetName() => GetText("http://fogid.net/o/name");

        private static readonly string[] months = new[] { "янв", "фев", "мар", "апр", "май", "июн", "июл", "авг", "сен", "окт", "ноя", "дек" };
        public static string DatePrinted(string date)
        {
            if (date == null) return null;
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

        public string GetDates()
        {
            string fd = GetText("http://fogid.net/o/from-date")?.Text;
            string td = GetText("http://fogid.net/o/to-date")?.Text;
            return (string.IsNullOrEmpty(fd) ? "" : DatePrinted(fd)) +
                (string.IsNullOrEmpty(td) ? "" : " - " + DatePrinted(td));
        }
        public string GetLabel(string ontoTerm) => db.ontology.LabelOfOnto(ontoTerm);
        public TRecord GetDirect(string propId)
        {
            int ind = db.ontology.PropPosition(Tp, propId, false);
            if (ind == -1) return null;
            var prop = props[ind];
            if (prop == null || prop.PropId != propId) return null;
            return ((TDirect)prop).Record;
        }
        public IEnumerable<TRecord> GetMultiInverse(string propId)
        {
            int ind = db.ontology.PropPosition(Tp, propId, true);
            if (ind == -1) return new TRecord[0];
            var prop = props[ind];
            if (prop == null) return new TRecord[0];
            if (prop.PropId != propId) throw new Exception("Err: 28380");
            return ((TInverse)prop).Records;
        }


    }

    public abstract class TProperty
    {
        public string PropId { get; set; }
    }
    public class LangText
    {
        public string Lang { get; set; }
        public string Text { get; set; }
        public override string ToString()
        {
            string t = Text;
            if (Lang != null) t = t + "^" + Lang;
            return t;
        }
    }
    public class TFieldValues : TProperty
    {
        public LangText[] Values { get; set; }
    }
    public class TDirect : TProperty
    {
        public TRecord Record { get; set; }
    }
    public class TInverse : TProperty
    {
        public TRecord[] Records { get; set; }
    }

}
