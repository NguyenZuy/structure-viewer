using System;
using System.Collections.Generic;
using UnityEngine;

namespace StructureViewer.Presentation.Takeoff
{
    public sealed class TakeoffContent
    {
        public static readonly TakeoffContent Empty = new TakeoffContent(Array.Empty<TakeoffLine>(), false, false);

        public TakeoffContent(IReadOnlyList<TakeoffLine> lines, bool visibleOnly, bool showSwatches)
        {
            Lines = lines ?? throw new ArgumentNullException(nameof(lines));
            VisibleOnly = visibleOnly;
            ShowSwatches = showSwatches;
        }

        // Data lines followed by the totals line (when there is any data).
        public IReadOnlyList<TakeoffLine> Lines { get; }
        public bool VisibleOnly { get; }
        public bool ShowSwatches { get; }
    }

    // Already formatted for display; empty strings where a column doesn't apply (no length for panels, no area for members).
    public sealed class TakeoffLine
    {
        public TakeoffLine(bool isTotal, Color? swatch, string type, string section, string material,
            string count, string length, string volume, string area)
        {
            IsTotal = isTotal;
            Swatch = swatch;
            Type = type;
            Section = section;
            Material = material;
            Count = count;
            Length = length;
            Volume = volume;
            Area = area;
        }

        public bool IsTotal { get; }
        public Color? Swatch { get; }
        public string Type { get; }
        public string Section { get; }
        public string Material { get; }
        public string Count { get; }
        public string Length { get; }
        public string Volume { get; }
        public string Area { get; }
    }
}
