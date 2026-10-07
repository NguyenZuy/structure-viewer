using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace StructureViewer.Presentation.Takeoff
{
    // Plain panel element (bottom slot on desktop, full sheet on phones). The table scrolls horizontally on narrow screens.
    public sealed class TakeoffView : ITakeoffView
    {
        public const string TotalCellClass = "sv-takeoff__cell--total";
        private const string NumericCellClass = "sv-takeoff__cell--numeric";

        private readonly Toggle _visibleOnly;
        private readonly Label _empty;
        private readonly MultiColumnListView _table;
        private readonly Column _swatchColumn;
        private readonly List<TakeoffLine> _lines = new List<TakeoffLine>();

        public TakeoffView(VisualTreeAsset layout)
        {
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));

            Root = layout.Instantiate();
            Root.AddToClassList("sv-takeoff-host");
            _visibleOnly = Root.Q<Toggle>("takeoff-visible-only") ?? throw Missing("takeoff-visible-only");
            _empty = Root.Q<Label>("takeoff-empty") ?? throw Missing("takeoff-empty");
            var host = Root.Q("takeoff-table-host") ?? throw Missing("takeoff-table-host");
            _visibleOnly.RegisterValueChangedCallback(evt => VisibleOnlyToggled?.Invoke(evt.newValue));

            _table = new MultiColumnListView
            {
                name = "takeoff-table",
                selectionType = SelectionType.None,
                fixedItemHeight = 32f,
                horizontalScrollingEnabled = true,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                itemsSource = _lines
            };
            _table.AddToClassList("sv-takeoff__table");

            _swatchColumn = new Column
            {
                name = "swatch",
                title = string.Empty,
                width = 28f,
                resizable = false,
                makeCell = MakeSwatch,
                bindCell = (cell, i) => cell.style.backgroundColor = _lines[i].Swatch ?? Color.clear
            };
            _table.columns.Add(_swatchColumn);
            AddTextColumn("type", "Type", 150f, line => line.Type, numeric: false);
            AddTextColumn("section", "Section", 84f, line => line.Section, numeric: false);
            AddTextColumn("material", "Material", 84f, line => line.Material, numeric: false);
            AddTextColumn("count", "Qty", 56f, line => line.Count, numeric: true);
            AddTextColumn("length", "Length (m)", 92f, line => line.Length, numeric: true);
            AddTextColumn("volume", "Volume (m³)", 100f, line => line.Volume, numeric: true);
            AddTextColumn("area", "Area (m²)", 88f, line => line.Area, numeric: true);
            host.Add(_table);
        }

        public VisualElement Root { get; }
        public int LineCount => _lines.Count;

        public event Action<bool> VisibleOnlyToggled;

        public void Render(TakeoffContent content)
        {
            _lines.Clear();
            for (int i = 0; i < content.Lines.Count; i++)
                _lines.Add(content.Lines[i]);
            _visibleOnly.SetValueWithoutNotify(content.VisibleOnly);
            _swatchColumn.visible = content.ShowSwatches;

            bool empty = content.Lines.Count == 0;
            _empty.style.display = empty ? DisplayStyle.Flex : DisplayStyle.None;
            _table.style.display = empty ? DisplayStyle.None : DisplayStyle.Flex;
            _table.RefreshItems();
        }

        private void AddTextColumn(string name, string title, float width, Func<TakeoffLine, string> text, bool numeric)
        {
            _table.columns.Add(new Column
            {
                name = name,
                title = title,
                width = width,
                makeCell = () =>
                {
                    var label = new Label();
                    label.AddToClassList("sv-takeoff__cell");
                    if (numeric)
                        label.AddToClassList(NumericCellClass);
                    return label;
                },
                bindCell = (cell, i) =>
                {
                    var line = _lines[i];
                    ((Label)cell).text = text(line);
                    cell.EnableInClassList(TotalCellClass, line.IsTotal);
                }
            });
        }

        private static VisualElement MakeSwatch()
        {
            var swatch = new VisualElement();
            swatch.AddToClassList("sv-takeoff__swatch");
            return swatch;
        }

        private static InvalidOperationException Missing(string name) =>
            new InvalidOperationException($"Takeoff layout is missing '{name}'.");
    }
}
