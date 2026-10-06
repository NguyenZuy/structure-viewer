using System;
using System.Collections.Generic;
using StructureViewer.Application.Events;

namespace StructureViewer.Tests.Fixtures
{
    // Captures every T published on the bus until disposed.
    public sealed class EventRecorder<T> : IDisposable where T : struct
    {
        private readonly IDisposable _subscription;

        public EventRecorder(EventBus bus) => _subscription = bus.Subscribe<T>(Events.Add);

        public List<T> Events { get; } = new List<T>();
        public int Count => Events.Count;
        public T Last => Events.Count > 0 ? Events[Events.Count - 1] : throw new InvalidOperationException($"No {typeof(T).Name} recorded.");

        public void Clear() => Events.Clear();

        public void Dispose() => _subscription.Dispose();
    }
}
