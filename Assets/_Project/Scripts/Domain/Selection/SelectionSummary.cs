using System;
using System.Collections.Generic;
using StructureViewer.Domain.Structure;

namespace StructureViewer.Domain.Selection
{
    // Totals for any set of elements. Length counts members only; area counts panels and slabs only.
    public sealed class SelectionSummary
    {
        private SelectionSummary(int count, float totalLength, float totalArea, IReadOnlyList<TypeTotal> byType)
        {
            Count = count;
            TotalLength = totalLength;
            TotalArea = totalArea;
            ByType = byType;
        }

        public int Count { get; }

        // Metres.
        public float TotalLength { get; }

        // Square metres.
        public float TotalArea { get; }

        // Most frequent type first, then by name, so the list is stable.
        public IReadOnlyList<TypeTotal> ByType { get; }

        public static SelectionSummary Of(StructureModel model, IReadOnlyList<int> indices)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            if (indices == null)
                throw new ArgumentNullException(nameof(indices));

            float length = 0f;
            float area = 0f;
            var totals = new Dictionary<string, TypeTotal>();
            for (int i = 0; i < indices.Count; i++)
            {
                var element = model.Elements[indices[i]];
                float elementLength = element.Member?.Length ?? 0f;
                float elementArea = element.Panel?.Area ?? element.Slab?.Area ?? 0f;
                length += elementLength;
                area += elementArea;

                string type = element.Info.Type ?? string.Empty;
                totals.TryGetValue(type, out var total);
                totals[type] = new TypeTotal(type, total.Count + 1, total.Length + elementLength, total.Area + elementArea);
            }

            var byType = new List<TypeTotal>(totals.Values);
            byType.Sort((a, b) => a.Count != b.Count ? b.Count.CompareTo(a.Count) : string.CompareOrdinal(a.Type, b.Type));
            return new SelectionSummary(indices.Count, length, area, byType);
        }
    }

    public readonly struct TypeTotal
    {
        public TypeTotal(string type, int count, float length, float area)
        {
            Type = type;
            Count = count;
            Length = length;
            Area = area;
        }

        public string Type { get; }
        public int Count { get; }
        public float Length { get; }
        public float Area { get; }
    }
}
