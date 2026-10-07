using System;
using System.Collections.Generic;
using System.Globalization;
using StructureViewer.Application.Events;
using StructureViewer.Application.Loading;
using StructureViewer.Domain.Takeoff;
using StructureViewer.Domain.Visibility;
using UnityEngine;

namespace StructureViewer.Presentation.Takeoff
{
    public sealed class TakeoffPresenter : IDisposable
    {
        private static readonly CultureInfo Format = CultureInfo.InvariantCulture;

        private readonly ITakeoffView _view;
        private readonly StructureSession _session;
        private readonly IDisposable _loaded;
        private readonly IDisposable _visibilityChanged;

        private IVisibility _visibility = AllVisible.Instance;
        private Func<string, Color?> _swatches;

        public TakeoffPresenter(ITakeoffView view, StructureSession session, EventBus bus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            if (bus == null)
                throw new ArgumentNullException(nameof(bus));

            _view.VisibleOnlyToggled += OnVisibleOnlyToggled;
            _loaded = bus.Subscribe<StructureLoaded>(_ =>
            {
                _visibility = AllVisible.Instance;
                Recalculate();
            });
            _visibilityChanged = bus.Subscribe<VisibilityChanged>(OnVisibilityChanged);
            Recalculate();
        }

        public bool VisibleOnly { get; private set; }
        public int CalculationCount { get; private set; }

        // Type → colour as "Color by Type" shows it; null hides the swatch column.
        public void SetSwatchProvider(Func<string, Color?> swatches)
        {
            _swatches = swatches;
            Recalculate();
        }

        public void Dispose()
        {
            _view.VisibleOnlyToggled -= OnVisibleOnlyToggled;
            _loaded.Dispose();
            _visibilityChanged.Dispose();
        }

        private void OnVisibleOnlyToggled(bool visibleOnly)
        {
            if (visibleOnly == VisibleOnly)
                return;
            VisibleOnly = visibleOnly;
            Recalculate();
        }

        // The full table doesn't depend on visibility, so it is only recalculated when "visible only" is on.
        private void OnVisibilityChanged(VisibilityChanged evt)
        {
            _visibility = evt.Visibility ?? AllVisible.Instance;
            if (VisibleOnly)
                Recalculate();
        }

        private void Recalculate()
        {
            if (!_session.HasModel)
            {
                _view.Render(TakeoffContent.Empty);
                return;
            }

            CalculationCount++;
            var table = TakeoffCalculator.Calculate(_session.Current, _visibility, VisibleOnly);
            var lines = new List<TakeoffLine>(table.Rows.Count + 1);
            foreach (var row in table.Rows)
                lines.Add(ToLine(row, isTotal: false));
            if (table.Rows.Count > 0)
                lines.Add(ToLine(table.Totals, isTotal: true));
            _view.Render(new TakeoffContent(lines, VisibleOnly, _swatches != null));
        }

        private TakeoffLine ToLine(TakeoffRow row, bool isTotal) =>
            new TakeoffLine(
                isTotal,
                isTotal || _swatches == null ? null : _swatches(row.Type),
                isTotal ? "Total" : row.Type,
                row.Section,
                row.Material ?? string.Empty,
                row.Count.ToString("N0", Format),
                row.Length > 0f ? row.Length.ToString("N2", Format) : string.Empty,
                row.Volume > 0f ? row.Volume.ToString("N3", Format) : string.Empty,
                row.Area > 0f ? row.Area.ToString("N2", Format) : string.Empty);
    }
}
