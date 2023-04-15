using Family.Authentication;
using FamilyWeb.Handlers;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Mime;
using System.Text;
using System.Xml.Linq;

var builder = WebApplication.CreateBuilder(args);

// Added as service
//builder.Services.AddSingleton<Service>();
builder.Services.AddScoped<ProtectedSessionStorage>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddSingleton<FactographData.IFDataService, FactographData.FDataService>();
builder.Services.AddSingleton<UserAccountService>();

var app = builder.Build();

//app.MapGet("/{id}", (int id,
//                     int page,
//                     [FromHeader(Name = "X-CUSTOM-HEADER")] string customHeader,
//                     Service service) => { });


//app.MapGet("/", ForDelegates.Page1Delegate);
//app.MapGet("/", () => Results.Redirect("/view/qwerty"));
app.MapGet("/view/{id}", ForDelegates.Page2Delegate);
//app.MapGet("/edit/{id}", Page3Delegate);

// Общая часть страниц
Func<XElement, XElement> xSbor = (XElement xwrk) => new XElement("html",
    new XElement("head"),
    new XElement("body", xwrk)
    );
// Начальная страница
app.MapGet("/", (HttpRequest request) => new HtmlResult(xSbor(new XElement("div", "Family site. Empty work page",
        new XElement("form", new XAttribute("method", "post"), new XAttribute("action", "/getlogin"),
            new XElement("input", new XAttribute("name", "login")),
            new XElement("input", new XAttribute("name", "password")),
            new XElement("input", new XAttribute("type", "submit")))
        )).ToString()));
app.MapPost("/getlogin", Input.Login);

//// view-страница
//app.MapGet("/view/{id}", (string? id, HttpRequest request) =>
//{
//    return new HtmlResult(xSbor(new XElement("h1", "Empty work page")).ToString());
//});
//// Альтернативные варианты:
//app.MapGet("/{id}", (HttpRequest request) =>
//{
//    var id = request.RouteValues["id"];
//    var page = request.Query["page"];
//    var customHeader = request.Headers["X-CUSTOM-HEADER"];
//    // ...
//});
//app.MapPost("/", async (HttpRequest request) =>
//{
//    var person = await request.ReadFromJsonAsync<Person>();

//    // ...
//});

app.Run();

class Service { }

class ForDelegates
{
    public static HtmlResult Page1Delegate(HttpRequest request)
    {
        return new HtmlResult(new XElement("h1", "Empty work page").ToString());
    }
    public static HtmlResult Page2Delegate(FactographData.IFDataService db, HttpRequest request)
    {
        var id = request.RouteValues["id"];
        var page = request.Query["page"];
        var query = db.SearchByName("Марчук");
        string toprint = $"[{id}] [{page}] [{query.Count()}]";
        return new HtmlResult(new XElement("h1", "Empty work page: " + toprint).ToString());
    }
}

static class ResultsExtensions
{
    public static IResult Html(this IResultExtensions resultExtensions, string html)
    {
        ArgumentNullException.ThrowIfNull(resultExtensions);

        return new HtmlResult(html);
    }
}

class HtmlResult : IResult
{
    private readonly string _html;

    public HtmlResult(string html)
    {
        _html = html;
    }

    public Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.ContentType = MediaTypeNames.Text.Html;
        httpContext.Response.ContentLength = Encoding.UTF8.GetByteCount(_html);
        return httpContext.Response.WriteAsync(_html);
    }
}