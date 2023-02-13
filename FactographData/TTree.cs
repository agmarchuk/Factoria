using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FactographData
{
    public class TTree
    {
        public string Id { get; private set; }
        public string Tp { get; private set; }
        public TGroup[] Groups { get; private set; }
        public TTree(string id, string tp, TGroup[] groups)
        {
            this.Id = id;
            this.Tp = tp;
            this.Groups = groups;
        }
    }
    //public enum TVid { fields, directs, inverses }
    public abstract class TGroup
    {
        //public TVid Vid { get; internal set; }
        public string Pred { get; internal set; } = "";
        //public TObject[] Objs { get; internal set; }
    }
    //public abstract class TObject {}
    public class TString : TGroup
    {
        public string Value { get; private set; }
        public TString(string pred, string val)
        {
            //this.Vid = TVid.fields; 
            this.Pred = pred;
            this.Value = val;
        }
    }

    public class TTexts : TGroup
    {
        public TextLan[] Values; // { get; private set; }
        public TTexts(string pred, TextLan[] vals)
        {
            //this.Vid = TVid.fields; 
            this.Pred = pred;
            this.Values = vals;
        }
    }
    public class TextLan
    {
        public string Text { get; private set; }
        public string Lang { get; private set; }
        public TextLan(string text, string lang) { this.Text = text; this.Lang = lang; }
    }
    public class TDTree : TGroup
    {
        public TTree Resource { get; private set; }
        public TDTree(string pred, TTree resource) { this.Pred = pred; this.Resource = resource; }
    }

    // WARNING! == Этот вариант TGroup неправильный: нет двухуровнего разбиения свойства - классы
    public class TITree : TGroup
    {
        public TTree[] Sources { get; private set; }
        public TITree(string pred, TTree[] sources)
        {
            this.Pred = pred;
            this.Sources = sources;
        }
    }
    // =========== Двухуровневое разбиение: =========
    /// Вспомогательный класс - группировка списков обратных свойств
    public class InversePropType
    {
        public string Prop;
        public InversePropType(string _prop) { Prop = _prop; }
        public InverseType[] lists = Array.Empty<InverseType>();
    }
    /// Вспомогательный класс - группировка списков по типам ссылающихся записей
    public class InverseType
    {
        public string Tp; 
        public InverseType(string _tp) { Tp = _tp; }
        public TTree[] list = Array.Empty<TTree>();
    }
    // ======== Это четвертый вариант =========
    public class TInverseGroup : TGroup
    {
        public InversePropType[] SourceTypes { get; set; } = Array.Empty<InversePropType>();       
        public TInverseGroup(string pred)
        {
            this.Pred = pred;
        }
    }

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
            var shablon = dic_specs[tp];
            // Здесь возможно надо упорядочить (отсортировать) элементы шаблона
            // ...
            // Теперь создадим массив, длиной oprops - номера свойств в шаблоне
            int[] nomsinshablon = new int[oprops.Length];
            // И массив целых, длиной в шаблон. Это будет к-во стрелок в группе
            int[] prop_count = Enumerable.Repeat<int>(0, shablon.Length).ToArray(); 
            // Сканируем массив свойств с подсчетом и вычислениями
            for (int i = 0; i<oprops.Length; i++)
            {
                var oprop = (object[])oprops[i];
                int v = (int)oprop[0]; // 1 - field, 2 - direct, 3 - inverse
                object[] rest = (object[])oprop[1];
                string prop_id = (string)rest[0];
                // Пока нет дополнительного словаря, находим строчку шаблона перебором
                // Строчку такую, что вид строчки и вид шаблона совпадают, как и ид свойства 
                // выявляем шаблонную пару, из нее получаем и номер и сам шаблон
                var shpair = shablon.Select((p, i) => new {p, i})
                    .FirstOrDefault(pair => pair.p.propId == prop_id && (
(v == 1 && (pair.p.vid == PropVid.tstring || pair.p.vid == PropVid.ttexts)) ||
(v == 2 && (pair.p.vid == PropVid.direct)) ||
(v == 3 && (pair.p.vid == PropVid.inverse))
                        ));
                if (shpair == null) throw new Exception("243434");
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
            TGroup[] groups = new TGroup[shablon.Length];
            // Сканируем свойства
            for (int i = 0; i < oprops.Length; i++)
            {
                var oprop = (object[])oprops[i];
                int v = (int)oprop[0]; // 1 - field, 2 - direct, 3 - inverse
                object[] rest = (object[])oprop[1];
                string prop_id = (string)rest[0];
                // Смотрим что надо создавать. По индексу находим номер шаблона
                int nom = nomsinshablon[i];
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
                    groups[nom] = new TDTree((string)rest[0], GetTTree((string)rest[1], level - 1));
                }
                else if (prop_spec.vid == PropVid.inverse && level > 1)
                {
                    // Если первое значение, то 
                    if (curr_nom[nom] == 0)
                    {
                        //groups[nom] = new TITree((string)rest[1], new TTree[]
                        //    GetTTree((string)rest[2], level - 1));
                    }
                }
            }
            // Оставляем непустые

            TTree result = new TTree(id, tp, groups);
            return result;
        }
    }

    public class TRecordBuilder
    {
        private OAData.Adapters.UpiAdapter adapter;
        private IOntology ontology;
        // Класс позволяет строить деревья TTree, опираясь на базу данных и онтологию
        public TRecordBuilder(OAData.Adapters.UpiAdapter adapter, IOntology ontology)
        {
            this.adapter = adapter;
            this.ontology = ontology;
        }

        public TTree BuildTRecord(string recId, int level = 2, string forbidden = null)
        {
            // Если level = 0 - только поля, 1 - поля и прямые ссылки,  2 - поля, прямые ссылки и обратные ссылки
            object[] erec = (object[])adapter.GetRecord(recId); //(new RYEngine(db)).GetRRecord(recId, level > 1);
            if (erec == null) return null;
            string Id = (string)erec[0];
            string Tp = (string)erec[1];
            object[] oprops = (object[])erec[2];

            // Списки, регулирующие выходные наборы
            List<Tuple<int, string>> vidpred_list = new List<Tuple<int, string>>();
            List<int> ningroup = new List<int>();
            // Список, регулирующий входной набор
            Tuple<int, int>[] noutkingroup = new Tuple<int, int>[oprops.Length];
            // Заполним списки
            for (int i = 0; i < oprops.Length; i++)
            {
                object[] oprop = (object[])oprops[i];
                int tag = (int)oprop[0];
                if (tag == 2 && level < 1 || tag == 3 && level < 2) continue;
                object[] vprop = (object[])oprop[1];
                if (tag == 2 && (string)vprop[0] == forbidden) continue;
                string pred = (string)vprop[0];
                Tuple<int, string> t = new Tuple<int, string>(tag, pred);
                int pos = vidpred_list.IndexOf(t);
                if (pos == -1)
                {
                    pos = vidpred_list.Count;
                    vidpred_list.Add(t);
                    ningroup.Add(0);
                }
                noutkingroup[i] = new Tuple<int, int>(pos, ningroup[pos]);
                ningroup[pos] += 1;
            }

            // Теперь сформируем массив 
            //TGroup[] groups = new TGroup[ningroup.Count];
            TGroup[] groups = Enumerable.Range(0, ningroup.Count)
                .Select<int, TGroup>(i =>
                {
                    var vidpred = vidpred_list[i];
                    TGroup group = null;
                    if (vidpred.Item1 == 1)
                    {
                        group = new TTexts(vidpred.Item2, new TextLan[ningroup[i]]);
                    }
                    else if (vidpred.Item1 == 2 && level > 0)
                    {
                        group = new TDTree(vidpred.Item2, null); // потом надо будет заменить
                    }
                    else if (vidpred.Item1 == 3 && level > 1)
                    {
                        group = new TITree(vidpred.Item2, new TTree[ningroup[i]]);
                    }
                    //else throw new Exception("err: 2984");
                    return group;
                }).ToArray();

            // Заполним его в новом сканировании
            for (int i = 0; i < oprops.Length; i++)
            {
                object[] oprop = (object[])oprops[i];
                int tag = (int)oprop[0];
                if (tag == 2 && level < 1 || tag == 3 && level < 2) continue;
                object[] vprop = (object[])oprop[1];
                if (tag == 2 && (string)vprop[0] == forbidden) continue;
                // 
                Tuple<int, int> gk = noutkingroup[i]; // номер в массиве жгутов, номер в жгуте
                int g = gk.Item1;
                int k = gk.Item2;
                TGroup group = groups[g];
                if (vidpred_list[g].Item1 == 1)
                {
                    TTexts txts = (TTexts)group;
                    txts.Values[k] = new TextLan((string)vprop[1], (string)vprop[2]);
                }
                else if (vidpred_list[g].Item1 == 2)
                {
                    string idd = (string)vprop[1];
                    groups[g] = new TDTree(groups[g].Pred, BuildTRecord(idd, level - 1));
                }
                else if (vidpred_list[g].Item1 == 3)
                {
                    TITree inverse_gr = (TITree)group;
                    string idd = (string)vprop[1];
                    inverse_gr.Sources[k] = BuildTRecord(idd, level - 1, (string)vprop[0]);
                }
            }
            return new TTree(Id, Tp, groups);
        }
    }
}