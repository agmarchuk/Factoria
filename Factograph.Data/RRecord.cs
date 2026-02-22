using Factograph.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Factograph.Data
{
    public class RRecord
    {
        public string Id { get; private set; } = string.Empty;
        public string Tp { get; private set; } = string.Empty;
        public RProperty[] Props { get; private set; } = Array.Empty<RProperty>();
        private IFDataService db;
        public RRecord(string id, string tp, RProperty[] props, IFDataService db)
        {
            Id = id;
            Tp = tp;
            Props = props;
            this.db = db;
        }

        public string? GetField(string propName)
        {
            RProperty? query = Props.FirstOrDefault(p => p is RField && p.Prop == propName);
            return query == null ? null : ((RField)query).Value;
        }
        public string? GetDirectResource(string propName)
        {
            var prop = this.Props.FirstOrDefault(p => p.Prop == propName);
            if (prop == null) return null;
            if (prop is RLink) return ((RLink)prop).Resource;
            return null;
        }
        public RRecord? GetDirect(string propName)
        {
            if (propName == null) return null;
            var prop = this.Props.FirstOrDefault(p => p is RLink && p.Prop == propName);
            if (prop == null) return null;
            string resource = ((RLink)prop).Resource;
            return db.GetRRecord(resource, false);
        }

        public string GetName()
        {
            return ((RField)this.Props.FirstOrDefault(p => p is RField && p.Prop == "http://fogid.net/o/name"))?.Value;
        }
        public string GetName(string lang)
        {
            var name = ((RField)this.Props.FirstOrDefault(p => p is RField && ((RField)p).Lang == lang && p.Prop == "http://fogid.net/o/name"));
            if (name != null)
            {
                return name.Value;
            }
            else
            {
                name = ((RField)this.Props.FirstOrDefault(p => p is RField && p.Prop == "http://fogid.net/o/name"));
                if (name == null) return null;
                var langName = (name.Lang == null) ? "ru" : name.Lang;
                if (langName != lang)
                {
                    return name.Value + " (" + langName + ")";
                }
                else
                {
                    return name.Value;
                }
            }
        }
        public string GetDates()
        {
            string df = GetField("http://fogid.net/o/from-date");
            string dt = GetField("http://fogid.net/o/to-date");
            return (df == null ? "" : df) + (string.IsNullOrEmpty(dt) ? "" : "-" + dt);
        }

        public string BuildPortrait(string? forbidden, int level)
        {
            // Получим RRec
            // var rr = db.GetRRecord(id, true);
            var rr = this;
            // Сформируем прямые свойства
            var fieldsanddirects = rr?.Props
                .Where(fd => fd is RField || (fd is RLink && fd.Prop != forbidden))
                .ToArray();
            StringBuilder sb = new StringBuilder();
            if (fieldsanddirects != null)
            {
                sb = new StringBuilder("<table>");
                sb.Append("<tr>");
                foreach (var fd in fieldsanddirects)
                {
                    string header = db.ontology.LabelOfOnto(fd.Prop) ?? fd.Prop;
                    sb.Append($"<th class='bor'>{header}</th>");
                }
                sb.Append("</tr>");
                sb.Append("<tr>");
                foreach (var fd in fieldsanddirects)
                {
                    string cell = "";
                    if (fd is RField) { cell = ((RField)fd).Value; }
                    else
                    {
                        var lnk = (RLink)fd;
                        if (lnk != null)
                        {
                            var rec = db.GetRRecord(lnk.Resource, false);
                            if (rec != null)
                            {
                                string idd = rec.Id;
                                string nam = rec.GetName();
                                cell = $"<a href='look/{idd}'>{nam}</a>";
                            }
                        }
                    }
                    sb.Append($"<td class='bor'>{cell}</td>");
                }
                sb.Append("</tr>");
                sb.Append("</table>");
            }
            if (level > 0)
            {
                var inverse = rr?.Props.Where(fd => fd is RInverseLink).Cast<RInverseLink>().ToArray();
                if (inverse != null)
                {
                    sb.Append("<table>");
                    foreach (var pair in inverse.GroupBy(d => d.Prop).Select(pa => (pa.Key, pa)))
                    {
                        string leftheader = db.ontology.InvLabelOfOnto(pair.Key) ?? pair.Key;
                        string pred = pair.Key;
                        sb.Append($"<tr><th style='vertical-align: top;'>{leftheader}</th>");
                        // У данной группы найдем количество обратных ссылок
                        int nlnks = pair.pa.Count();
                        // Создаем множество записей, ссылающихся на данную по данному отношению pred
                        RRecord[] inv_recs = pair.pa
                            .Select(ilnk => db.GetRRecord(ilnk.Source, false))
                            .Where(r => r != null).Cast<RRecord>()
                            .ToArray();
                        // У данной группы найдем множество использованных предикатов (имен свойств)
                        // будем учитывать только поля и прямые ссылки, но без pred (forbidden)
                        string[] predicates = inv_recs.SelectMany(r => r.Props
                            .Where(p => p is RField || (p is RLink && p.Prop != pred)))
                            .Select(p => p.Prop)
                            .Distinct()
                            .ToArray();

                        // Строим таблицу
                        sb.Append("<td><table>");
                        // Рядок заголовков
                        sb.Append("<tr>");
                        foreach (var p in predicates)
                        {
                            string nm = db.ontology.LabelOfOnto(p) ?? p;
                            sb.Append($"<th class='bor'>{nm}</th>");
                        }
                        sb.Append("</tr>");
                        // Делаем словарь для "раскидывания" значений по позициям рядка
                        var dic = predicates.Select((s, i) => new { s, i })
                            .ToDictionary(si => si.s, si => si.i);
                        // Цикл по записям
                        foreach (var r in inv_recs)
                        {
                            // Создадим массив
                            string[] cells = Enumerable.Repeat<string>("", predicates.Length).ToArray();
                            // Заполним массив значениями полей и ссылок записи r
                            foreach (RProperty pr in r.Props
                                .Where(p => p is RField || (p is RLink && p.Prop != pred)))
                            {
                                int ind = dic[pr.Prop];
                                if (pr is RField)
                                {
                                    RField f = (RField)pr;
                                    cells[ind] = f.Value;
                                }
                                else if (pr is RLink)
                                {
                                    RLink rl = (RLink)pr;
                                    var target = db.GetRRecord(rl.Resource, false);
                                    if (target != null)
                                    {
                                        cells[ind] = $"<a href='look/{target?.Id}'>{target?.GetName()}</a>";
                                    }
                                    else
                                    {
                                        cells[ind] = $"<a>[висит {rl.Resource}]</a>";
                                    }
                                }
                            }
                            sb.Append("<tr>");
                            if (cells.Length > 0) sb.Append(cells.Select(cell => $"<td class='bor'>{cell}</td>")
                                .Aggregate((sum, s) => sum + s));
                            sb.Append("</tr>");
                        }
                        sb.Append("</table></td>");
                        //sb.Append($"<td>{pair.pa.Count()} {predicates.Length}</td>");
                        sb.Append("</tr>");
                    }
                    sb.Append("</table>");
                }
            }

            string html = sb.ToString();
            return html;
        }

    }
    public abstract class RProperty
    {
        public string Prop { get; set; }
    }
    public class RField : RProperty
    {
        public string Value { get; set; }
        public string Lang { get; set; }
    }
    public class RLink : RProperty, IEquatable<RLink>
    {
        public string Resource { get; set; }

        public bool Equals(RLink other)
        {
            return this.Prop == other.Prop && this.Resource == other.Resource;
        }

        public int GetHashCode([DisallowNull] RLink obj)
        {
            return obj.Prop.GetHashCode() ^ obj.Resource.GetHashCode();
        }
    }
    // Расширение вводится на странице 11 пособия "Делаем фактографию"
    public class RInverseLink : RProperty
    {
        public string Source { get; set; }
    }


    // Custom comparer for the RRecord class
    public class RRecordComparer : IEqualityComparer<RRecord>
    {
        public bool Equals(RRecord x, RRecord y)
        {
            //Check whether the compared objects reference the same data.
            if (Object.ReferenceEquals(x, y)) return true;

            //Check whether any of the compared objects is null.
            if (Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null))
                return false;
            return x.Id == y.Id;
        }

        // If Equals() returns true for a pair of objects
        // then GetHashCode() must return the same value for these objects.

        public int GetHashCode([DisallowNull] RRecord obj)
        {
            //Check whether the object is null
            if (Object.ReferenceEquals(obj, null)) return 0;
            return obj.Id.GetHashCode();
        }
    }

    // Новое расширение
    public class RDirect : RProperty
    {
        public RRecord DRec { get; set; }
    }
    //public class RInverse : RProperty
    //{
    //    public RRecord IRec { get; set; }
    //}
    //// Еще более новое расширение
    //public class RMultiInverse : RProperty
    //{
    //    public RRecord[] IRecs { get; set; }
    //}

    // Специальное расширение для описателей перечислимых  
    public class RState : RProperty
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public string lang { get; set; }
    }





}
