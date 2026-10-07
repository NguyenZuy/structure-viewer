using System;
using System.Collections.Generic;
using StructureViewer.Presentation.Contracts;
using UnityEngine.UIElements;

namespace StructureViewer.Presentation.Shell
{
    // Toolbar buttons plus an overflow menu holding whatever doesn't fit, lowest priority first.
    internal sealed class ToolbarView
    {
        public const float ItemWidth = 72f;
        public const float CompactItemWidth = 56f;
        public const float OverflowWidth = 44f;
        private const long TooltipDelayMs = 500;

        private sealed class Entry
        {
            public ToolbarItem Item;
            public Button Bar;
            public Button MenuRow;
        }

        private readonly VisualElement _toolbar;
        private readonly VisualElement _title;
        private readonly VisualElement _items;
        private readonly Button _overflow;
        private readonly VisualElement _scrim;
        private readonly VisualElement _menu;
        private readonly VisualElement _overlay;
        private readonly Label _tooltip;
        private readonly List<Entry> _entries = new List<Entry>();
        private readonly List<int> _priorities = new List<int>();
        private bool[] _visible = Array.Empty<bool>();
        private bool _compact;
        private IVisualElementScheduledItem _tooltipTimer;
        private Entry _tooltipEntry;

        public ToolbarView(VisualElement root)
        {
            _toolbar = root.Q("toolbar");
            _title = root.Q("app-title");
            _items = root.Q("toolbar-items");
            _overflow = root.Q<Button>("toolbar-overflow");
            _scrim = root.Q("overflow-scrim");
            _menu = root.Q("overflow-menu");
            _overlay = root.Q("overlay");
            _tooltip = root.Q<Label>("tooltip");

            _overflow.clicked += ShowMenu;
            // Tapping anywhere outside the menu closes it; the scrim also keeps that tap away from the 3D view.
            _scrim.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.target == _scrim)
                    HideMenu();
            });
            _toolbar.RegisterCallback<GeometryChangedEvent>(_ => Relayout());
            Relayout();
        }

        public void Add(ToolbarItem item)
        {
            foreach (var existing in _entries)
                if (existing.Item.Id == item.Id)
                    throw new ArgumentException($"Toolbar item '{item.Id}' already exists.", nameof(item));

            var entry = new Entry { Item = item };
            entry.Bar = CreateButton(item, "sv-toolbar__item", () => item.OnClick());
            entry.MenuRow = CreateButton(item, "sv-menu__item", () =>
            {
                HideMenu();
                item.OnClick();
            });
            entry.Bar.RegisterCallback<PointerEnterEvent>(evt => OnPointerEnter(entry, evt));
            entry.Bar.RegisterCallback<PointerLeaveEvent>(_ => HideTooltip());
            entry.Bar.RegisterCallback<PointerDownEvent>(_ => HideTooltip(), TrickleDown.TrickleDown);

            _entries.Add(entry);
            _priorities.Add(item.Priority);
            _visible = new bool[_entries.Count];
            _items.Add(entry.Bar);
            _menu.Add(entry.MenuRow);
            Relayout();
        }

        public void SetState(string id, bool active, bool enabled)
        {
            foreach (var entry in _entries)
            {
                if (entry.Item.Id != id)
                    continue;
                foreach (var button in new[] { entry.Bar, entry.MenuRow })
                {
                    button.EnableInClassList("sv-toolbar__item--active", active);
                    button.SetEnabled(enabled);
                }
                return;
            }
            throw new ArgumentException($"Unknown toolbar item '{id}'.", nameof(id));
        }

        public void SetCompact(bool compact)
        {
            _compact = compact;
            Relayout();
        }

        private void Relayout()
        {
            float itemWidth = _compact ? CompactItemWidth : ItemWidth;
            float available = _toolbar.contentRect.width;
            if (!_compact)
                available -= _title.layout.width + _title.resolvedStyle.marginRight;
            // Before the first layout pass widths are NaN; show everything until real sizes arrive.
            if (float.IsNaN(available))
                available = float.MaxValue;

            bool overflow = ToolbarOverflow.Split(_priorities, available, itemWidth, OverflowWidth, _visible);
            for (int i = 0; i < _entries.Count; i++)
            {
                _entries[i].Bar.style.width = itemWidth;
                _entries[i].Bar.EnableInClassList("sv-hidden", !_visible[i]);
                _entries[i].MenuRow.EnableInClassList("sv-hidden", _visible[i]);
            }
            _overflow.EnableInClassList("sv-hidden", !overflow);
            if (!overflow)
                HideMenu();
        }

        private static Button CreateButton(ToolbarItem item, string className, Action onClick)
        {
            var button = new Button(onClick) { name = $"toolbar-{item.Id}" };
            button.AddToClassList("sv-button");
            button.AddToClassList(className);
            var glyph = new Label(item.Glyph) { pickingMode = PickingMode.Ignore };
            glyph.AddToClassList("sv-toolbar__glyph");
            var label = new Label(item.Label) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("sv-toolbar__label");
            button.Add(glyph);
            button.Add(label);
            return button;
        }

        private void ShowMenu()
        {
            HideTooltip();
            _scrim.RemoveFromClassList("sv-hidden");
        }

        private void HideMenu() => _scrim.AddToClassList("sv-hidden");

        // Hover is a desktop bonus: touch never shows tooltips, and the label already says what the button does.
        private void OnPointerEnter(Entry entry, PointerEnterEvent evt)
        {
            if (evt.pointerType != PointerType.mouse || string.IsNullOrEmpty(entry.Item.Tooltip))
                return;
            _tooltipEntry = entry;
            _tooltipTimer?.Pause();
            _tooltipTimer = _tooltip.schedule.Execute(ShowTooltip).StartingIn(TooltipDelayMs);
        }

        private void ShowTooltip()
        {
            if (_tooltipEntry == null)
                return;
            var button = _tooltipEntry.Bar.worldBound;
            var overlay = _overlay.worldBound;
            _tooltip.text = _tooltipEntry.Item.Tooltip;
            // Anchored to the button's right edge: toolbar items sit on the right, so this never runs off screen.
            _tooltip.style.right = overlay.xMax - button.xMax;
            _tooltip.style.top = button.yMax - overlay.yMin + 4f;
            _tooltip.RemoveFromClassList("sv-hidden");
        }

        private void HideTooltip()
        {
            _tooltipEntry = null;
            _tooltipTimer?.Pause();
            _tooltip.AddToClassList("sv-hidden");
        }
    }
}
