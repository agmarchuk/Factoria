namespace SypBlazor
{
    public class DataService : Factograph.Data.FDataService
    {
        public DataService() : base("wwwroot/", "wwwroot/Ontology_iis-v14.xml", null)
        {
        }
    }
}
