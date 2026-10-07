using System;
using System.Collections.Generic;
using StructureViewer.Application.Events;
using StructureViewer.Domain.Labels;
using StructureViewer.Domain.Selection;
using StructureViewer.Domain.Structure;
using StructureViewer.Domain.Visibility;

namespace StructureViewer.Presentation.Labels
{
    public sealed class LabelsPresenter : IDisposable
    {
        // Fade distances as multiples of the model's bounding radius: a "fit all" view (~2.5 r) stays fully opaque.
        public const float NearFadeRadii = 3f;
        public const float FarFadeRadii = 8f;

        private readonly ILabelsView _view;
        private readonly IDisposable[] _subscriptions;
        private readonly Dictionary<string, int> _anchorOf = new Dictionary<string, int>();

        private StructureModel _model;
        private IReadOnlyList<AssemblyAnchor> _anchors = Array.Empty<AssemblyAnchor>();
        private bool[] _visible = Array.Empty<bool>();
        private int _emphasized = -1;

        public LabelsPresenter(ILabelsView view, EventBus bus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            if (bus == null)
                throw new ArgumentNullException(nameof(bus));

            _subscriptions = new[]
            {
                bus.Subscribe<StructureLoaded>(OnLoaded),
                bus.Subscribe<VisibilityChanged>(OnVisibilityChanged),
                bus.Subscribe<SelectionChanged>(OnSelectionChanged)
            };
        }

        public bool IsEnabled { get; private set; } = true;

        // Toolbar toggle: on by default on desktop, off on compact screens (decided by the caller).
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
            _view.SetEnabled(enabled);
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
                subscription.Dispose();
        }

        private void OnLoaded(StructureLoaded evt)
        {
            _model = evt.Model;
            _anchors = AssemblyAnchors.Build(_model);
            _anchorOf.Clear();
            for (int i = 0; i < _anchors.Count; i++)
                _anchorOf.Add(_anchors[i].Group, i);
            _visible = new bool[_anchors.Count];
            for (int i = 0; i < _visible.Length; i++)
                _visible[i] = true;
            _emphasized = -1;

            float radius = _model.Bounds.extents.magnitude;
            _view.ShowAnchors(_anchors, radius * NearFadeRadii, radius * FarFadeRadii);
            _view.SetEmphasized(-1);
        }

        // Group visibility changes rarely, so it is resolved here once instead of per label per frame.
        private void OnVisibilityChanged(VisibilityChanged evt)
        {
            var visibility = evt.Visibility ?? AllVisible.Instance;
            for (int i = 0; i < _anchors.Count; i++)
            {
                bool visible = _anchors[i].AnyVisible(visibility);
                if (visible == _visible[i])
                    continue;
                _visible[i] = visible;
                _view.SetGroupVisible(i, visible);
            }
        }

        private void OnSelectionChanged(SelectionChanged evt)
        {
            int anchor = -1;
            var selection = evt.Selection;
            if (_model != null && selection != null && selection.Kind == SelectionKind.Assembly)
            {
                string group = _model.Elements[selection.Indices[0]].Info.Group;
                if (group == null || !_anchorOf.TryGetValue(group, out anchor))
                    anchor = -1;
            }

            if (anchor == _emphasized)
                return;
            _emphasized = anchor;
            _view.SetEmphasized(anchor);
        }
    }
}
