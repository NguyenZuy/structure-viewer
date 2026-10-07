using System;
using StructureViewer.Application.Events;
using StructureViewer.Application.Selection;
using StructureViewer.Domain.Interaction;
using StructureViewer.Presentation.Contracts;
using UnityEngine;

namespace StructureViewer.Presentation.Viewport
{
    // Turns taps and mouse hover over the 3D view into selection and hover state.
    public sealed class ViewportPresenter : IDisposable
    {
        private readonly IPointerEvents _pointer;
        private readonly IStructureRenderer _renderer;
        private readonly ICameraControl _camera;
        private readonly SelectionService _selection;
        private readonly EventBus _bus;
        private readonly IDisposable _modeChanged;

        private InteractionMode _mode = InteractionMode.Select;
        private int _hover = HoverChanged.None;

        public ViewportPresenter(IPointerEvents pointer, IStructureRenderer renderer, ICameraControl camera,
            SelectionService selection, EventBus bus)
        {
            _pointer = pointer ?? throw new ArgumentNullException(nameof(pointer));
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _selection = selection ?? throw new ArgumentNullException(nameof(selection));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));

            _pointer.Tapped += OnTapped;
            _pointer.Hovered += OnHovered;
            _modeChanged = _bus.Subscribe<InteractionModeChanged>(OnModeChanged);
        }

        // Touch has no Ctrl: the toolbar toggle makes every tap additive.
        public bool MultiSelect { get; private set; }

        public void SetMultiSelect(bool enabled) => MultiSelect = enabled;

        public void ClearSelection() => _selection.Clear();

        // False when there is nothing to focus on.
        public bool FocusSelection()
        {
            var indices = _selection.Current.Indices;
            if (indices.Count == 0)
                return false;

            var bounds = _renderer.GetWorldBounds(indices[0]);
            for (int i = 1; i < indices.Count; i++)
                bounds.Encapsulate(_renderer.GetWorldBounds(indices[i]));
            _camera.Focus(bounds);
            return true;
        }

        public void Dispose()
        {
            _pointer.Tapped -= OnTapped;
            _pointer.Hovered -= OnHovered;
            _modeChanged.Dispose();
        }

        // The first tap of a double tap has already selected the element; the second widens it to the assembly.
        private void OnTapped(TapEvent tap)
        {
            if (_mode != InteractionMode.Select)
                return;
            if (tap.Device == PointerDevice.Touch)
                SetHover(HoverChanged.None);

            bool additive = tap.Additive || MultiSelect;
            if (!_renderer.TryPick(tap.ScreenPosition, out var hit))
            {
                if (!additive)
                    _selection.Clear();
                return;
            }

            if (tap.IsDouble)
                _selection.SelectAssembly(hit.Index);
            else if (additive)
                _selection.Toggle(hit.Index);
            else
                _selection.Select(hit.Index);
        }

        // The input adapter raises Hovered at most once per frame and only on movement, so one pick per event is the budget.
        private void OnHovered(Vector2 screenPosition)
        {
            if (_mode != InteractionMode.Select || _pointer.Current != PointerDevice.Mouse)
                return;

            SetHover(_renderer.TryPick(screenPosition, out var hit) ? hit.Index : HoverChanged.None);
        }

        private void OnModeChanged(InteractionModeChanged evt)
        {
            _mode = evt.Mode;
            if (_mode != InteractionMode.Select)
                SetHover(HoverChanged.None);
        }

        private void SetHover(int index)
        {
            if (index == _hover)
                return;

            _hover = index;
            _bus.Publish(new HoverChanged(index));
        }
    }
}
