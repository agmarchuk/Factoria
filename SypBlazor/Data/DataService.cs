namespace SypBlazor.Data
{
    public class DataService : Factograph.Data.FDataService
    {
        public DataService() : base("wwwroot/", "wwwroot/Ontology.xml", null)
        {
        }
    }
}
