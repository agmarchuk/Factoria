using Factograph.Data.r;
using Factograph.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Xml.Linq;
using System.Runtime.CompilerServices;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<Factograph.Data.IFDataService, Factograph.Data.FDataService>();

var app = builder.Build();


//app.MapGet("/", () => "Hello World!");
app.MapGet("/", () => Results.Content(
    $@"<html><head><meta charset='utf-8'</head><body>
<div><a href='xml/search/кассеты'>кассеты</a></div>
<div><a href='xml/portrait/cassetterootcollection'>Коллекция кассет</a></div>
<div><a href='photo/uri/uri'>Фото uri</a></div>
</body></html>
", "text/html"));
app.MapGet("/xml/search/{ss}", (Factograph.Data.IFDataService db, HttpRequest request, string ss) =>
{
    var sbor = new XElement("items", db.SearchByName(ss));
    return Results.Content($"{sbor}", "text/xml");
});
app.MapGet("/xml/portrait/{id}", (Factograph.Data.IFDataService db, HttpRequest request, string id) =>
{
    var portrait = db.GetItemByIdBasic(id, true);
    return Results.Content($"{portrait}", "text/xml");
});
app.MapGet("/xml/tree/{id}", (Factograph.Data.IFDataService db, HttpRequest request, string id) =>
{
    var rrec = db.GetRRecord(id, true);
    XElement tree = new XElement("empty");
    if (rrec != null)
    {
        var shablon = Rec.GetUniShablon(rrec.Tp, 2, null, db.ontology);
        var rec = Rec.Build(rrec, shablon, db.ontology, idd => db.GetRRecord(idd, false));
        tree = Util.RecToXml(rec);
    }
    return Results.Content($"{tree.ToString()}", "text/xml");
});
app.MapGet("/photo/{uri}", (Factograph.Data.IFDataService db, HttpRequest request, string uri) =>
{
    string? c = db.CassDirPath(uri);
    string cdp = c ?? "";
    //if (cdp == null) return EmptyResult;
    string fn = cdp;
    string ct = "image/jpeg";
    return new MimeResult(fn, ct);
});
app.MapGet("/video/{uri}", (Factograph.Data.IFDataService db, HttpRequest request, string uri) =>
{
    string fn = "wwwroot/img/0005.mp4";
    string ct = "video/mp4";
    return new MimeResult(fn, ct);
});
app.Run();

class Util
{
    public static XElement RecToXml(Rec rec)
    {
        int lastslash = rec.Tp.LastIndexOf('/');
        return new XElement("r", new XAttribute("tp", rec.Tp.Substring(lastslash + 1)), new XAttribute("id", rec.Id), rec.Props
            .Select(p =>
            {
                string pred = p.Pred; int last = pred.LastIndexOf('/'); string pr = pred.Substring(last + 1);
                if (p is Str)
                {
                    Str s = (Str) p;
                    if (!string.IsNullOrEmpty(s.Value)) return new XElement("s", new XAttribute("p", pr), s.Value);
                }
                else if (p is Tex)
                {
                    Tex t = (Tex) p;
                    if (t.Values.Length > 0) return new XElement("t", new XAttribute("p", pr), t.Values
                        .Select(v => new XElement("v", new XAttribute("lang", v.Lang), v.Text)));                        
                }
                else if (p is Dir)
                {
                    Dir d = (Dir) p;
                    if (d.Resources.Length > 0) return new XElement("d", new XAttribute("p", pr), d.Resources
                        .Select(r => RecToXml(r)));
                }
                else if (p is Inv)
                {
                    Inv inv = (Inv) p;
                    if (inv.Sources.Length > 0) return new XElement("i", new XAttribute("p", pr), inv.Sources
                        .Select(s => RecToXml(s)));
                }
                return null;
            }));
    }
}
public class MimeResult : IResult
{
    //private readonly string _fn, _ct;
    private string _fn, _ct;

    public MimeResult(string fn, string ct)
    {
        _fn = fn;
        _ct = ct;
    }

    public Task ExecuteAsync(HttpContext httpContext)
    {
        //string filename = "wwwroot/soldat.jpg";
        httpContext.Response.ContentType = _ct;

        //byte[] bytes = File.ReadAllBytes(filename);
        //httpContext.Response.ContentLength = bytes.LongLength;
        Console.WriteLine("file " + _fn);
        if (File.Exists(_fn))
        {
            return httpContext.Response.SendFileAsync(_fn);
        }
        else
        {
            return httpContext.Response.SendFileAsync("wwwroot/error.jpg");
        }
    }
}