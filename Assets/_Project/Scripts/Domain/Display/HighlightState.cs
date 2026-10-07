using StructureViewer.Domain.Selection;

namespace StructureViewer.Domain.Display
{
    public enum HighlightState
    {
        None,
        Hover,
        Member,
        Assembly
    }

    public static class HighlightResolver
    {
        // Selection beats hover beats the mode colour. A multi-selection highlights like members (orange).
        public static HighlightState Resolve(int index, SelectionSnapshot selection, int hoverIndex)
        {
            if (selection != null && selection.Contains(index))
                return selection.Kind == SelectionKind.Assembly ? HighlightState.Assembly : HighlightState.Member;
            return index == hoverIndex ? HighlightState.Hover : HighlightState.None;
        }
    }
}
