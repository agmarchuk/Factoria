using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<Factograph.Data.IFDataService, Factograph.Data.FDataService>();
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

// Перезагрузка
app.MapGet("~/room216", (Factograph.Data.IFDataService db) =>
{
    db.Reload();
    return Results.Redirect("/index");
});

// Выдача мультимедиа документов
app.MapGet("~/photo", (HttpRequest request, Factograph.Data.IFDataService db) =>
{
    string? uri = request.Query["u"].FirstOrDefault();
    if (uri == null) return Results.Empty;
    string? sz = request.Query["s"].FirstOrDefault();
    string s = sz ?? "normal";
    string path = db.GetFilePath(uri, s);
    if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path + ".jpg"))
    {
        return Results.Empty;
    }
    return Results.File(path + ".jpg", "image/jpeg");
});
app.MapGet("~/video", (HttpRequest request, Factograph.Data.IFDataService db) =>
{
    string? uri = request.Query["u"].FirstOrDefault();
    if (uri == null) return Results.Empty;
    string? path = db.GetFilePath(uri, "medium");
    if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path + ".mp4"))
    {
        return Results.Empty;
    }
    return Results.File(path + ".mp4", "video/mp4");
});
app.MapGet("~/audio", (HttpRequest request, Factograph.Data.IFDataService db) =>
{
    string? uri = request.Query["u"].FirstOrDefault();
    if (uri == null) return Results.Empty;
    string? path = db.GetOriginalPath(uri);
    if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path + ".mp3"))
    {
        return Results.Empty;
    }
    return Results.File(path + ".mp3", "video/mp3");
});
app.MapGet("~/doc", (HttpRequest request, Factograph.Data.IFDataService db) =>
{
    string? uri = request.Query["u"].FirstOrDefault();
    if (uri == null) return Results.Empty;
    string? path = db.GetOriginalPath(uri);
    if (string.IsNullOrEmpty(path)) return Results.NotFound();
    int pos = path.LastIndexOf(".");
    int pos1 = path.LastIndexOfAny(new char[] { '/', '\\' });
    string filename = path.Substring(pos1 + 1);
    return Results.File(path, "application/" + path.Substring(pos + 1));
});

app.Run();
