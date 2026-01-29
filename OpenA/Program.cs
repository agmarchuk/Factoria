var builder = WebApplication.CreateBuilder(args);

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
app.UseStaticFiles();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();
app.MapGet("~/", () =>
    //Results.Redirect("/view")); //"/view/syp2001-p-marchuk_a"));
    Results.Redirect("/index/syp2001-p-marchuk_a"));

app.MapControllerRoute(name: "default",
         pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
