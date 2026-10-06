using System;
using System.Collections.Generic;

namespace StructureViewer.Domain.Selection
{
    // Immutable. Indices keep the order they were selected in.
    public sealed class SelectionSnapshot
    {
        public static readonly SelectionSnapshot Empty = new SelectionSnapshot(Array.Empty<int>(), SelectionKind.None);

        private readonly int[] _indices;
        private readonly HashSet<int> _set;

        public SelectionSnapshot(IReadOnlyList<int> indices, SelectionKind kind)
        {
            if (indices == null)
                throw new ArgumentNullException(nameof(indices));

            _set = new HashSet<int>();
            var unique = new List<int>(indices.Count);
            for (int i = 0; i < indices.Count; i++)
            {
                if (_set.Add(indices[i]))
                    unique.Add(indices[i]);
            }
            _indices = unique.ToArray();
            Kind = _indices.Length == 0 ? SelectionKind.None : kind;
        }

        public IReadOnlyList<int> Indices => _indices;
        public SelectionKind Kind { get; }
        public int Count => _indices.Length;
        public bool IsEmpty => _indices.Length == 0;

        public bool Contains(int index) => _set.Contains(index);

        public bool SameAs(SelectionSnapshot other) =>
            other != null && Kind == other.Kind && _set.SetEquals(other._set);
    }
}
