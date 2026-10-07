using System;
using System.Collections.Generic;
using UnityEngine;

namespace StructureViewer.Presentation.Display
{
    public sealed class LegendContent
    {
        public static readonly LegendContent Hidden = new LegendContent(false, null, Array.Empty<LegendEntry>());

        public LegendContent(bool isVisible, string title, IReadOnlyList<LegendEntry> entries)
        {
            IsVisible = isVisible;
            Title = title;
            Entries = entries ?? throw new ArgumentNullException(nameof(entries));
        }

        // Only in "Color by" mode.
        public bool IsVisible { get; }
        public string Title { get; }
        public IReadOnlyList<LegendEntry> Entries { get; }
    }

    public readonly struct LegendEntry
    {
        public LegendEntry(Color color, string label, int count, bool isHidden)
        {
            Color = color;
            Label = label;
            Count = count;
            IsHidden = isHidden;
        }

        public Color Color { get; }
        public string Label { get; }
        public int Count { get; }
        public bool IsHidden { get; }
    }
}
