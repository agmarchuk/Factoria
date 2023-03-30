namespace Family.Authentication
{
    public class AuthState
    {
        public string? UserName { get; set;}
        public string? Role { get; set;}
        public string Regime { get; set; } = "view"; // view, gene, edit
        public string Lang { get; set; } = "ru";
    }
}
