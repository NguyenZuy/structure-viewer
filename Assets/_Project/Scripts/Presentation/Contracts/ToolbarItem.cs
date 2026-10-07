using System;

namespace StructureViewer.Presentation.Contracts
{
    public sealed class ToolbarItem
    {
        public ToolbarItem(string id, string label, ToolbarIcon icon, string tooltip, Action onClick, bool isToggle = false, int priority = 0)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Label = label;
            Icon = icon;
            Tooltip = tooltip;
            OnClick = onClick ?? throw new ArgumentNullException(nameof(onClick));
            IsToggle = isToggle;
            Priority = priority;
        }

        public string Id { get; }
        public string Label { get; }

        public ToolbarIcon Icon { get; }

        // Desktop hover bonus only; the label must carry the meaning.
        public string Tooltip { get; }
        public Action OnClick { get; }
        public bool IsToggle { get; }

        // Higher stays visible longer in compact mode; the rest go to the overflow menu.
        public int Priority { get; }
    }
}
