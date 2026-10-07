using System;
using StructureViewer.Application.Commands;
using StructureViewer.Application.Display;
using StructureViewer.Application.Events;
using StructureViewer.Application.Loading;
using StructureViewer.Application.Visibility;
using StructureViewer.Domain.Display;
using StructureViewer.Domain.Measure;
using StructureViewer.Presentation.Contracts;
using StructureViewer.Presentation.Input;
using StructureViewer.Presentation.Labels;
using StructureViewer.Presentation.Measure;
using StructureViewer.Presentation.Viewport;

namespace StructureViewer.Bootstrap
{
    // Every user action reachable from the toolbar, with the keyboard shortcuts routed to the same methods,
    // plus the toolbar's active/enabled states.
    public sealed class AppActions : IDisposable
    {
        public const string Display = "display";
        public const string Layers = "layers";
        public const string Takeoff = "takeoff";
        public const string LabelsToggle = "labels";
        public const string MultiSelect = "multi-select";

        public const string NothingToFocus = "Select something to focus on.";
        public const string NothingSelected = "Select something first.";
        public const string MeasureHint = "Measure: tap two points.";

        private readonly IShell _shell;
        private readonly ICameraControl _camera;
        private readonly IStructureRenderer _renderer;
        private readonly StructureSession _session;
        private readonly ViewportPresenter _viewport;
        private readonly VisibilityActions _visibility;
        private readonly VisibilityService _visibilityState;
        private readonly CommandHistory _history;
        private readonly DisplaySettings _display;
        private readonly MeasurePresenter _measure;
        private readonly LabelsPresenter _labels;
        private readonly PanelLayout _panels;
        private readonly IDisposable[] _subscriptions;

        private bool _registered;
        private bool _hasSelection;
        private bool _labelsChosenByUser;

        public AppActions(IShell shell, ICameraControl camera, IStructureRenderer renderer, StructureSession session,
            ViewportPresenter viewport, VisibilityActions visibility, VisibilityService visibilityState, CommandHistory history,
            DisplaySettings display, MeasurePresenter measure, LabelsPresenter labels, PanelLayout panels, EventBus bus)
        {
            _shell = shell ?? throw new ArgumentNullException(nameof(shell));
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _viewport = viewport ?? throw new ArgumentNullException(nameof(viewport));
            _visibility = visibility ?? throw new ArgumentNullException(nameof(visibility));
            _visibilityState = visibilityState ?? throw new ArgumentNullException(nameof(visibilityState));
            _history = history ?? throw new ArgumentNullException(nameof(history));
            _display = display ?? throw new ArgumentNullException(nameof(display));
            _measure = measure ?? throw new ArgumentNullException(nameof(measure));
            _labels = labels ?? throw new ArgumentNullException(nameof(labels));
            _panels = panels ?? throw new ArgumentNullException(nameof(panels));
            if (bus == null)
                throw new ArgumentNullException(nameof(bus));

            _subscriptions = new[]
            {
                bus.Subscribe<SelectionChanged>(evt =>
                {
                    _hasSelection = evt.Selection != null && !evt.Selection.IsEmpty;
                    RefreshToolbar();
                }),
                bus.Subscribe<VisibilityChanged>(_ => RefreshToolbar())
            };
            _history.Changed += RefreshToolbar;
            _panels.Changed += RefreshToolbar;
            _shell.CompactChanged += OnCompactChanged;
        }

        // Toolbar order is the desktop order; priority decides what stays visible on a narrow phone toolbar.
        public void RegisterToolbar()
        {
            Add(ShortcutMap.FitAll, "Fit all", ToolbarIcon.FitAll, "Fit the whole model in view (Home)", FitAll, false, 100);
            Add(ShortcutMap.Focus, "Focus", ToolbarIcon.Focus, "Frame the selection (F)", Focus, false, 70);
            Add(ShortcutMap.Undo, "Undo", ToolbarIcon.Undo, "Undo visibility change (Ctrl+Z)", Undo, false, 80);
            Add(ShortcutMap.Redo, "Redo", ToolbarIcon.Redo, "Redo (Ctrl+Y)", Redo, false, 75);
            Add(Display, "Display", ToolbarIcon.Display, "Display mode: Realistic, Color by, X-ray, Clay (1–4)", ToggleDisplay, true, 90);
            Add(Layers, "Layers", ToolbarIcon.Layers, "Categories, levels and legend", ToggleLayers, true, 95);
            Add(ShortcutMap.Isolate, "Isolate", ToolbarIcon.Isolate, "Show only the selection (I)", Isolate, true, 60);
            Add(ShortcutMap.Hide, "Hide", ToolbarIcon.Hide, "Hide the selection (H)", Hide, false, 55);
            Add(ShortcutMap.ShowAll, "Show all", ToolbarIcon.ShowAll, "Reset layers, isolate and hidden (Shift+H)", ShowAll, false, 50);
            Add(ShortcutMap.Measure, "Measure", ToolbarIcon.Measure, "Measure between two points (M)", ToggleMeasure, true, 85);
            Add(Takeoff, "Takeoff", ToolbarIcon.Takeoff, "Material takeoff table", ToggleTakeoff, true, 45);
            Add(LabelsToggle, "Labels", ToolbarIcon.Labels, "Assembly labels", ToggleLabels, true, 40);
            Add(MultiSelect, "Multi", ToolbarIcon.Multi, "Add or remove taps from the selection (Ctrl+click on desktop)", ToggleMultiSelect, true, 65);

            _labels.SetEnabled(!_shell.IsCompact);
            _registered = true;
            RefreshToolbar();
        }

        // Shortcut ids from ShortcutMap.
        public void Execute(string action)
        {
            switch (action)
            {
                case ShortcutMap.FitAll: FitAll(); break;
                case ShortcutMap.Focus: Focus(); break;
                case ShortcutMap.Escape: Escape(); break;
                case ShortcutMap.Isolate: Isolate(); break;
                case ShortcutMap.Hide: Hide(); break;
                case ShortcutMap.ShowAll: ShowAll(); break;
                case ShortcutMap.Undo: Undo(); break;
                case ShortcutMap.Redo: Redo(); break;
                case ShortcutMap.Measure: ToggleMeasure(); break;
                case ShortcutMap.Mode1: _display.SetMode(DisplayMode.Realistic); break;
                case ShortcutMap.Mode2: _display.SetMode(DisplayMode.ColorBy); break;
                case ShortcutMap.Mode3: _display.SetMode(DisplayMode.XRay); break;
                case ShortcutMap.Mode4: _display.SetMode(DisplayMode.Clay); break;
            }
        }

        public void FitAll()
        {
            if (_session.HasModel)
                _camera.FitAll(_renderer.ModelBounds);
        }

        public void Focus()
        {
            if (!_viewport.FocusSelection())
                _shell.ShowToast(NothingToFocus);
        }

        // Esc steps back one level: measurement → measure mode → selection.
        public void Escape()
        {
            if (_measure.IsActive)
            {
                if (_measure.State == MeasureState.AwaitingA)
                    ToggleMeasure();
                else
                    _measure.Clear();
                return;
            }
            _viewport.ClearSelection();
        }

        public void Isolate()
        {
            if (!_visibility.HasSelection)
                _shell.ShowToast(NothingSelected);
            else
                _visibility.IsolateSelection();
        }

        public void Hide()
        {
            if (!_visibility.HasSelection)
                _shell.ShowToast(NothingSelected);
            else
                _visibility.HideSelection();
        }

        public void ShowAll() => _visibility.ShowAll();
        public void Undo() => _visibility.Undo();
        public void Redo() => _visibility.Redo();

        public void ToggleMeasure()
        {
            _measure.SetActive(!_measure.IsActive);
            if (_measure.IsActive)
                _shell.ShowToast(MeasureHint);
            RefreshToolbar();
        }

        public void ToggleMultiSelect()
        {
            _viewport.SetMultiSelect(!_viewport.MultiSelect);
            RefreshToolbar();
        }

        public void ToggleLabels()
        {
            _labelsChosenByUser = true;
            _labels.SetEnabled(!_labels.IsEnabled);
            RefreshToolbar();
        }

        public void ToggleDisplay() => _panels.ToggleDisplay();
        public void ToggleLayers() => _panels.ToggleLayers();
        public void ToggleTakeoff() => _panels.ToggleTakeoff();

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
                subscription.Dispose();
            _history.Changed -= RefreshToolbar;
            _panels.Changed -= RefreshToolbar;
            _shell.CompactChanged -= OnCompactChanged;
        }

        // Labels default to on for desktop and off for phones, until the user picks.
        private void OnCompactChanged(bool compact)
        {
            if (!_labelsChosenByUser)
                _labels.SetEnabled(!compact);
            RefreshToolbar();
        }

        private void Add(string id, string label, ToolbarIcon icon, string tooltip, Action onClick, bool toggle, int priority) =>
            _shell.AddToolbarItem(new ToolbarItem(id, label, icon, tooltip, onClick, toggle, priority));

        // Item states can only be set once the items exist.
        private void RefreshToolbar()
        {
            if (!_registered)
                return;

            var state = _visibilityState.Current;
            bool filtered = state != null && !state.Equals(state.ShowAll());
            _shell.SetToolbarItemState(ShortcutMap.Focus, false, _hasSelection);
            _shell.SetToolbarItemState(ShortcutMap.Undo, false, _history.CanUndo);
            _shell.SetToolbarItemState(ShortcutMap.Redo, false, _history.CanRedo);
            _shell.SetToolbarItemState(ShortcutMap.Isolate, state != null && state.IsIsolating, true);
            _shell.SetToolbarItemState(ShortcutMap.ShowAll, false, filtered);
            _shell.SetToolbarItemState(ShortcutMap.Measure, _measure.IsActive, true);
            _shell.SetToolbarItemState(MultiSelect, _viewport.MultiSelect, true);
            _shell.SetToolbarItemState(LabelsToggle, _labels.IsEnabled, true);
            _shell.SetToolbarItemState(Display, _panels.IsDisplayOpen, true);
            _shell.SetToolbarItemState(Layers, _panels.IsLayersActive, true);
            _shell.SetToolbarItemState(Takeoff, _panels.IsTakeoffActive, true);
        }
    }
}
