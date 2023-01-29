using FactographData;

namespace Family.Models
{
    public class GeneTreeModel
    {
        public int level;
        public RRecord node;
        public List<RRecord> spouse;
        public GeneTreeModel parent;
        public GeneTreeModel[] childs;

        private static IEnumerable<GeneTreeModel> Tr(GeneTreeModel model, int level)
        {
            var firstelem = new GeneTreeModel { node = model.node, spouse = model.spouse, level = level };
            var query2 = (new GeneTreeModel[] { firstelem })
                .Concat(model.childs.SelectMany(ch => Tr(ch, level + 1)));
            var query3 = query2.ToArray();
            return query3;
        }
        public static GeneTreeModel[] Traverse(GeneTreeModel model)
        {
            return Tr(model, 0).ToArray();
        }
    }
}
