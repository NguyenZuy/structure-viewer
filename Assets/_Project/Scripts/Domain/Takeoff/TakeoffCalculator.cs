using System;
using System.Collections.Generic;
using System.Globalization;
using StructureViewer.Domain.Structure;
using StructureViewer.Domain.Visibility;

namespace StructureViewer.Domain.Takeoff
{
    // One line of the takeoff: all elements sharing type + section + material.
    public sealed class TakeoffRow
    {
        public TakeoffRow(ElementCategory category, ElementKind kind, string type, string section, string material)
        {
            Category = category;
            Kind = kind;
            Type = type;
            Section = section;
            Material = material;
        }

        public ElementCategory Category { get; }
        public ElementKind Kind { get; }
        public string Type { get; }

        // Members: "35 × 90". Panels and slabs: thickness, "12 mm".
        public string Section { get; }

        // Null for panels and slabs (the schema has no material for them).
        public string Material { get; }

        public int Count { get; private set; }

        // Metres, members only.
        public float Length { get; private set; }

        // Cubic metres, every kind.
        public float Volume { get; private set; }

        // Square metres, panels and slabs only.
        public float Area { get; private set; }

        internal void Add(int count, float length, float volume, float area)
        {
            Count += count;
            Length += length;
            Volume += volume;
            Area += area;
        }
    }

    public sealed class TakeoffTable
    {
        public TakeoffTable(IReadOnlyList<TakeoffRow> rows, TakeoffRow totals)
        {
            Rows = rows;
            Totals = totals;
        }

        public IReadOnlyList<TakeoffRow> Rows { get; }

        // Type/section/material are empty; Count, Length, Volume and Area are the sums of the rows.
        public TakeoffRow Totals { get; }
    }

    public static class TakeoffCalculator
    {
        private static readonly CultureInfo Format = CultureInfo.InvariantCulture;

        public static TakeoffTable Calculate(StructureModel model, IVisibility visibility, bool visibleOnly)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            if (visibleOnly && visibility == null)
                throw new ArgumentNullException(nameof(visibility));

            var rows = new Dictionary<(ElementKind, string, string, string), TakeoffRow>();
            var totals = new TakeoffRow(default, default, string.Empty, string.Empty, null);
            foreach (var element in model.Elements)
            {
                if (visibleOnly && !visibility.IsVisible(element.Index))
                    continue;

                var info = element.Info;
                string section;
                string material = null;
                float length = 0f, volume, area = 0f;
                if (element.Member != null)
                {
                    section = element.Member.Section.Label;
                    material = element.Member.Material;
                    length = element.Member.Length;
                    volume = element.Member.Volume;
                }
                else if (element.Panel != null)
                {
                    section = Thickness(element.Panel.Thickness);
                    area = element.Panel.Area;
                    volume = area * element.Panel.Thickness;
                }
                else
                {
                    section = Thickness(element.Slab.Thickness);
                    area = element.Slab.Area;
                    volume = element.Slab.Volume;
                }

                var key = (element.Kind, info.Type ?? string.Empty, section, material);
                if (!rows.TryGetValue(key, out var row))
                {
                    row = new TakeoffRow(info.Category, element.Kind, key.Item2, section, material);
                    rows.Add(key, row);
                }
                row.Add(1, length, volume, area);
                totals.Add(1, length, volume, area);
            }

            var sorted = new List<TakeoffRow>(rows.Values);
            sorted.Sort(Compare);
            return new TakeoffTable(sorted, totals);
        }

        private static int Compare(TakeoffRow a, TakeoffRow b)
        {
            int result = a.Category.CompareTo(b.Category);
            if (result == 0)
                result = string.CompareOrdinal(a.Type, b.Type);
            if (result == 0)
                result = string.CompareOrdinal(a.Section, b.Section);
            return result != 0 ? result : string.CompareOrdinal(a.Material, b.Material);
        }

        private static string Thickness(float metres) =>
            $"{Math.Round(metres * 1000f).ToString(Format)} mm";
    }
}
