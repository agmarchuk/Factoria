using FactographData;
using Microsoft.AspNetCore.Components;

namespace FTTree.Components
{
    public interface ITreeComponent
    {
        [Parameter]
        public TTree[] ttrees { get; set; }
    }
}
