using System;
using System.Collections.Generic;
using StructureViewer.Application.Events;
using StructureViewer.Application.Loading;
using StructureViewer.Domain.Selection;

namespace StructureViewer.Application.Selection
{
    // Owns the current selection. Not undoable (DESIGN.md), so plain methods rather than commands.
    public sealed class SelectionService : IDisposable
    {
        private readonly StructureSession _session;
        private readonly EventBus _bus;
        private readonly IDisposable _loaded;
        private readonly List<int> _buffer = new List<int>();

        public SelectionService(StructureSession session, EventBus bus)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            // Indices from the previous model mean nothing in a new one.
            _loaded = _bus.Subscribe<StructureLoaded>(_ => Clear());
        }

        public SelectionSnapshot Current { get; private set; } = SelectionSnapshot.Empty;

        public void Select(int index)
        {
            if (IsValid(index))
                Set(new SelectionSnapshot(new[] { index }, SelectionKind.Member));
        }

        // Elements without a group have no assembly: selects just the element.
        public void SelectAssembly(int index)
        {
            if (!IsValid(index))
                return;

            var model = _session.Current;
            var group = model.ElementsInGroup(model.Elements[index].Info.Group);
            if (group.Count == 0)
                Select(index);
            else
                Set(new SelectionSnapshot(group, SelectionKind.Assembly));
        }

        public void Toggle(int index)
        {
            if (!IsValid(index))
                return;

            _buffer.Clear();
            _buffer.AddRange(Current.Indices);
            if (!_buffer.Remove(index))
                _buffer.Add(index);
            Set(new SelectionSnapshot(_buffer.ToArray(), KindForCount(_buffer.Count)));
        }

        public void Clear() => Set(SelectionSnapshot.Empty);

        // A partly removed assembly stays an assembly; a multi-selection left with one element becomes a member.
        public void RemoveWhere(Func<int, bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            _buffer.Clear();
            var indices = Current.Indices;
            for (int i = 0; i < indices.Count; i++)
            {
                if (!predicate(indices[i]))
                    _buffer.Add(indices[i]);
            }
            if (_buffer.Count == indices.Count)
                return;

            var kind = Current.Kind == SelectionKind.Assembly ? SelectionKind.Assembly : KindForCount(_buffer.Count);
            Set(new SelectionSnapshot(_buffer.ToArray(), kind));
        }

        public void Dispose() => _loaded.Dispose();

        private bool IsValid(int index) =>
            _session.HasModel && index >= 0 && index < _session.Current.Elements.Count;

        private static SelectionKind KindForCount(int count) => count > 1 ? SelectionKind.Multi : SelectionKind.Member;

        private void Set(SelectionSnapshot next)
        {
            if (next.SameAs(Current))
                return;

            Current = next;
            _bus.Publish(new SelectionChanged(next));
        }
    }
}
