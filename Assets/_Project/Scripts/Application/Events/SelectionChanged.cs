using StructureViewer.Domain.Selection;

namespace StructureViewer.Application.Events
{
    public readonly struct SelectionChanged
    {
        public SelectionChanged(SelectionSnapshot selection) => Selection = selection;

        public SelectionSnapshot Selection { get; }
    }
}
