using FactographData.r;
using Microsoft.AspNetCore.Components;

namespace FTTree.Components
{
    public interface ICellComponent
    {
        [Parameter]
        public bool isEdited { get; set; }

        [Parameter]
        public Pro property { get; set; }
    }
}
