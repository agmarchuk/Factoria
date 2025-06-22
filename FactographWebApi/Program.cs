using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Xml.Linq;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<Factograph.Data.IFDataService, Factograph.Data.FDataService>();

var app = builder.Build();


app.MapGet("/", () => "Hello World!");
app.MapGet("/xml/search/{ss}", (Factograph.Data.IFDataService db, HttpRequest request, string ss) =>
{
    var sbor = new XElement("items", db.SearchByName(ss));
    return Results.Content($"{sbor}", "text/xml");
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