using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;

namespace FactographData
{
    public class TTree1
    {
        public string Id { get; private set; }
        public string Tp { get; private set; }
        public TGroup[] Groups { get; private set; }
        public TTree1(string id, string tp, TGroup[] groups)
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
        public string Pred { get; internal set; }
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
        public TextLan[] Values { get; private set; }
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
        public TTree1 Resource { get; private set; }
        public TDTree(string pred, TTree1 resource) { this.Pred = pred; this.Resource = resource; }
    }
    /// Вспомогательный класс - группировка списков обратных свойств
    public class InversePropType
    {
        public string Prop;
        public InverseType[] lists;
    }
    /// Вспомогательный класс - группировка списков по типам ссылающихся записей
    public class InverseType
    {
        public string Tp;
        public TTree1[] list;
    }
    public class TITree : TGroup
    {
        public InversePropType[]? SourceTypes { get; set; }
        public TITree(string pred) 
        { 
            this.Pred = pred; 
        }
    }
    public class TRecordBuilder
    {
        //private OAData.Adapters.UpiAdapter adapter;
        private OAData.Adapters.DAdapter adapter;
        private IOntology ontology;

        // Структуры данных для использовании онтологии в преобразовании в TTree

        // Каждому классу по TClass:
        Dictionary<string, TClass> dic_classes;
        class TClass
        {
            internal string TypeId { get; set; } = "";
            internal string? ParentType { get; set; } // Кажется, эта информация не доходит
            internal string? Priority { get; set; } // Для типов актуально ли это?
            internal RField? Label { get; set; }
            internal UseProperty[]? Uprops { get; set; }
            internal Dictionary<string, int>? FieldsAndDirects { get; set; }
            internal Dictionary<string, int>? Inverses { get; set; }
        }
        class UseProperty
        {
            bool _isDirect;
            string _property;
            public UseProperty(bool isDirect, string property)
            {
                _isDirect = isDirect;
                _property = property;
            }
            internal bool IsDirect { get { return _isDirect; } }
            internal string Property { get { return _property; } }
        }
        // Каждому свойству - по определению
        Dictionary<string, TProperty> dic_properties;
        enum PropVid
        {
            tstring, ttexts, direct, inverse
        }
        struct TProperty
        {
            private string _propid;
            private PropVid _vid = PropVid.tstring;
            public TProperty(PropVid vid, string propId) { _vid = vid; _propid = propId; }
            internal PropVid Vid { get { return _vid; } } // inverse используется не всегда
            internal string PropId { get { return _propid; } }
            internal string? Priority { get; set; } = ""; // А я буду использовать?
            internal RField? Label { get; set; } = default;
            internal RField? InverseLabel { get; set; } = default;
            internal string[] Domains { get; set; } = new string[0];
            internal string[] Ranges { get; set; } = new string[0];
        }


        public TRecordBuilder(OAData.Adapters.UpiAdapter adapter, IOntology ontology)
        {
            this.adapter = adapter;
            this.ontology = ontology;
            string dflt_lang = "ru";
            // Подготовка данных для BuildTrecordByOntology. Данные будем брать из
            // ontology.OntoSpec
            // Нужны все типы
            dic_classes = ontology.OntoSpec
                .Where(ospec => ospec.Tp == "Class")
                .Select(ospec =>
                {
                    TClass c = new TClass { TypeId = ospec.Id };

                    foreach (var p in ospec.Props)
                    {
                        if (p is RField)
                        {
                            RField f = (RField)p;
                            if (f.Prop == "priority") //  && f.Value != null
                            { 
                                c.Priority = f.Value; 
                            }
                            else if (f.Prop == "Label")
                            {
                                if (c.Label == null || f.Lang == dflt_lang)
                                c.Label = f;
                            }
                            else  {  }
                        }
                        else if (p is RLink)
                        {
                            RLink d = (RLink)p; // Сейчас ничего не будем делать
                        }
                        else throw new Exception("29489");
                    }
                    // Строим Uprops
                    var dire = ontology.GetDirectPropsByType(ospec.Id).ToArray();
                    var inve = ontology.GetInversePropsByType(ospec.Id).ToArray();
                    c.Uprops = dire.Select(u => new UseProperty(true, u))
                        .Concat(inve.Select(u => new UseProperty(false, u)))
                        .ToArray();
                    // Строим словари пользуясь тем, что была конкатенация
                    c.FieldsAndDirects = dire
                        .Select((u, i) => new Tuple<string, int>(u, i))
                        .ToDictionary(pair => pair.Item1, pair => pair.Item2);
                    int n = dire.Count();
                    c.Inverses = inve
                        .Select((u, i) => new Tuple<string, int>(u, n + i))
                        .ToDictionary(pair => pair.Item1, pair => pair.Item2);
                    return c;
                })
                .ToDictionary(c => c.TypeId);

            // А также обработаем все свойства. Причем обработаем и как прямые и как обратные,
            // информацию поместим в словарь
            var dic_properties1 = ontology.OntoSpec
                .Where(spec => spec.Tp == "DatatypeProperty" || spec.Tp == "ObjectProperty")
                .Select(spec =>
                {
                    TProperty tres;
                    if (spec.Tp == "DatatypeProperty")
                    {
                        tres = new TProperty(PropVid.tstring, spec.Id);
                    }
                    else if (spec.Tp == "ObjectProperty")
                    {
                        tres = new TProperty(PropVid.direct, spec.Id);
                    }
                    else throw new Exception("2948445");
                    // Проанализируем свойства в спецификации
                    foreach (var p in spec.Props)
                    {
                        if (p is RField)
                        {
                            RField f = (RField)p;
                            if (f.Prop == "priority") //  && f.Value != null
                            {
                                tres.Priority = f.Value;
                            }
                            else if (f.Prop == "Label")
                            {
                                if (tres.Label == null || f.Lang == dflt_lang)
                                    tres.Label = f;
                            }
                            else { }
                        }
                        else if (p is RLink)
                        {
                            RLink d = (RLink)p; // Сейчас ничего не будем делать
                        }
                        else throw new Exception("29489");
                    }
                    return tres;
                })
                .ToDictionary(tp => tp.PropId);
            // Теперь обратные. Информацию поместим в словарь
            var dic_properties2 = ontology.OntoSpec
                .Where(spec => spec.Tp == "ObjectProperty")
                .Select(spec =>
                {
                    TProperty tres;
                    if (spec.Tp != "ObjectProperty") throw new Exception("2446765");
                    tres = new TProperty(PropVid.inverse, spec.Id);
                    // Проанализируем свойства в спецификации
                    foreach (var p in spec.Props)
                    {
                        if (p is RField)
                        {
                            RField f = (RField)p;
                            if (f.Prop == "priority") //  && f.Value != null
                            {
                                tres.Priority = f.Value;
                            }
                            else if (f.Prop == "Label")
                            {
                                if (tres.Label == null || f.Lang == dflt_lang)
                                    tres.Label = f;
                            }
                            else { }
                        }
                        else if (p is RLink)
                        {
                            RLink d = (RLink)p; // Сейчас ничего не будем делать
                        }
                        else throw new Exception("29489");
                    }
                    return tres;
                })
                .ToDictionary(tp => tp.PropId);

            // Обработаем каждый тип
            foreach (var kc_pair in dic_classes) 
            {
                string tp = kc_pair.Value.TypeId;
                var c_spec = dic_classes[tp];
            }
        }

        /// <summary>
        /// Построение дерева TTree с использованием онтологии
        /// </summary>
        /// <returns></returns>
        public TTree1? BuildTRecordByOntology(string recId)
        {
            object[] erec = (object[])adapter.GetRecord(recId); //(new RYEngine(db)).GetRRecord(recId, level > 1);
            if (erec == null) return null;
            string id = (string)erec[0];
            string tp = (string)erec[1];
            object[] oprops = (object[])erec[2];
            // Я приготовился к созданию дерева. 
            // По типу определим T-спецификатор класса
            var tspec = dic_classes[tp];
            // Для построения дерева, самое главное построить набор жгутов
            var tgroups = tspec.Uprops
                .Select(u =>
                {
                    TGroup tg;
                    bool isdirect = u.IsDirect;
                    string propId = u.Property;
                    // берем t-спецификацию свойства
                    var tprop = dic_properties[propId];
                    // Определяем вид группы
                    PropVid vid = tprop.Vid;
                    if (vid == PropVid.tstring)
                    {

                    }
                    else if (vid == PropVid.ttexts)
                    {

                    }
                    else if (vid == PropVid.direct)
                    {

                    }
                    else if (vid == PropVid.inverse)
                    {

                    }
                    else throw new Exception("329933");
                    return new TString("peredicateidd", "valueee");
                })
                .ToArray();
            // Попробуем построить результат
            TTree1 tres = new TTree1(id, tp, tgroups);

            return null;
        }
        public TTree1? BuildTRecord(string recId, int level = 2, string? forbidden = null)
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
            for (int i = 0; i<oprops.Length; i++)
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
                        group = new TITree(vidpred.Item2);
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
            return new TTree1(Id, Tp, groups);



            // В зависимости от типа, узнаем количество прямых и обратных свойств и заводим массив t-свойств этого размера  
            int nprops = ontology.PropsTotal(Tp);
            // Массив списков свойств для накапливания информации oprop [tag, [pred, value(, lang)?]]
            List<object>[] lists = new List<object>[nprops];

            // Также заводим массив списков RRecord'ов для накопления сгруппированных полей и обратных свойств
            List<TTree1>[] reclists = new List<TTree1>[nprops];
            /*
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
                            return new TextLan { Text = f.Value, Lang = f.Lang };
                        }).ToArray()
                    };
                }
                else if (level > 0 && p0 is RLink)
                {
                    // Буду фиксировать только первое значение p0
                    RLink lnk = (RLink)p0;
                    t = new TDTree { PropId = p0.Prop, Record = new TRecord(lnk.Resource, db, level - 1, null) };
                }
                else if (level > 1 && p0 is RInverseLink)
                {
                    RInverseLink inv = (RInverseLink)p0;
                    t = new TITree
                    {
                        PropId = p0.Prop,
                        Records = p_list.Cast<RInverseLink>()
                            .Select(ri => new TRecord(ri.Source, db, level - 1, p0.Prop)).ToArray()
                    };
                }
                props[ind] = t;
            }
            */
        }

    }

}
