namespace Family.Models
{
    public class AppStateModel
    {
        public string? User { get; set; }
        public string Regime { get; set; } = "view"; // view, gene, edit
    }
}
