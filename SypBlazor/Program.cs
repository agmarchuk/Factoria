using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MatBlazor;
//using SypBlazor.Data;
using Family.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using SypBlazor.Data;
//using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
//builder.Services.ProtectedLocalStorage();
//builder.Services.AddScoped<StateMemory>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddSingleton<Factograph.Data.IFDataService, DataService>();
builder.Services.AddSingleton<UserAccountService>();
builder.Services.AddMatBlazor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapRazorPages();
app.MapFallbackToPage("/_Host");
app.MapControllerRoute(name: "default",
         pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapGet("/", (Factograph.Data.IFDataService db) =>
{
    if (db.precalculated == null) // Предвычисления
    {
        db.precalculated = new object[2];
        Dictionary<string, string[]> targets = new Dictionary<string, string[]>();
        targets.Add("http://fogid.net/o/info-source", new string[] { "http://fogid.net/o/person", "http://fogid.net/o/org-sys" });
        targets.Add("http://fogid.net/o/author", new string[] { "http://fogid.net/o/person", "http://fogid.net/o/org-sys" });
        targets.Add("http://fogid.net/o/location-place", new string[] { "http://fogid.net/o/city", "http://fogid.net/o/country", "http://fogid.net/o/region", "http://fogid.net/o/geosys-special" });

        ((object[])db.precalculated)[1] = targets;
    }
    return Results.Redirect("~/index/Cassette_20211014_tester_637763849054494762_1019");
});

// ====== Предвычисления =======
//var db = app.Services.GetService(typeof(DataService));

app.Run();
