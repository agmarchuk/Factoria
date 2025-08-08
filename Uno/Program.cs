using Factograph.Data;
using Factograph.Data.r;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Uno.Controllers;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseStaticFiles();

Factograph.Data.IFDataService db = new Factograph.Data.FDataService();



app.MapGet("~/", () => Results.Redirect("/view")); //"/view/syp2001-p-marchuk_a"));
app.MapGet("~/room216", () => { db.Reload(); Results.Redirect("/view"); }); //"/view/syp2001-p-marchuk_a"));

// Вход view - основной в сервисе. Может присутствовать основной параметр id - идентификатор сущности. Также могут быть параметры:
// ss (search string) - поисковый образ; tp - тип результата поиска, IsBullOrEmpty(tp) - все типы; bw (by words) - поиск "по словам";
// idd - идентификатор "верхнего" уровня, напр. идентификатор "охватывающей" коллекции.
//ViewController viewCont = new ViewController(db);
VController vCont = new VController(null, db);
app.MapGet("~/view/{id?}", (HttpRequest request, string? id) =>
{
    //string sresult = viewCont.SborkaPage(request, id);
    vCont.OnGet(request);
    string sresult = vCont.portrait;
    return Results.Content(sresult, "text/html");
});

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


