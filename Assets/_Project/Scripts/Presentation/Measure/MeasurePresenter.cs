using System;
using System.Collections.Generic;
using System.Globalization;
using StructureViewer.Application.Events;
using StructureViewer.Application.Loading;
using StructureViewer.Domain.Interaction;
using StructureViewer.Domain.Measure;
using StructureViewer.Presentation.Contracts;
using UnityEngine;

namespace StructureViewer.Presentation.Measure
{
    public sealed class MeasurePresenter : IDisposable
    {
        // At 96 dpi. A fingertip covers far more screen than a cursor, so touch gets the larger radius.
        public const float MouseSnapRadius = 12f;
        public const float TouchSnapRadius = 28f;

        private static readonly CultureInfo Format = CultureInfo.InvariantCulture;

        private readonly IPointerEvents _pointer;
        private readonly IStructureRenderer _renderer;
        private readonly IMeasureView _view;
        private readonly StructureSession _session;
        private readonly EventBus _bus;
        private readonly IDisposable _loaded;
        private readonly MeasureSession _measure = new MeasureSession();
        private readonly List<SnapCandidate> _candidates = new List<SnapCandidate>(3);

        public MeasurePresenter(IPointerEvents pointer, IStructureRenderer renderer, IMeasureView view, StructureSession session, EventBus bus)
        {
            _pointer = pointer ?? throw new ArgumentNullException(nameof(pointer));
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));

            _pointer.Tapped += OnTapped;
            // Points from the previous model mean nothing in a new one.
            _loaded = _bus.Subscribe<StructureLoaded>(_ => Clear());
            _view.Render(MeasureVisual.None);
        }

        public bool IsActive { get; private set; }
        public MeasureState State => _measure.State;

        public void SetActive(bool active)
        {
            if (active == IsActive)
                return;

            IsActive = active;
            if (active)
                _measure.Start();
            else
                _measure.Cancel();
            _bus.Publish(new InteractionModeChanged(active ? InteractionMode.Measure : InteractionMode.Select));
            Render();
        }

        // Drops the current measurement but stays in measure mode.
        public void Clear()
        {
            if (!IsActive)
                return;
            _measure.Start();
            Render();
        }

        public float SnapRadius(PointerDevice device)
        {
            float dpi = _view.Dpi;
            float scale = dpi > 0f ? Mathf.Max(1f, dpi / 96f) : 1f;
            return (device == PointerDevice.Touch ? TouchSnapRadius : MouseSnapRadius) * scale;
        }

        public void Dispose()
        {
            _pointer.Tapped -= OnTapped;
            _loaded.Dispose();
        }

        // A tap on empty space is ignored: there is nothing to measure to.
        private void OnTapped(TapEvent tap)
        {
            if (!IsActive || !_session.HasModel || !_renderer.TryPick(tap.ScreenPosition, out var hit))
                return;

            CollectCandidates(hit.Index);
            _measure.Pick(MeasureSnapper.Snap(hit.Point, tap.ScreenPosition, _candidates, SnapRadius(tap.Device)));
            Render();
        }

        // Members snap to both ends and the middle of their centreline; panels and slabs only give free points.
        private void CollectCandidates(int index)
        {
            _candidates.Clear();
            var member = _session.Current.Elements[index].Member;
            if (member == null)
                return;

            AddCandidate(member.Start, SnapKind.Endpoint);
            AddCandidate(member.End, SnapKind.Endpoint);
            AddCandidate(member.Midpoint, SnapKind.Midpoint);
        }

        private void AddCandidate(Vector3 world, SnapKind kind)
        {
            if (_view.TryWorldToScreen(world, out var screen))
                _candidates.Add(new SnapCandidate(world, screen, kind));
        }

        private void Render()
        {
            if (!IsActive || !_measure.HasA)
            {
                _view.Render(MeasureVisual.None);
                return;
            }

            string label = null;
            if (_measure.HasResult)
            {
                var result = _measure.Result;
                label = $"{Mm(result.Distance)} mm\ndx {Mm(result.Delta.x)} · dy {Mm(result.Delta.y)} · dz {Mm(result.Delta.z)}";
            }
            _view.Render(new MeasureVisual(true, _measure.A, _measure.HasResult, _measure.B, label));
        }

        private static string Mm(float value) => value.ToString("N0", Format);
    }
}
