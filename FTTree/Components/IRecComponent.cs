using FactographData;
using FactographData.r;
using Microsoft.AspNetCore.Components;

namespace FTTree.Components
{
    public interface IRecComponent
    {
        [Parameter]
        public Rec[] recs { get; set; }

        [Parameter]
        public string forbidden { get; set; }
    }
}
