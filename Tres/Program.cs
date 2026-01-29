var builder = WebApplication.CreateBuilder(args);

//Factograph.Data.IFDataService db = new Factograph.Data.FDataService();
builder.Services.AddSingleton<Factograph.Data.IFDataService, Factograph.Data.FDataService>();
// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Error");
//    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//    app.UseHsts();
//}

//app.UseHttpsRedirection();

//app.UseRouting();

//app.UseAuthorization();



//app.MapGet("/photo", (HttpRequest request, Factograph.Data.IFDataService db) =>
//{
//    string? uri = request.Query["uri"].FirstOrDefault();
//    string? sz = request.Query["size"].FirstOrDefault();
//    string s = sz ?? "small";
//    string path = db.GetFilePath(uri, s);
//    if (!System.IO.File.Exists(path + ".jpg"))
//    {
//        s = s == "medium" ? "normal" : "medium";
//        path = db.GetFilePath(uri, s);
//    }
//    if (string.IsNullOrEmpty(path))
//    {
//        return Results.Empty; //new EmptyResult();
//    }
//    //return PhysicalFile(path + ".jpg", "image/jpg");
//    return Results.File(path + ".jpg", "image/jpeg");
//});


app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.MapControllerRoute(name: "default",
         pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
