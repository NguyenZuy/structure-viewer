using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace StructureViewer.Presentation.Layers
{
    // Plain panel element: the shell decides whether it lives in the left slot or a sheet.
    public sealed class LayerPanelView : ILayerPanelView
    {
        private readonly VisualElement _empty;
        private readonly VisualElement _content;
        private readonly VisualElement _filter;
        private readonly VisualElement _categories;
        private readonly VisualElement _levels;
        private readonly List<Toggle> _categoryToggles = new List<Toggle>();
        private readonly List<Toggle> _levelToggles = new List<Toggle>();

        private LayerPanelContent _built;

        public LayerPanelView(VisualTreeAsset layout)
        {
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));

            Root = layout.Instantiate();
            Root.AddToClassList("sv-layers-host");
            _empty = Require<VisualElement>("layers-empty");
            _content = Require<VisualElement>("layers-content");
            _filter = Require<VisualElement>("layers-filter");
            _categories = Require<VisualElement>("layers-categories");
            _levels = Require<VisualElement>("layers-levels");
            Require<Button>("layers-show-all").clicked += () => ShowAllClicked?.Invoke();
        }

        public VisualElement Root { get; }

        public event Action<int, bool> CategoryToggled;
        public event Action<int, bool> LevelToggled;
        public event Action<int> IsolateLevelClicked;
        public event Action ShowAllClicked;

        public void Render(LayerPanelContent content)
        {
            bool empty = content.Categories.Count == 0 && content.Levels.Count == 0;
            Show(_empty, empty);
            Show(_content, !empty);
            Show(_filter, content.IsFiltered);

            // Rows are only rebuilt for a new model; toggles are updated without notify so they never echo back as input.
            if (_built == null || !SameRows(_built.Categories, content.Categories) || !SameRows(_built.Levels, content.Levels))
            {
                BuildRows(_categories, _categoryToggles, content.Categories, isLevel: false);
                BuildRows(_levels, _levelToggles, content.Levels, isLevel: true);
                _built = content;
            }

            for (int i = 0; i < content.Categories.Count; i++)
                _categoryToggles[i].SetValueWithoutNotify(content.Categories[i].IsOn);
            for (int i = 0; i < content.Levels.Count; i++)
                _levelToggles[i].SetValueWithoutNotify(content.Levels[i].IsOn);
        }

        private void BuildRows(VisualElement container, List<Toggle> toggles, IReadOnlyList<LayerRow> rows, bool isLevel)
        {
            container.Clear();
            toggles.Clear();
            for (int i = 0; i < rows.Count; i++)
            {
                int key = rows[i].Key;
                var row = new VisualElement();
                row.AddToClassList("sv-layers__row");

                var toggle = new Toggle(rows[i].Label);
                toggle.AddToClassList("sv-layers__toggle");
                if (isLevel)
                    toggle.RegisterValueChangedCallback(evt => LevelToggled?.Invoke(key, evt.newValue));
                else
                    toggle.RegisterValueChangedCallback(evt => CategoryToggled?.Invoke(key, evt.newValue));
                row.Add(toggle);
                toggles.Add(toggle);

                var count = new Label(rows[i].Count.ToString());
                count.AddToClassList("sv-layers__count");
                row.Add(count);

                if (isLevel)
                {
                    var isolate = new Button(() => IsolateLevelClicked?.Invoke(key)) { text = "Isolate" };
                    isolate.AddToClassList("sv-layers__button");
                    row.Add(isolate);
                }

                container.Add(row);
            }
        }

        private static bool SameRows(IReadOnlyList<LayerRow> a, IReadOnlyList<LayerRow> b)
        {
            if (a.Count != b.Count)
                return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i].Key != b[i].Key || a[i].Label != b[i].Label || a[i].Count != b[i].Count)
                    return false;
            }
            return true;
        }

        private static void Show(VisualElement element, bool visible) =>
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        private T Require<T>(string name) where T : VisualElement =>
            Root.Q<T>(name) ?? throw new InvalidOperationException($"LayerPanel layout is missing '{name}'.");
    }
}
