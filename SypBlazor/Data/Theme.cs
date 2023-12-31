namespace SypBlazor.Data
{
    public class Theme
    {
        public string label;
        public string[] predicates;
        public Theme(string label, IEnumerable<string> predicateFlow)
        {
            this.label = label;
            this.predicates = predicateFlow.ToArray();
        }
        public static Theme[] themes = new Theme[]
        {
        new Theme("Общие", new string[] {
            "http://fogid.net/o/from-date", "http://fogid.net/o/to-date", "http://fogid.net/o/name", "http://fogid.net/o/description",
            "http://fogid.net/o/person", "http://fogid.net/o/org-sys",
            "http://fogid.net/o/org-classification", "http://fogid.net/o/org-classificator", "http://fogid.net/o/org-category",
            "http://fogid.net/o/participation",
            "http://fogid.net/o/participant", "http://fogid.net/o/in-org", "http://fogid.net/o/role", "http://fogid.net/o/role-classification",
            "http://fogid.net/o/document", "http://fogid.net/o/reflection", "http://fogid.net/o/documentPart",
            "http://fogid.net/o/doc-content", "http://fogid.net/o/reflected", "http://fogid.net/o/in-doc", "http://fogid.net/o/ground", "http://fogid.net/o/inDocument", "http://fogid.net/o/partItem", "http://fogid.net/o/pageNumbers",
            "http://fogid.net/o/titled", "http://fogid.net/o/has-title", "http://fogid.net/o/degree",
            "http://fogid.net/o/collection", "http://fogid.net/o/collection-member", "http://fogid.net/o/in-collection", "http://fogid.net/o/collection-item"
        }),
        new Theme("Детали", new string[]
        {
          "http://fogid.net/o/org-relatives", "http://fogid.net/o/org-parent", "http://fogid.net/o/org-child",
          "http://fogid.net/o/comment", "http://fogid.net/o/sex", "http://fogid.net/o/naming", "http://fogid.net/o/referred-sys", "http://fogid.net/o/alias",
          "http://fogid.net/o/dating",
          "http://fogid.net/o/referred", "http://fogid.net/o/dating-specificator", "http://fogid.net/o/dating-variants", "http://fogid.net/o/some-date", "http://fogid.net/o/dating-accuracy"
        }),
        new Theme("Семья", new string[]
        {
          "http://fogid.net/o/father", "http://fogid.net/o/mother", "http://fogid.net/o/family", "http://fogid.net/o/husband", "http://fogid.net/o/wife"
        })
        };
        public static HashSet<string> Allowed(int[] th_noms)
        {
            return new HashSet<string>(
                th_noms.SelectMany(nom => themes[nom].predicates)
                );
        }
    }
}
