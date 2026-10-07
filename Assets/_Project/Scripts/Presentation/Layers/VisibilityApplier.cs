using System;
using StructureViewer.Application.Events;
using StructureViewer.Presentation.Contracts;

namespace StructureViewer.Presentation.Layers
{
    // Pushes visibility to the renderer, touching only elements whose visibility changed (each touch dirties a chunk).
    public sealed class VisibilityApplier : IDisposable
    {
        private readonly IStructureRenderer _renderer;
        private readonly IDisposable _loaded;
        private readonly IDisposable _changed;

        // What the renderer currently shows; null forces a full pass (first state, or a new model was built).
        private bool[] _applied;

        public VisibilityApplier(IStructureRenderer renderer, EventBus bus)
        {
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            if (bus == null)
                throw new ArgumentNullException(nameof(bus));
            _loaded = bus.Subscribe<StructureLoaded>(_ => _applied = null);
            _changed = bus.Subscribe<VisibilityChanged>(OnChanged);
        }

        public void Dispose()
        {
            _loaded.Dispose();
            _changed.Dispose();
        }

        private void OnChanged(VisibilityChanged evt)
        {
            int count = _renderer.Count;
            bool full = _applied == null || _applied.Length != count;
            if (full)
                _applied = new bool[count];

            for (int i = 0; i < count; i++)
            {
                bool visible = evt.Visibility.IsVisible(i);
                if (!full && _applied[i] == visible)
                    continue;

                _applied[i] = visible;
                _renderer.SetVisible(i, visible);
            }
        }
    }
}
