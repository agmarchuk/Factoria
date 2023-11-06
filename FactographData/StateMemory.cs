using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Family.Authentication
{
    public static class StateMemory
    {
        public static async Task SaveInStorage(ProtectedBrowserStorage _storage, string key, string value)
        {
            await _storage.SetAsync(key, value);
        }
        public static async Task<string?> ReadFromStorage(ProtectedBrowserStorage _storage, string key)
        {
            var result = await _storage.GetAsync<string>(key);
            var v = result.Success ? result.Value : "";
            return v;
        }
        public static async Task DeleteFromStorage(ProtectedBrowserStorage _storage, string key)
        {
            await _storage.DeleteAsync(key);
        }
    }
}
