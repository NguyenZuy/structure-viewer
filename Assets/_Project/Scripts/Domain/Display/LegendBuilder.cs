using System;
using System.Collections.Generic;
using StructureViewer.Domain.Structure;
using StructureViewer.Domain.Visibility;

namespace StructureViewer.Domain.Display
{
    public readonly struct LegendRow
    {
        public LegendRow(ColorKey key, ElementCategory category, string label, int count, bool isHidden)
        {
            Key = key;
            Category = category;
            Label = label;
            Count = count;
            IsHidden = isHidden;
        }

        public ColorKey Key { get; }

        // Category of the first element with this key (types missing from a palette fall back to it).
        public ElementCategory Category { get; }
        public string Label { get; }
        public int Count { get; }

        // Every element with this key is currently invisible.
        public bool IsHidden { get; }
    }

    public static class LegendBuilder
    {
        // Only keys present in the model. Order: category and level by ordinal; types grouped by category, then by name.
        public static IReadOnlyList<LegendRow> Build(StructureModel model, ColorByField field, IVisibility visibility)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            visibility ??= AllVisible.Instance;

            var entries = new Dictionary<ColorKey, Entry>();
            for (int i = 0; i < model.Elements.Count; i++)
            {
                var element = model.Elements[i];
                var key = ColorKeyResolver.KeyOf(element, field);
                if (!entries.TryGetValue(key, out var entry))
                {
                    entry = new Entry { Key = key, Category = element.Info.Category };
                    entries.Add(key, entry);
                }
                entry.Count++;
                if (visibility.IsVisible(i))
                    entry.Visible++;
            }

            var sorted = new List<Entry>(entries.Values);
            sorted.Sort(Compare);

            var rows = new List<LegendRow>(sorted.Count);
            foreach (var entry in sorted)
                rows.Add(new LegendRow(entry.Key, entry.Category, Label(model, entry.Key), entry.Count, entry.Visible == 0));
            return rows;
        }

        private static int Compare(Entry a, Entry b)
        {
            if (a.Key.Field != ColorByField.Type)
                return a.Key.Ordinal.CompareTo(b.Key.Ordinal);
            int byCategory = a.Category.CompareTo(b.Category);
            return byCategory != 0 ? byCategory : string.CompareOrdinal(a.Key.Name, b.Key.Name);
        }

        private static string Label(StructureModel model, ColorKey key) =>
            key.Field == ColorByField.Level ? model.Levels[key.Ordinal].Name : key.Name;

        private sealed class Entry
        {
            public ColorKey Key;
            public ElementCategory Category;
            public int Count;
            public int Visible;
        }
    }
}
