using System;
using System.Collections.Generic;

namespace StructureViewer.Presentation.Layers
{
    public sealed class LayerPanelContent
    {
        public static readonly LayerPanelContent Empty = new LayerPanelContent(Array.Empty<LayerRow>(), Array.Empty<LayerRow>(), false);

        public LayerPanelContent(IReadOnlyList<LayerRow> categories, IReadOnlyList<LayerRow> levels, bool isFiltered)
        {
            Categories = categories ?? throw new ArgumentNullException(nameof(categories));
            Levels = levels ?? throw new ArgumentNullException(nameof(levels));
            IsFiltered = isFiltered;
        }

        // Only categories present in the model. Key = (int)ElementCategory.
        public IReadOnlyList<LayerRow> Categories { get; }

        // Key = level index.
        public IReadOnlyList<LayerRow> Levels { get; }

        // Isolating or hiding something: the panel offers "Show all" and explains why things are missing.
        public bool IsFiltered { get; }
    }

    public readonly struct LayerRow
    {
        public LayerRow(int key, string label, int count, bool isOn)
        {
            Key = key;
            Label = label;
            Count = count;
            IsOn = isOn;
        }

        public int Key { get; }
        public string Label { get; }
        public int Count { get; }
        public bool IsOn { get; }
    }
}
