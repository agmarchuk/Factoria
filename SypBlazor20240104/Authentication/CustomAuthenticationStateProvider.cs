using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Claims;

namespace Family.Authentication
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private ProtectedSessionStorage _storage;
        private ProtectedLocalStorage _localstorage;
        private ClaimsPrincipal _anonimous = new ClaimsPrincipal(new ClaimsIdentity());

        public CustomAuthenticationStateProvider(
            ProtectedSessionStorage sessionStorage,
            ProtectedLocalStorage localStorage)
        {
            _storage = sessionStorage;
            _localstorage = localStorage;
        }
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var userSessionStorageResult = await _storage.GetAsync<AuthState>("AuthState");
                var userSession = userSessionStorageResult.Success ? userSessionStorageResult.Value : null;
                if (userSession == null)
                {
                    return await Task.FromResult(new AuthenticationState(_anonimous));
                }

                var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                {
                    new Claim(ClaimTypes.Name, userSession.UserName??""),
                    new Claim(ClaimTypes.Role, userSession.Role??"")
                }, "CustomAuth"));
                return await Task.FromResult(new AuthenticationState(claimsPrincipal));
            }
            catch
            {
                return await Task.FromResult(new AuthenticationState(_anonimous));
            }
        }
        public async Task UpdateAuthenticationState(AuthState? userSession)
        {
            ClaimsPrincipal claimsPrincipal;
            if (userSession != null)
            {
                await _storage.SetAsync("AuthState", userSession);
                claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                {
                    new Claim(ClaimTypes.Name, userSession.UserName??""),
                    new Claim(ClaimTypes.Role, userSession.Role??"")
                }));
            }
            else
            {
                await _storage.DeleteAsync("AuthState");
                claimsPrincipal = _anonimous;
            }
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
        }
        // =========== Секция локальной памяти ============
        public async Task SaveInStorage(string key, string value)
        {
            await _localstorage.SetAsync(key, value);
        }
        public async Task<string?> ReadFromStorage(string key)
        {
            var result = await _localstorage.GetAsync<string>(key);
            var v = result.Success ? result.Value : "";
            return v;
        }
        public async Task DeleteFromStorage(string key)
        {
            await _localstorage.DeleteAsync(key);
        }

    }
}
