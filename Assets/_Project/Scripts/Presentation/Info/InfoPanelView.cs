using System;
using System.Collections.Generic;
using StructureViewer.Domain.Selection;
using UnityEngine.UIElements;

namespace StructureViewer.Presentation.Info
{
    // Plain panel element: the shell decides whether it lives in the right slot or a bottom sheet.
    public sealed class InfoPanelView : IInfoPanelView
    {
        public const string MemberClass = "sv-info--member";
        public const string AssemblyClass = "sv-info--assembly";
        public const string MultiClass = "sv-info--multi";

        private readonly Label _empty;
        private readonly VisualElement _content;
        private readonly Label _title;
        private readonly Label _subtitle;
        private readonly VisualElement _rows;
        private readonly Label _breakdownTitle;
        private readonly VisualElement _breakdown;
        private readonly Button _selectAssembly;
        private readonly Button _isolateAssembly;

        public InfoPanelView(VisualTreeAsset layout)
        {
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));

            Root = layout.Instantiate();
            Root.AddToClassList("sv-info-host");
            _empty = Require<Label>("info-empty");
            _content = Require<VisualElement>("info-content");
            _title = Require<Label>("info-title");
            _subtitle = Require<Label>("info-subtitle");
            _rows = Require<VisualElement>("info-rows");
            _breakdownTitle = Require<Label>("info-breakdown-title");
            _breakdown = Require<VisualElement>("info-breakdown");
            _selectAssembly = Require<Button>("info-select-assembly");
            _isolateAssembly = Require<Button>("info-isolate-assembly");

            _selectAssembly.clicked += () => SelectAssemblyClicked?.Invoke();
            _isolateAssembly.clicked += () => IsolateAssemblyClicked?.Invoke();
            Require<Button>("info-clear").clicked += () => ClearClicked?.Invoke();
        }

        public VisualElement Root { get; }

        public event Action SelectAssemblyClicked;
        public event Action IsolateAssemblyClicked;
        public event Action ClearClicked;

        public void Render(InfoPanelContent content)
        {
            Root.EnableInClassList(MemberClass, content.Kind == SelectionKind.Member);
            Root.EnableInClassList(AssemblyClass, content.Kind == SelectionKind.Assembly);
            Root.EnableInClassList(MultiClass, content.Kind == SelectionKind.Multi);

            Show(_empty, content.IsEmpty);
            Show(_content, !content.IsEmpty);
            if (content.IsEmpty)
            {
                _empty.text = content.Title;
                return;
            }

            _title.text = content.Title;
            _subtitle.text = content.Subtitle;
            FillRows(_rows, content.Rows);
            FillRows(_breakdown, content.Breakdown);
            Show(_breakdownTitle, content.Breakdown.Count > 0);
            Show(_selectAssembly, content.CanSelectAssembly);
            Show(_isolateAssembly, content.CanIsolateAssembly);
        }

        // Rebuilt per selection change, not per frame, so allocations here are fine.
        private static void FillRows(VisualElement container, IReadOnlyList<InfoRow> rows)
        {
            container.Clear();
            for (int i = 0; i < rows.Count; i++)
            {
                var row = new VisualElement();
                row.AddToClassList("sv-info__row");
                var label = new Label(rows[i].Label);
                label.AddToClassList("sv-info__label");
                var value = new Label(rows[i].Value);
                value.AddToClassList("sv-info__value");
                row.Add(label);
                row.Add(value);
                container.Add(row);
            }
        }

        private static void Show(VisualElement element, bool visible) =>
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        private T Require<T>(string name) where T : VisualElement =>
            Root.Q<T>(name) ?? throw new InvalidOperationException($"InfoPanel layout is missing '{name}'.");
    }
}
