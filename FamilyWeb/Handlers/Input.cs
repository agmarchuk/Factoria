using Family.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using System.Reflection;

namespace FamilyWeb.Handlers
{
    class Model
    {
        public string? Username;
        public string? Password;
    }
    public class Input
    {
        public static async Task<IResult> Login(HttpRequest request,
            AuthenticationStateProvider authStateProvider,
            UserAccountService userAccountService)
        {
            var formvalues = await request.ReadFormAsync();
            var login = formvalues["login"].FirstOrDefault();
            var password = formvalues["password"].FirstOrDefault();

            Model model = new Model();
            var userAccount = userAccountService.GetByUserName(model.Username);
            if (userAccount == null || userAccount.Password != model.Password)
            {
                return Results.Unauthorized();
            }
            else
            {
                var customAuthStateProvider = (CustomAuthenticationStateProvider)authStateProvider;
                await customAuthStateProvider.UpdateAuthenticationState(new AuthState
                {
                    UserName = model.Username,
                    Role = userAccount.Role
                });
                return Results.Redirect("/");
            }
        }
    }
}
