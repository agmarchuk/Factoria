using Factograph.Data;
using Factograph.Data.r;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Uno.Controllers;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseStaticFiles();

Factograph.Data.IFDataService db = new Factograph.Data.FDataService();



app.MapGet("~/", () => 
    //Results.Redirect("/view")); //"/view/syp2001-p-marchuk_a"));
    Results.Redirect("/view/syp2001-p-marchuk_a"));
app.MapGet("~/room216", () => { db.Reload(); Results.Redirect("/view"); }); //"/view/syp2001-p-marchuk_a"));

// Функция получения html-блока записи параметры rrec, forbidden, level. Результат - строка
Func<RRecord, string?, int, string> RRecToHtml = (rrec, forbidden, level) => "";
RRecToHtml = (rrec, forbidden, level) =>
{
    var properties = rrec.Props.Select(p =>
    {
        int m = 80 - level * 20;
        string pred = p.Prop;
        string inner = "";
        if (p is RField)
        {
            var f = (RField)p;
            inner = $"f^({pred} {f.Value})";
        }
        else if (level > 0 && p is RLink && pred != forbidden)
        {
            var dir = (RLink)p;
            string idd = dir.Resource;
            var rresource = db.GetRRecord(idd, false);
            if (rresource == null)
            {
                inner = $"d^({pred}, {dir.Resource})";
            }
            else
            {
                inner = $"d^({pred} {RRecToHtml(rresource, null, level + 1)}";
            }
                
        }
        else if (level > 0 && p is RInverseLink)
        {
            var inv = (RInverseLink)p;
            string idd = inv.Source;
            var rsource = db.GetRRecord(idd, false);
            if (rsource == null)
            {
                inner = $"i^({pred} {inv.Source})";
            }
            else
            {
                inner = $"i({pred} {RRecToHtml(rsource, pred, level + 1)}";
            }
        }
        return $"<div style='margin-left:{m}'>" + inner + "</div>";
    })
    .Aggregate((sum, s) => sum + s);

    return @$"
<div style='margin-left:{80 - level * 20}px;'>
r(id: {rrec.Id} tp: {rrec.Tp}
  {properties ?? ""}
</div>
";
}; 

app.MapGet("~/view/{id?}", (HttpRequest request, string? id) =>
{
    var rr = db.GetRRecord(id, true);
    string portr = "";
    if (rr != null)
    {
        portr = RRecToHtml(rr, null, 2);
        //portr = " id=" + rr.Id + " tp=" + rr.Tp + "\n" +
        //    rr.Props.Select(p =>
        //    {
        //        string pred = p.Prop;
        //        if (p is RField)
        //        {
        //            var f = (RField)p;
        //            return $"f^({pred}, {f.Value})\n";
        //        }
        //        else if (p is RLink)
        //        {
        //            var dir = (RLink)p;
        //            return $"d^({pred}, {dir.Resource})\n";
        //        }
        //        else if (p is RInverseLink)
        //        {
        //            var inv = (RInverseLink)p;
        //            return $"i^({pred}, {inv.Source})\n";
        //        }
        //        else
        //        {
        //            return "\n";
        //        }
        //    }).Aggregate((sum, s) => sum + s);
    }
    // Соберем страницу из поиска и портрета
    string page = $@"<!DOCTYPE html>
<html><head> <meta charset='utf-8'> <link rel='stylesheet' type='text/css' href='/css/Site.css' > </link> </head>
    <body>
        {SearchPanel(request)}
        {BuildPortrait(id, null, 2)}
<hr/>

{portr}

    </body>
</html>";
    return Results.Content(page, "text/html");
});

string SearchPanel(HttpRequest request)
{
    string? ss = request.Query["ss"];
    string? w = request.Query["w"];
    string chcked = w == "on" ? "checked" : ""; 
    string html = $@"
<div>
  <form method='get' action=''>
    <input type='text' name='ss' value='{ss}'>
    <input type='checkbox' name='w' {chcked}>
  </form>
</div>";
    if (ss != null)
    {
        var rres = db.SearchRRecords(ss, w == "on");
        string searchresults = "";
        foreach (var r in rres)
        {
            string tip = db.ontology.LabelOfOnto(r.Tp);
            string id = r.Id;
            string name = r.GetName();
            searchresults += $@"
<div>
  {tip} <a href='/view/{id}'>{name}</a>
</div>
";
        }
        html += searchresults;
    }
    return html;
}
string BuildPortrait(string id, string? forbidden, int level)
{
    // Получим RRec
    var rr = db.GetRRecord(id, true);
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
            sb.Append($"<th class='grid'>{header}</th>");
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
                        cell = $"<a href='/view/{idd}'>{nam}</a>";
                    }
                }
            }
            sb.Append($"<td class='grid'>{cell}</td>");
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
                    sb.Append($"<th class='grid'>{nm}</th>");
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
                        } else if (pr is RLink)
                        {
                            RLink rl = (RLink)pr;
                            var target = db.GetRRecord(rl.Resource, false);
                            cells[ind] = $"<a href='{target?.Id}'>{target?.GetName()}</a>";
                        }
                    }
                    sb.Append("<tr>");
                    sb.Append(cells.Select(cell => $"<td class='grid'>{cell}</td>")
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

app.MapGet("~/photo", (HttpRequest request) =>
{
    string? uri = request.Query["uri"].FirstOrDefault();
    string? sz = request.Query["size"].FirstOrDefault();
    string s = sz ?? "small";
    string path = db.GetFilePath(uri, s);
    if (!System.IO.File.Exists(path + ".jpg"))
    {
        s = s == "medium" ? "normal" : "medium";
        path = db.GetFilePath(uri, s);
    }
    if (string.IsNullOrEmpty(path))
    {
        return Results.Empty; //new EmptyResult();
    }
    //return PhysicalFile(path + ".jpg", "image/jpg");
    return Results.File(path + ".jpg", "image/jpeg");
});

app.MapGet("~/video", (HttpRequest request) =>
{
    string? uri = request.Query["uri"].FirstOrDefault();
    if (uri == null) return Results.Empty;
    string path = db.GetFilePath(uri, "medium");
    if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path + ".mp4"))
    {
        return Results.Empty; //new EmptyResult();
    }
    return Results.File(path + ".mp4", "video/mp4");
});

app.MapGet("~/document", (HttpRequest request) =>
{
    string? uri = request.Query["uri"].FirstOrDefault();
    if (uri == null) return Results.Empty;
    string? path = db.GetOriginalPath(uri);
    if (path == null) return Results.NotFound();
    int pos1 = path.LastIndexOfAny(new char[] { '/', '\\' }); // позиия последнего слеша
    DirectoryInfo di = new DirectoryInfo(path.Substring(0, pos1));
    string filefirst = path.Substring(pos1 + 1);
    var files = di.GetFiles(filefirst + ".*");

    if (files.Length == 1)
    {
        string fname = files[0].Name;
        int lastpoint = fname.LastIndexOf('.');
        if (lastpoint == -1) return Results.Empty;
        string ext = fname.Substring(lastpoint + 1);
        return Results.File(files[0].FullName, $"application/{ext}");
    }
    else return Results.Empty; // Не учтен вариант, когда есть документ и посторонний файл
});

app.Run();


