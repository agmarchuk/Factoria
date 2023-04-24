using System.Text;

namespace FactographData.r
{
    // ========== Rec классы ==========
    public class Rec
    {
        public string? Id { get; private set; }
        public string Tp { get; private set; }
        public Pro[] Props { get; internal set; }
        public Rec(string? id, string tp, params Pro[] props)
        {
            this.Id = id;
            this.Tp = tp;
            this.Props = props;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder($"r({Id}, {Tp}");
            foreach (var p in Props) { sb.Append(", "); sb.Append(p.ToString()); }
            sb.Append(')');
            return sb.ToString();
        }
        // =================== Самое главное: генерация дерева по шаблону ==========
        public static Rec Build(RRecord r, Rec shablon, Func<string, RRecord> getRecord)
        {
            if (r == null) return new Rec("noname", "notype");
            Rec result = new(r.Id, r.Tp);
            // Следуем шаблону. Подсчитаем количество стрелок
            int[] nprops = Enumerable.Repeat<int>(0, shablon.Props.Length)
                .ToArray();
            // Другой массив говорит к какому свойству шаблона относится i-й элемент
            int[] nominshab = Enumerable.Repeat<int>(-1, r.Props.Length).ToArray();
            for (int i = 0; i < r.Props.Length; i++)
            {
                RProperty p = r.Props[i];
                int nom = -1;
                IEnumerable<Tuple<int, Pro>> pairs = Enumerable.Range(0, shablon.Props.Length)
                        .Select(i => new Tuple<int, Pro>(i, shablon.Props[i]))
                        .Where(pa =>
                            pa.Item2.Pred != null &&
                            pa.Item2.Pred == p.Prop);
                if (p is RField)
                {
                    var pair = pairs.FirstOrDefault(pa => pa.Item2 is Str || pa.Item2 is Tex);
                    if (pair != null) nom = pair.Item1;
                }
                else if (p is RLink)
                {
                    var pair = pairs.FirstOrDefault(pa => pa.Item2 is Dir);
                    if (pair != null) nom = pair.Item1;
                }
                else if (p is RInverseLink)
                {
                    var pair = pairs.FirstOrDefault(pa => pa.Item2 is Inv);
                    if (pair != null) nom = pair.Item1;
                }
                else throw new Exception("sfwefg");
                if (nom != -1)
                {
                    nprops[nom] += 1;
                    nominshab[i] = nom;
                }
            }
            // Теперь заполним свойства для результата
            Pro[] pros = new Pro[nprops.Length];
            // Обработаем все номера, и которые нулевые и которые не нулевые в nprops
            for (int j = 0; j < pros.Length; j++)
            {
                //if (nprops[j] == 0) continue;
                var p = shablon.Props[j];
                if (p is Str) pros[j] = new Str(p.Pred, null);
                else if (p is Tex) pros[j] = new Tex(p.Pred, new TextLan[nprops[j]]);
                else if (p is Dir) pros[j] = new Dir(p.Pred, new Rec[nprops[j]]);
                else if (p is Inv) pros[j] = new Inv(p.Pred, new Rec[nprops[j]]);
                else new Exception("928439");
            }
            // Сделаем массив индексов (можно было бы использовать nprops)
            int[] pos = new int[nprops.Length]; // вроде размечается нулями...
            // Снова пройдемся по свойствам записи и "разбросаем" элементы по приготовленным ячейкам.
            for (int i = 0; i < r.Props.Length; i++)
            {
                RProperty p = r.Props[i];
                // Номер в шаблоне берем из nominshab
                int nom = nominshab[i];
                // Если нет в шаблоне, то не рассматриваем
                if (nom == -1) continue;
                // Выясняем какой тип у свойства и в зависимости от типа делаем пополнение
                if (pros[nom] is Str)
                {
                    if (((Str)pros[nom]).Value == null)
                    { // нормально
                        ((Str)pros[nom]).Value = ((RField)p).Value;
                    }
                    else throw new Exception($"Err: too many string values for {((RField)p).Prop}");
                }
                else if (pros[nom] is Tex)
                {
                    var f = (RField)p;
                    ((Tex)pros[nom]).Values[pos[nom]] = new TextLan(f.Value, f.Lang);
                    pos[nom]++;
                }
                else if (pros[nom] is Dir)
                {
                    string id1 = ((RLink)p).Resource;
                    RRecord? r1 = getRecord(id1);
                    var shablon1 = ((Dir)shablon.Props[nom]).Resources
                        .FirstOrDefault(res => res.Tp == r1?.Tp || res.Tp == null);
                    if (shablon1 != null)
                    {
                        Rec r11 = Rec.Build(r1, shablon1, getRecord);
                        ((Dir)pros[nom]).Resources[pos[nom]] = r11;
                        pos[nom]++;
                    }
                }
                else if (pros[nom] is Inv)
                {
                    string id1 = ((RInverseLink)p).Source;
                    RRecord? r1 = getRecord(id1);
                    var shablon1 = ((Inv)shablon.Props[nom]).Sources
                        .FirstOrDefault(res => res.Tp == r1?.Tp);
                    if (shablon1 != null)
                    {
                        Rec r11 = Rec.Build(r1, shablon1, getRecord);
                        ((Inv)pros[nom]).Sources[pos[nom]] = r11;
                        pos[nom]++;
                    }
                }
            }
            // Добавляем pros, устранив нулевые
            result.Props = pros.Where(p => p != null).ToArray();
            return result;
        }


        public static Rec BuildByObj(object[] r, Rec shablon, Func<string, object> getRecord)
        {
            if (r == null) return new Rec("noname", "notype");
            Rec result = new(r[0].ToString(), r[1].ToString());
            // Следуем шаблону. Подсчитаем количество стрелок
            int[] nprops = Enumerable.Repeat<int>(0, shablon.Props.Length)
                .ToArray();
            // Другой массив говорит к какому свойству шаблона относится i-й элемент
            object[] props = (object[])r[2];
            int[] nominshab = Enumerable.Repeat<int>(-1, props.Length).ToArray();
            for (int i = 0; i < props.Length; i++)
            {
                object[] p = (object[])props[i];
                string Pprop = (string)((object[]) p[1])[0];
                int nom = -1;
                IEnumerable<Tuple<int, Pro>> pairs = Enumerable.Range(0, shablon.Props.Length)
                        .Select(i => new Tuple<int, Pro>(i, shablon.Props[i]))
                        .Where(pa =>
                            pa.Item2.Pred != null &&
                            pa.Item2.Pred == Pprop);
                if ((int)p[0] == 1)
                {
                    var pair = pairs.FirstOrDefault(pa => pa.Item2 is Str || pa.Item2 is Tex);
                    if (pair != null) nom = pair.Item1;
                }
                else if ((int)p[0] == 2)
                {
                    var pair = pairs.FirstOrDefault(pa => pa.Item2 is Dir);
                    if (pair != null) nom = pair.Item1;
                }
                else if ((int)p[0] == 3)
                {
                    var pair = pairs.FirstOrDefault(pa => pa.Item2 is Inv);
                    if (pair != null) nom = pair.Item1;
                }
                else throw new Exception("sfwefg");
                if (nom != -1)
                {
                    nprops[nom] += 1;
                    nominshab[i] = nom;
                }
            }
            // Теперь заполним свойства для результата
            Pro[] pros = new Pro[nprops.Length];
            // Обработаем все номера, и которые нулевые и которые не нулевые в nprops
            for (int j = 0; j < pros.Length; j++)
            {

                //if (nprops[j] == 0) continue;
                var p = shablon.Props[j];
                if (p is Str) pros[j] = new Str(p.Pred, null);
                else if (p is Tex) pros[j] = new Tex(p.Pred, new TextLan[nprops[j]]);
                else if (p is Dir) pros[j] = new Dir(p.Pred, new Rec[nprops[j]]);
                else if (p is Inv) pros[j] = new Inv(p.Pred, new Rec[nprops[j]]);
                else new Exception("928439");
            }
            // Сделаем массив индексов (можно было бы использовать nprops)
            int[] pos = new int[nprops.Length]; // вроде размечается нулями...
            // Снова пройдемся по свойствам записи и "разбросаем" элементы по приготовленным ячейкам.
            for (int i = 0; i < props.Length; i++)
            {
                object[] p = (object[])props[i];
                // Номер в шаблоне берем из nominshab
                int nom = nominshab[i];
                // Если нет в шаблоне, то не рассматриваем
                if (nom == -1) continue;
                // Выясняем какой тип у свойства и в зависимости от типа делаем пополнение
                string Pprop = (string)((object[])p[1])[0];
                string Pvalue = (string)((object[])p[1])[1];

                if (pros[nom] is Str)
                {
                    if (((Str)pros[nom]).Value == null)
                    { // нормально
                        ((Str)pros[nom]).Value = Pvalue;
                    }
                    else throw new Exception($"Err: too many string values for {Pvalue}");
                }
                else if (pros[nom] is Tex)
                {
                    string Plang = (string)((object[])p[1])[2];
                    ((Tex)pros[nom]).Values[pos[nom]] = new TextLan(Pvalue, Plang);
                    pos[nom]++;
                }
                else if (pros[nom] is Dir)
                {
                    string id1 = Pvalue;
                    object[]? r1 = (object[])getRecord(id1);
                    var shablon1 = ((Dir)shablon.Props[nom]).Resources
                        .FirstOrDefault(/*res => res.Tp == r1?[1].ToString()*/);
                    if (shablon1 != null)
                    {
                        Rec r11 = Rec.BuildByObj(r1, shablon1, getRecord);
                        ((Dir)pros[nom]).Resources[pos[nom]] = r11;
                        pos[nom]++;
                    }
                }
                else if (pros[nom] is Inv)
                {
                    string id1 = Pvalue;
                    object[]?  r1 = (object[])getRecord(id1);
                    var shablon1 = ((Inv)shablon.Props[nom]).Sources
                        .FirstOrDefault(/*res => res.Tp == r1?[1].ToString()*/);
                    if (shablon1 != null)
                    {
                        Rec r11 = Rec.BuildByObj(r1, shablon1, getRecord);
                        ((Inv)pros[nom]).Sources[pos[nom]] = r11;
                        pos[nom]++;
                    }
                }
            }
            // Добавляем pros, устранив нулевые
            result.Props = pros.Where(p => p != null).ToArray();
            return result;
        }
        // ======= Теперь доступы =======
        public string? GetText(string pred)
        {
            var group = Props.FirstOrDefault(p => p.Pred == pred);
            if (group == null || !(group is Tex) ) return null;
            Tex texts = (Tex)group;
            string? result = null;
            foreach (var t in texts.Values) 
            {
                result = t.Text;
                if (t.Lang == "ru") break;
            }
            return result;
        }

    }
    public abstract class Pro
    {
        public string Pred { get; internal set; } = "";
        //public string? Val 
        //{ 
        //    get 
        //    {
        //        if ( this is Tex)
        //        {
        //            var tex = (Tex)this;
        //            return tex.Values.FirstOrDefault(t => t.Lang == "ru")?.Text;
        //        }
        //        else if (this is Str)
        //        {
        //            return ((Str)this).Value;
        //        }
        //        else { return null; }
        //    }
        //}
    }
    public class Tex : Pro
    {
        public TextLan[] Values { get; internal set; }
        public Tex(string pred, params TextLan[] values)
        {
            this.Pred = pred;
            this.Values = values;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("t(");
            bool firsttime = true;
            foreach (var v in Values)
            {
                if (!firsttime) sb.Append(", ");
                firsttime = false;
                sb.Append($"\"{v.Text}\"");
                if (v.Lang != null) sb.Append("^^" + v.Lang);
            }
            sb.Append(')');
            return sb.ToString();
        }
    }
    public class Str : Pro
    {
        public string? Value { get; internal set; }
        public Str(string pred)
        {
            this.Pred = pred;
        }
        public Str(string pred, string? value)
        {
            this.Pred = pred;
            this.Value = value;
        }
        public override string ToString()
        {
            return $"\"{Value}\"";
        }
    }
    public class Dir : Pro
    {
        public Rec[] Resources { get; internal set; }
        public Dir(string pred, params Rec[] resources)
        {
            Pred = pred;
            Resources = resources;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("d(");
            bool firsttime = true;
            foreach (var res in Resources)
            {
                if (!firsttime) sb.Append(", ");
                firsttime = false;
                sb.Append(res.ToString());
            }
            sb.Append(')');
            return sb.ToString();
        }
    }
    public class Inv : Pro
    {
        public Rec[] Sources { get; internal set; }
        public Inv(string pred, params Rec[] sources)
        {
            Pred = pred;
            Sources = sources;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("i(");
            bool firsttime = true;
            foreach (var res in Sources)
            {
                if (!firsttime) sb.Append(", ");
                firsttime = false;
                sb.Append(res.ToString());
            }
            sb.Append(')');
            return sb.ToString();
        }
    }
}
