using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactographData
{
    /// <summary>
    /// Класс предназначен для порождения объектов TTree из базы данных и онтологии.
    /// Сначала инстанс класса конструируется. При этом, делается подготовка данных. 
    /// Потом, по мере необходимости, запускается создатель объектов TTree. 
    /// </summary>
    public class TTreeBuilder
    {
        private OAData.Adapters.UpiAdapter adapter;
        private IOntology ontology;

        public TTreeBuilder(OAData.Adapters.UpiAdapter adapter, IOntology ontology)
        {
            this.adapter = adapter;
            this.ontology = ontology;


            // Подготовка данных
            dic_specs = ontology.OntoSpec
                .Where(os => os.Tp == "Class")
                .Select(tab =>
                {
                    string id = tab.Id;
                    var directProps = ontology.GetDirectPropsByType(id);
                    var inverseProps = ontology.GetInversePropsByType(id);
                    PSpec[] dps_spec = directProps
                        .Select(p =>
                        {
                            return GetPSpec(p);
                        })
                        .OrderBy(sp => sp.priority) // посортируем...
                        .Concat(
                            inverseProps
                                .Select(ip =>
                                {
                                    var sp = GetPSpec(ip);
                                    return new PSpec
                                    {
                                        propId = sp.propId,
                                        vid = PropVid.inverse,
                                        label = sp.inverselabel,
                                        priority = sp.priority,
                                        domain = sp.domain,
                                        range = sp.range
                                    };
                                })
                        ).ToArray();
                    return new Tuple<string, PSpec[]>(id, dps_spec);
                })
                .ToDictionary(pair => pair.Item1, pair => pair.Item2);
        }
        // Перечисление вариантов возможных свойств
        enum PropVid { tstring, ttexts, direct, inverse }
        // Структура спецификатора свойства
        struct PSpec
        {
            public string propId;
            public PropVid vid;
            public string? priority;
            public string? label;
            public string? inverselabel;
            public string? domain;
            public string? range;
        }
        // Вычисление спецификаци свойства (вариант inverse отсутствует)
        PSpec GetPSpec(string prop_id)
        {
            var spec = ontology.OntoSpec
                .FirstOrDefault(spec => spec.Id == prop_id);
            if (spec == null) throw new Exception("983224");
            PSpec pspec = new PSpec() { propId = prop_id };
            bool isdtprop = spec.Tp == "DatatypeProperty";
            bool istext = false;
            string domain = null;
            string range = null;
            string label = null;
            string inverselabel = null;
            string priority = null;
            foreach (var p in spec.Props)
            {
                if (p is RField)
                {
                    var f = (RField)p;
                    if (f.Prop == "Label" && f.Lang == "ru") label = f.Value;
                    else if (f.Prop == "InvLabel" && f.Lang == "ru") inverselabel = f.Value;
                    else if (f.Prop == "priority") priority = f.Value;
                }
                else if (p is RLink)
                {
                    var d = (RLink)p;
                    if (d.Prop == "domain") domain = d.Resource;
                    else if (d.Prop == "range") range = d.Resource;
                }
            }
            pspec.vid = isdtprop ? (range == "http://fogid.net/o/text" ? PropVid.ttexts : PropVid.tstring) :
                PropVid.direct;
            pspec.label = label;
            pspec.inverselabel = inverselabel;
            pspec.priority = priority;
            pspec.domain = domain;
            pspec.range = range;
            return pspec;
        }

        // Словарь массивов спецификаций (колонок), от типа 
        private Dictionary<string, PSpec[]> dic_specs;

        public TTree? GetTTree(string recId, int level = 2, string? forbidden = null) // null если не найден
        {
            // Если level = 0 - только поля, 1 - поля и прямые ссылки,  2 - поля, прямые ссылки и обратные ссылки
            object[] erec = (object[])adapter.GetRecord(recId); //(new RYEngine(db)).GetRRecord(recId, level > 1);
            if (erec == null) return null;
            string id = (string)erec[0];
            string tp = (string)erec[1];
            object[] oprops = (object[])erec[2];
            // Определяем шаблон
            PSpec[] shablon = dic_specs[tp];
            // Здесь возможно надо упорядочить (отсортировать) элементы шаблона
            // ...
            // Теперь создадим массив, длиной oprops - номера свойств в шаблоне
            int[] nomsinshablon = new int[oprops.Length];
            // И массив целых, длиной в шаблон. Это будет к-во стрелок в группе
            int[] prop_count = Enumerable.Repeat<int>(0, shablon.Length).ToArray();
            // Сканируем массив свойств с подсчетом и вычислениями
            for (int i = 0; i < oprops.Length; i++)
            {
                var oprop = (object[])oprops[i];
                int v = (int)oprop[0]; // 1 - field, 2 - direct, 3 - inverse
                object[] rest = (object[])oprop[1];
                string prop_id = (string)rest[0];
                // Пока нет дополнительного словаря, находим строчку шаблона перебором
                // Строчку такую, что вид строчки и вид шаблона совпадают, как и ид свойства 
                // выявляем шаблонную пару, из нее получаем и номер и сам шаблон
                var shpair = shablon.Select((p, i) => new { p, i })
                    .FirstOrDefault(pair => pair.p.propId == prop_id && (
(v == 1 && (pair.p.vid == PropVid.tstring || pair.p.vid == PropVid.ttexts)) ||
(v == 2 && (pair.p.vid == PropVid.direct)) ||
(v == 3 && (pair.p.vid == PropVid.inverse))
                        ));
                if (shpair == null)
                {
                    // несоответствие онтологии
                    nomsinshablon[i] = -1;
                    continue;
                    //throw new Exception("243434");
                }
                int nom = shpair.i;
                var sh = shablon[nom];

                // Корректируем счетчик
                prop_count[(int)nom] += 1;

                // Вычислим информацию о стрелке и вставим ее i-ым элементом в afterscan 
                nomsinshablon[i] = nom;
            }
            // Главное действие: сканирование свойств, получение групп
            // В каждую группу войдут prop_count элементов из oprops создадим текущий номер для каждой
            int[] curr_nom = Enumerable.Repeat<int>(0, shablon.Length).ToArray();
            // Результирующие группы
            TGroup[] groups =
                //new TGroup[shablon.Length];
                shablon.Select<PSpec, TGroup>(sh =>
                {
                    if (sh.vid == PropVid.tstring) { return new TString(sh.propId, ""); }
                    else if (sh.vid == PropVid.ttexts) { return new TTexts(sh.propId, new TextLan[0]); }
                    else if (sh.vid == PropVid.direct) { return new TDTree(sh.propId, null); }
                    else if (sh.vid == PropVid.inverse) { return null; }
                    else return null;
                })
                .ToArray();
            // Сканируем свойства
            for (int i = 0; i < oprops.Length; i++)
            {
                var oprop = (object[])oprops[i];
                int v = (int)oprop[0]; // 1 - field, 2 - direct, 3 - inverse
                object[] rest = (object[])oprop[1];
                string prop_id = (string)rest[0];
                // Смотрим что надо создавать. По индексу находим номер шаблона
                int nom = nomsinshablon[i];
                // Пропускаем не существующее в онтологии
                if (nom == -1) continue;
                // Пропускаем запрещенные свойства
                if (prop_id == forbidden) continue;
                // Получим спецификацию используемого свойства
                var prop_spec = shablon[nom];
                if (prop_spec.vid == PropVid.tstring)
                {
                    // Если строка, то единственное значение вот оно!
                    groups[nom] = new TString(prop_id, (string)rest[1]);
                }
                else if (prop_spec.vid == PropVid.ttexts)
                {
                    // Если первый текст, то сначала создадим группу
                    if (curr_nom[nom] == 0)
                    {
                        groups[nom] = new TTexts(prop_id, new TextLan[prop_count[nom]]);
                    }
                    TTexts ttgroup = (TTexts)groups[nom];
                    ttgroup.Values[curr_nom[nom]] = new TextLan((string)rest[1], (string)rest[2]);
                    curr_nom[nom] += 1;
                }
                else if (prop_spec.vid == PropVid.direct && level > 0)
                {
                    // Единственное значение вот оно! (может запрещенный предикат нужен)
                    groups[nom] = new TDTree(prop_id, GetTTree((string)rest[1], level - 1));
                }
                else if (prop_spec.vid == PropVid.inverse && level > 1)
                {
                    // Если первое значение, то заводим группу
                    if (curr_nom[nom] == 0)
                    {
                        groups[nom] = new TITree(prop_id, new TTree[prop_count[nom]]);  
                    }
                    TITree igroup = (TITree)groups[nom];
                    igroup.Sources[curr_nom[nom]] = GetTTree((string)rest[1], level - 1, prop_id);
                    curr_nom[nom] += 1;
                }
            }
            // Оставляем непустые

            TTree result = new TTree(id, tp, groups);
            return result;
        }
    }

}
