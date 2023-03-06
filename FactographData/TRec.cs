

namespace FactographData.TRec
{
    public class Rec
    {
        public string Id { get; private set; }
        public string Tp { get; private set; }
        public Pro[] Props { get; private set; }
        public Rec(string id, string tp, params Pro[] props)
        {
            this.Id = id;
            this.Tp = tp;
            this.Props = props;
        }
    }
    public abstract class Pro
    {
        public string Pred { get; private set;} = "";
    }
}
