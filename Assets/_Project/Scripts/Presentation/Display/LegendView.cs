using System;
using UnityEngine.UIElements;

namespace StructureViewer.Presentation.Display
{
    // Plain panel element; hides itself outside "Color by". The shell decides where it is mounted.
    public sealed class LegendView : ILegendView
    {
        public const string HiddenRowClass = "sv-legend__row--hidden";

        private readonly Label _title;
        private readonly VisualElement _rows;

        public LegendView(VisualTreeAsset layout)
        {
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));

            Root = layout.Instantiate();
            Root.AddToClassList("sv-legend-host");
            _title = Root.Q<Label>("legend-title") ?? throw new InvalidOperationException("Legend layout is missing 'legend-title'.");
            _rows = Root.Q("legend-rows") ?? throw new InvalidOperationException("Legend layout is missing 'legend-rows'.");
        }

        public VisualElement Root { get; }

        public void Render(LegendContent content)
        {
            Root.style.display = content.IsVisible ? DisplayStyle.Flex : DisplayStyle.None;
            _title.text = content.Title;
            _rows.Clear();
            foreach (var entry in content.Entries)
            {
                var row = new VisualElement();
                row.AddToClassList("sv-legend__row");
                row.EnableInClassList(HiddenRowClass, entry.IsHidden);

                var swatch = new VisualElement();
                swatch.AddToClassList("sv-legend__swatch");
                swatch.style.backgroundColor = entry.Color;
                row.Add(swatch);

                var label = new Label(entry.Label);
                label.AddToClassList("sv-legend__label");
                row.Add(label);

                var count = new Label(entry.Count.ToString());
                count.AddToClassList("sv-legend__count");
                row.Add(count);

                _rows.Add(row);
            }
        }
    }
}
