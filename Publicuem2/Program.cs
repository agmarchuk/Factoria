using Microsoft.AspNetCore.Http;
using Publicuem2;
using System.Net.Mime;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<Factograph.Data.IFDataService, Factograph.Data.FDataService>();
var app = builder.Build();

app.UseStaticFiles();



//app.MapGet("/", () => "Hello World!");
//app.MapGet("/doc", (HttpRequest request) =>
//{
//    return new MimeResult("wwwroot/soldat.jpg", "image/jpeg");
//});
app.MapGet("/", Sbor.CreateHtml);
app.MapGet("/docs", ShowDocs.CreateMime);
app.MapGet("/room216", Sbor.Reload);


app.Run();
