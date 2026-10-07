using System;
using StructureViewer.Presentation.Contracts;
using UnityEngine.UIElements;

namespace StructureViewer.Bootstrap
{
    // Where each panel lives. Desktop: layers + legend left, info right, takeoff bottom (toggled).
    // Compact: no slots; every panel opens as the single bottom sheet, the info sheet following the selection.
    public sealed class PanelLayout : IDisposable
    {
        public const string LayersTitle = "Layers";
        public const string InfoTitle = "Details";
        public const string TakeoffTitle = "Material takeoff";
        public const string DisplayTitle = "Display";

        private readonly IShell _shell;
        private readonly VisualElement _info;
        private readonly VisualElement _layers;
        private readonly VisualElement _takeoff;
        private readonly VisualElement _display;

        // What this class last put in the sheet; null when the sheet is closed.
        private VisualElement _sheet;
        private bool _hasSelection;

        public PanelLayout(IShell shell, VisualElement info, VisualElement layers, VisualElement legend, VisualElement takeoff, VisualElement display)
        {
            _shell = shell ?? throw new ArgumentNullException(nameof(shell));
            _info = info ?? throw new ArgumentNullException(nameof(info));
            _takeoff = takeoff ?? throw new ArgumentNullException(nameof(takeoff));
            _display = display ?? throw new ArgumentNullException(nameof(display));

            // Legend sits under the layers (it hides itself outside "Color by").
            _layers = new VisualElement { name = "layers-stack" };
            _layers.style.flexGrow = 1;
            _layers.Add(layers ?? throw new ArgumentNullException(nameof(layers)));
            _layers.Add(legend ?? throw new ArgumentNullException(nameof(legend)));

            _shell.CompactChanged += OnCompactChanged;
            _shell.SheetHidden += OnSheetHidden;
            Apply();
        }

        public bool LayersOpen { get; private set; } = true;
        public bool TakeoffOpen { get; private set; }
        public bool IsDisplayOpen => _sheet == _display;

        // Raised after anything that changes which panels are open (for toolbar states).
        public event Action Changed;

        public void ToggleLayers()
        {
            if (_shell.IsCompact)
                ToggleSheet(_layers, LayersTitle);
            else
                LayersOpen = !LayersOpen;
            Apply();
        }

        public void ToggleTakeoff()
        {
            if (_shell.IsCompact)
                ToggleSheet(_takeoff, TakeoffTitle);
            else
                TakeoffOpen = !TakeoffOpen;
            Apply();
        }

        public void ToggleDisplay()
        {
            ToggleSheet(_display, DisplayTitle);
            Changed?.Invoke();
        }

        // Compact: a selection opens the details sheet and clearing it closes that sheet (other sheets are left alone).
        public void OnSelectionChanged(bool hasSelection)
        {
            _hasSelection = hasSelection;
            if (!_shell.IsCompact)
                return;
            if (hasSelection && _sheet != _info)
                ShowSheet(_info, InfoTitle);
            else if (!hasSelection && _sheet == _info)
                _shell.HideSheet();
            Changed?.Invoke();
        }

        public bool IsLayersActive => _shell.IsCompact ? _sheet == _layers : LayersOpen;
        public bool IsTakeoffActive => _shell.IsCompact ? _sheet == _takeoff : TakeoffOpen;

        public void Dispose()
        {
            _shell.CompactChanged -= OnCompactChanged;
            _shell.SheetHidden -= OnSheetHidden;
        }

        private void OnCompactChanged(bool compact)
        {
            // A slot panel left in the sheet would vanish on the next layout pass; start from a clean state.
            if (_sheet != null && _sheet != _display)
                _shell.HideSheet();
            Apply();
            if (compact && _hasSelection)
                OnSelectionChanged(true);
        }

        private void OnSheetHidden()
        {
            _sheet = null;
            Changed?.Invoke();
        }

        private void ToggleSheet(VisualElement content, string title)
        {
            if (_sheet == content)
                _shell.HideSheet();
            else
                ShowSheet(content, title);
        }

        private void ShowSheet(VisualElement content, string title)
        {
            _shell.ShowSheet(content, title);
            _sheet = content;
        }

        private void Apply()
        {
            if (_shell.IsCompact)
            {
                Detach(_layers, _shell.LeftSlot);
                Detach(_info, _shell.RightSlot);
                Detach(_takeoff, _shell.BottomSlot);
            }
            else
            {
                Mount(_layers, _shell.LeftSlot, LayersOpen);
                Mount(_info, _shell.RightSlot, true);
                Mount(_takeoff, _shell.BottomSlot, TakeoffOpen);
            }
            Changed?.Invoke();
        }

        private static void Mount(VisualElement panel, VisualElement slot, bool open)
        {
            if (open && panel.parent != slot)
                slot.Add(panel);
            else if (!open)
                Detach(panel, slot);
        }

        private static void Detach(VisualElement panel, VisualElement slot)
        {
            if (panel.parent == slot)
                panel.RemoveFromHierarchy();
        }
    }
}
