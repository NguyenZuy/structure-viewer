using System;
using StructureViewer.Application.Events;
using StructureViewer.Domain.Visibility;

namespace StructureViewer.Application.Visibility
{
    // Holds the single VisibilityState. Changes go through VisibilityCommand so they are undoable.
    public sealed class VisibilityService : IDisposable
    {
        private readonly EventBus _bus;
        private readonly IDisposable _loaded;

        public VisibilityService(EventBus bus)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _loaded = _bus.Subscribe<StructureLoaded>(evt => Publish(VisibilityState.AllVisible(evt.Model)));
        }

        // Null until a structure is loaded.
        public VisibilityState Current { get; private set; }

        // False when the state is unchanged (nothing published).
        public bool Set(VisibilityState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (state.Equals(Current))
                return false;

            Publish(state);
            return true;
        }

        public void Dispose() => _loaded.Dispose();

        private void Publish(VisibilityState state)
        {
            Current = state;
            _bus.Publish(new VisibilityChanged(state));
        }
    }
}
