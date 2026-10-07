using System;
using System.Collections.Generic;
using StructureViewer.Application.Display;
using StructureViewer.Application.Events;
using StructureViewer.Application.Loading;
using StructureViewer.Domain.Display;
using StructureViewer.Domain.Visibility;

namespace StructureViewer.Presentation.Display
{
    public sealed class LegendPresenter : IDisposable
    {
        private readonly ILegendView _view;
        private readonly DisplaySettings _settings;
        private readonly StructureSession _session;
        private readonly DisplayPaletteAsset _palette;
        private readonly IDisposable[] _subscriptions;

        private IVisibility _visibility = AllVisible.Instance;

        public LegendPresenter(ILegendView view, DisplaySettings settings, StructureSession session, DisplayPaletteAsset palette, EventBus bus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _palette = palette ? palette : throw new ArgumentNullException(nameof(palette));
            if (bus == null)
                throw new ArgumentNullException(nameof(bus));

            _subscriptions = new[]
            {
                bus.Subscribe<DisplayModeChanged>(_ => Render()),
                bus.Subscribe<VisibilityChanged>(evt =>
                {
                    _visibility = evt.Visibility ?? AllVisible.Instance;
                    Render();
                }),
                bus.Subscribe<StructureLoaded>(_ =>
                {
                    _visibility = AllVisible.Instance;
                    Render();
                })
            };
            Render();
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
                subscription.Dispose();
        }

        // Rebuilt on mode, field and visibility changes only; never per frame.
        private void Render()
        {
            if (_settings.Mode != DisplayMode.ColorBy || !_session.HasModel)
            {
                _view.Render(LegendContent.Hidden);
                return;
            }

            var rows = LegendBuilder.Build(_session.Current, _settings.Field, _visibility);
            var entries = new List<LegendEntry>(rows.Count);
            foreach (var row in rows)
                entries.Add(new LegendEntry(_palette.ColorOf(row.Key, row.Category), row.Label, row.Count, row.IsHidden));
            _view.Render(new LegendContent(true, $"Color by {_settings.Field}", entries));
        }
    }
}
