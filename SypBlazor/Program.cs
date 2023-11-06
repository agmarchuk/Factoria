using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MatBlazor;
//using SypBlazor.Data;
using Family.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
//using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
//builder.Services.ProtectedLocalStorage();
//builder.Services.AddScoped<StateMemory>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddSingleton<FactographData.IFDataService, FactographData.FDataService>();
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
app.MapFallbackToPage("/_Host");
app.MapControllerRoute(name: "default",
         pattern: "{controller=Home}/{action=Index}/{id?}");
         
app.Run();
