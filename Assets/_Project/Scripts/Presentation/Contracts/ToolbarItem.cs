using System;

namespace StructureViewer.Presentation.Contracts
{
    public sealed class ToolbarItem
    {
        public ToolbarItem(string id, string label, string glyph, string tooltip, Action onClick, bool isToggle = false, int priority = 0)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Label = label;
            Glyph = glyph;
            Tooltip = tooltip;
            OnClick = onClick ?? throw new ArgumentNullException(nameof(onClick));
            IsToggle = isToggle;
            Priority = priority;
        }

        public string Id { get; }
        public string Label { get; }

        // Text/Unicode glyph shown on the button.
        public string Glyph { get; }

        // Desktop hover bonus only; the label must carry the meaning.
        public string Tooltip { get; }
        public Action OnClick { get; }
        public bool IsToggle { get; }

        // Higher stays visible longer in compact mode; the rest go to the overflow menu.
        public int Priority { get; }
    }
}
