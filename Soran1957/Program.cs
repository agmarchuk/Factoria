var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<Factograph.Data.IFDataService, Factograph.Data.FDataService>();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromSeconds(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();
app.UseStaticFiles();
app.UseSession();



app.MapGet("/", Soran1957.SborSoran1957.CreateHtml);
app.MapGet("/docs", Soran1957.ShowDocs.CreateMime);
//app.MapGet("/room216", Sbor.Reload);


//app.MapGet("/", () => "Hello World! Привет Мир!");

app.Run();
