using FactographData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest
{
    public class RecBuilder
    {
        private Func<string, RRecord?> getRecord;
        private Func<string, RRecord[]?> getInverseRecords;
        private Rec shablon;
        public RecBuilder(Func<string, RRecord?> getRecord, 
            Func<string, RRecord[]?> getInverseRecords,
            Rec shablon)
        {
            this.getRecord = getRecord;
            this.getInverseRecords = getInverseRecords;
            this.shablon = shablon;
            // Заполним словари
            directInShablon = shablon.Props
                .Select((p, i) => new Tuple<Pro, int>(p, i))
                .Where(pa => !(pa.Item1 is Inv))
                .ToDictionary(pa => pa.Item1.Pred, pa => pa.Item2);
        }
        private Dictionary<string, int> directInShablon;
        private Dictionary<string, int> inverseInShablon;

        public Rec? ToRec(string id, string? forbidden)
        {
            RRecord? r = getRecord(id);
            if (r == null) return null;
            Rec result = new Rec(r.Id, r.Tp);
            // Следуем шаблону. Подсчитаем количество стрелок
            int[] nprops = Enumerable.Repeat<int>(0, shablon.Props.Length)
                .ToArray();
            // Другой массив говорит к какому свойству шаблона относится i-й элемент
            int[] nominshab = new int[r.Props.Length];
            for (int i = 0; i < r.Props.Length; i++)
            {
                RProperty p = r.Props[i];
                int nom = -1;
                if (p is RField)
                {
                    var pair = Enumerable.Range(0, shablon.Props.Length)
                        .Select(i => new Tuple<int, Pro>(i, shablon.Props[i]))
                        .FirstOrDefault(pa =>
                            pa.Item2.Pred != null &&
                            pa.Item2.Pred == p.Prop &&
                            (pa.Item2 is Str || pa.Item2 is Tex));
                    if (pair != null) nom = pair.Item1;
                }
                else if (p is RLink)
                {
                    var pair = Enumerable.Range(0, shablon.Props.Length)
                        .Select(i => new Tuple<int, Pro>(i, shablon.Props[i]))
                        .FirstOrDefault(pa =>
                            pa.Item2.Pred != null &&
                            pa.Item2.Pred == p.Prop &&
                            (pa.Item2 is Dir));
                    if (pair != null) nom = pair.Item1;
                }
                else if (p is RInverseLink)
                {
                    var pair = Enumerable.Range(0, shablon.Props.Length)
                        .Select(i => new Tuple<int, Pro>(i, shablon.Props[i]))
                        .FirstOrDefault(pa =>
                            pa.Item2.Pred != null &&
                            pa.Item2.Pred == p.Prop &&
                            (pa.Item2 is Inv));
                    if (pair != null) nom = pair.Item1;
                }
                else throw new Exception("sfwefg");
                if (nom != -1)
                {
                    nprops[nom] += 1;
                    nominshab[i] = nom;
                }
            }
            // Теперь заполним
            return null;
        }
    }

    // ========== Rec классы ==========
    public class Rec
    {
        public string? Id { get; private set; }
        public string Tp { get; private set; }
        public Pro[] Props { get; private set; }
        public Rec(string? id, string tp, params Pro[] props)
        {
            this.Id = id;
            this.Tp = tp;
            this.Props = props;
        }
    }
    public abstract class Pro
    {
        public string Pred { get; internal set; } = "";
    }
    public class Tex : Pro
    {
        public TextLan[]? Values { get; internal set; }
        public Tex(string pred, params TextLan[]? values)
        {
            this.Pred = pred;
            this.Values = values;
        }
    }
    public class Str : Pro
    {
        public string? Value { get; private set; }
        public Str(string pred)
        {
            this.Pred = pred;
        }
        public Str(string pred, string? value)
        {
            this.Pred = pred;
            this.Value = value;
        }
    }
    public class Dir : Pro
    {
        public Rec[] Resources { get; private set; }
        public Dir(string pred, params Rec[] resources) 
        {
            Pred = pred;
            Resources = resources;
        }
    }
    public class Inv : Pro
    {
        public Rec[] Sources { get; private set; }
        public Inv(string pred, params Rec[] sources)
        {
            Pred = pred;
            Sources = sources;
        }
    }

}
