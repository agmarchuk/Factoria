﻿using System;
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
        // Методы
        public string? GetTString(string propId)
        {
            return Groups
                .Where(g => g is TString && g.Pred == propId)
                .Cast<TString>()
                .Select(ts => ts.Value)
                .FirstOrDefault();
        }
        public TextLan[]? GetTTexts(string propId)
        {
            return Groups
                .Where(g => g is TTexts && g.Pred == propId)
                .Cast<TTexts>()
                .Select(tx => tx.Values)
                .FirstOrDefault();
        }
        public TTree? GetDirect(string propId)
        {
            return Groups
                .Where(g => g is TDTree && g.Pred == propId)
                .Cast<TDTree>()
                .Select(d => d.Resource)
                .FirstOrDefault();
        }

        public IEnumerable<TTree> GetInverseItem(string propId)
        {
            var r = Groups.Where(g => g is TITree && g.Pred == propId)
                .Cast<TITree>()
                .SelectMany(inv => inv.Sources)
                ;
            return r;
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
    // Ошибка! теперь он првильный, буду им пользоваться. 
    public class TITree : TGroup
    {
        public TTree[] Sources { get; private set; }
        public TITree(string pred, TTree[] sources)
        {
            this.Pred = pred;
            this.Sources = sources;
        }
    }

    public class TInvTree : TGroup
    {
        public TTypedInv[] Sources { get; private set; }
        public TInvTree(string pred, TTypedInv[] sources)
        {
            this.Pred = pred;
            this.Sources = sources;
        }
    }

    public class TTypedInv : TGroup
    {
        public TTree[] Sources { get; private set; }
        public TTypedInv(string pred, TTree[] sources)
        {
            this.Pred = pred;
            this.Sources = sources;
        }
    }

    // =========== Двухуровневое разбиение: =========
    /// Вспомогательный класс - группировка списков обратных свойств
    //public class InversePropType
    //{
    //    public string Prop;
    //    public InversePropType(string _prop) { Prop = _prop; }
    //    public InverseType[] lists = Array.Empty<InverseType>();
    //}
    /// Вспомогательный класс - группировка списков по типам ссылающихся записей
    public class InverseType
    {
        public string Tp;
        public TTree[] list;
        public InverseType(string _tp, TTree[] list) 
        { 
            Tp = _tp;
            this.list = list;
        }
    }
    // ======== Это четвертый вариант - теперь НЕПРАВИЛЬНЫЙ =========
    public class TInverseGroup : TGroup
    {
        public InverseType[] SourceTypeGroups { get; set; }       
        public TInverseGroup(string pred, InverseType[] sourceTypeGroups)
        {
            this.Pred = pred;
            this.SourceTypeGroups = sourceTypeGroups;
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

        public TTree BuildTTree(string recId, int level = 2, string forbidden = null)
        {
            // Если level = 0 - только поля, 1 - поля и прямые ссылки,  2 - поля, прямые ссылки и обратные ссылки
            object[] erec = (object[])adapter.GetRecord(recId); //(new RYEngine(db)).GetRRecord(recId, level > 1);
            if (erec == null) return null;
            string Id = (string)erec[0];
            string Tp = (string)erec[1];
            var oprops = ((object[])erec[2]).GroupBy(prop => ((object[])prop)[0]);
            TGroup[] groups = null;

            foreach (var oprop in oprops)
            {
                int tag = (int)(oprop.Key);
                if (tag == 2 && level < 1 || tag == 3 && level < 2) continue;

                IEnumerable<IGrouping<object, object>> pred_grouped_props = oprop.GroupBy(obj => ((object[])((object[])obj)[1])[0]);
                List<TGroup> resultGroups = new List<TGroup>();
                if (tag == 1) // Fields
                {
                    foreach (var pred_group in pred_grouped_props)
                    {
                        IEnumerable<TextLan> textLans = pred_group
                            .Select(pg => new TextLan((string)((object[])((object[])pg)[1])[1], (string)((object[])((object[])pg)[1])[2]));
                        resultGroups.Add(new TTexts((string)pred_group.Key, textLans.ToArray()));
                    }
                    groups = resultGroups.ToArray();
                }

                if (tag == 2) // Directs
                {
                    foreach (var pred_group in pred_grouped_props)
                    {
                        
                        TTree directTree = BuildTRecord((string)((object[])((object[])pred_group.First())[1])[1], level - 1);// First becasue direct supports only one link
                        resultGroups.Add(new TDTree((string)pred_group.Key, directTree));
                        
                    }
                }


                if (tag == 3) // Inverses
                {
                    foreach (var pred_group in pred_grouped_props)
                    {
                        IEnumerable<TTree> tTrees = pred_group
                            .Select(pg => BuildTRecord((string)((object[])((object[])pg)[1])[1], level - 1, (string)((object[])((object[])pg)[1])[0])); // All TTrees
                        IEnumerable<TTypedInv> typedInvs = tTrees.GroupBy(tree => tree.Tp).Select(x => new TTypedInv(x.Key, x.ToArray())); // TTrees grouped by type
                        resultGroups.Add(new TInvTree((string)pred_group.Key, typedInvs.ToArray()));

                    }
                }

                groups = groups.Concat(resultGroups.ToArray()).ToArray();
            }
            return new TTree(Id, Tp, groups);
        }

        public TTree? BuildTRecord(string recId, int level = 2, string? forbidden = null)
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