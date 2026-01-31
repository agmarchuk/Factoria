using Factograph.Data;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.MapStaticAssets();

Factograph.Data.IFDataService db = new Factograph.Data.FDataService();

// Есть несколько входов: начальный, поисковый, портретный, документный. Каждый вход запускает свою функцию. 
// А есть еще специально вычисляемые страницы, входы: /spec_name. Html-результат страницы формируется как
// единый html-шаблон в который вставляеются вычисленные значения. Буду накапливать функции в классе Handlers.

app.MapGet("~/", () => Results.Redirect("~/home")); 
app.MapGet("~/home/{id?}/{idd?}", (string? id, string? idd) => Results.Content(
    OpenAr.Handlers.Page("", "", "", "Get body"), "text/html", System.Text.Encoding.UTF8));
app.MapPost("~/post/", async context =>
{
    string? ss = context.Request.Form["ss"];
    string? bw = context.Request.Form["bw"];
    string? sv = context.Request.Form["sv"];
    RRecord[] searchresults = new RRecord[0];
    if (!string.IsNullOrEmpty(ss))
    {        
        var query = db.SearchRRecords(ss, bw == "on");
        // Получаем результат
        var sr = query
            .Select(r => new Tuple<RRecord, string>(r, r.GetName())) // получаем поток пар запись-имя
            .OrderBy(pa => pa.Item2) // сортируем по имени
                                     //.Select(pa => new Tuple<RRecord, string> (pa.Item1, pa.Item1.GetField("http://fogid.net/o/from-date") ?? "zz")) // Оставляем в потоке запись и дату
            .ThenBy(pa => pa.Item1.GetField("http://fogid.net/o/from-date") ?? "zz") // Вторичная сортировка 
            .Select(pa => pa.Item1)
            .Take(100)
            .ToArray();
        searchresults = sr;
    }
    await context.Response.WriteAsync(OpenAr.Handlers.Page(ss, bw, sv, searchresults.Length.ToString()));
});

//app.MapGet("/old-path", () => Results.Redirect("/new-path"));
//app.MapGet("/download", () => Results.File("myfile.text"));
//app.MapGet("/html", () => Results.Extensions.Html(@$"<!doctype html> ... 
//app.MapGet("/users/{userId}/books/{bookId}", (int userId, int bookId) => $"The user id is {userId} and book id is {bookId}");
//app.MapPost("/", () => "This is a POST");
//app.MapGet("/{id}", async context => { await context.Response.WriteAsJsonAsync(new { Message = "One todo item" }); });


app.Run();

