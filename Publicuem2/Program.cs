using Microsoft.AspNetCore.Http;
using Publicuem2;
using System.Net.Mime;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<Factograph.Data.IFDataService, Factograph.Data.FDataService>();
var app = builder.Build();

//app.MapGet("/", () => "Hello World!");
//app.MapGet("/", (HttpRequest request) => "Hello World!");
app.MapGet("/", Sbor.CreateHtml);
//app.MapGet("/docs", (HttpRequest request) => 
//{
//    return new MimeResult("wwwroot/soldat.jpg", "image/jpeg");
//});
app.MapGet("/docs", ShowDocs.CreateMime);

app.Run();
