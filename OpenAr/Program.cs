var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();

// Есть несколько входов: начальный, поисковый, портретный, документный. Каждый вход запускает свою функцию. 
// А есть еще специально вычисляемые страницы, входы: /spec_name. Html-результат страницы формируется как
// единый html-шаблон в который вставляеются вычисленные значения. Буду накапливать функции в классе Handlers.

app.MapGet("/", () => Results.Redirect("/home")); 
app.MapGet("/home/{id?}/{idd?}", (string? id, string? idd) => Results.Content(
    OpenAr.Handlers.Page(id, "", "", ""), "text/html", System.Text.Encoding.UTF8));

//app.MapGet("/old-path", () => Results.Redirect("/new-path"));
//app.MapGet("/download", () => Results.File("myfile.text"));
//app.MapGet("/html", () => Results.Extensions.Html(@$"<!doctype html> ... 
//app.MapGet("/users/{userId}/books/{bookId}", (int userId, int bookId) => $"The user id is {userId} and book id is {bookId}");
//app.MapPost("/", () => "This is a POST");
//app.MapGet("/{id}", async context => { await context.Response.WriteAsJsonAsync(new { Message = "One todo item" }); });


app.Run();

