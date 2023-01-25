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

    public class Texts : TGroup
    {
        public TextLan[] Values { get; private set; }
        public Texts(string pred, TextLan[] vals) 
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
    public class TITree : TGroup
    {
        public TTree[] Sources { get; private set; }
        public TITree(string pred, TTree[] sources) 
        { 
            this.Pred = pred; 
            this.Sources = sources; 
        }
    }
    public class TRecordBuilder
    {
        private OAData.Adapters.UpiAdapter adapter;
        private IOntology ontology;
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
                        group = new Texts(vidpred.Item2, new TextLan[ningroup[i]]);
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
                    Texts txts = (Texts)group;
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



            // В зависимости от типа, узнаем количество прямых и обратных свойств и заводим массив t-свойств этого размера  
            int nprops = ontology.PropsTotal(Tp);
            // Массив списков свойств для накапливания информации oprop [tag, [pred, value(, lang)?]]
            List<object>[] lists = new List<object>[nprops];

            // Также заводим массив списков RRecord'ов для накопления сгруппированных полей и обратных свойств
            List<TTree>[] reclists = new List<TTree>[nprops];
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
