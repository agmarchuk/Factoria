namespace Family.Models
{
    public class SessionState
    {
        //public string? User { get; set; }
        //public string? Role { get; set; }
        public string Regime { get; set; } = "home"; // home, view, gene, edit
        public string Lang { get; set; } = "ru";
        public SessionState() { Regime = "home"; Lang = "ru"; }
    }
}
