using NUnit.Framework;
using StructureViewer.Domain.Selection;
using StructureViewer.Presentation.Info;
using UnityEditor;
using UnityEngine.UIElements;

namespace StructureViewer.Tests.EditMode.Selection
{
    // Pins the element names shared by InfoPanel.uxml and InfoPanelView.
    public sealed class InfoPanelViewTests
    {
        private const string LayoutPath = "Assets/_Project/UI/Info/InfoPanel.uxml";

        private InfoPanelView _view;

        [SetUp]
        public void SetUp() => _view = new InfoPanelView(AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LayoutPath));

        [Test]
        public void Render_Member_ShowsDetailsAndHidesEmptyHint()
        {
            var rows = new[] { new InfoRow("Type", "Stud"), new InfoRow("Length", "2,330 mm") };

            _view.Render(new InfoPanelContent(SelectionKind.Member, "W-ST01", "Stud", rows, null, true, true));

            Assert.AreEqual("W-ST01", _view.Root.Q<Label>("info-title").text);
            Assert.AreEqual(2, _view.Root.Q("info-rows").childCount);
            Assert.AreEqual(DisplayStyle.None, _view.Root.Q("info-empty").style.display.value);
            Assert.AreEqual(DisplayStyle.None, _view.Root.Q("info-breakdown-title").style.display.value);
            Assert.IsTrue(_view.Root.ClassListContains(InfoPanelView.MemberClass));
        }

        [Test]
        public void Render_Empty_ShowsHintOnly()
        {
            _view.Render(InfoPanelContent.Empty("Tap a member"));

            Assert.AreEqual("Tap a member", _view.Root.Q<Label>("info-empty").text);
            Assert.AreEqual(DisplayStyle.None, _view.Root.Q("info-content").style.display.value);
            Assert.IsFalse(_view.Root.ClassListContains(InfoPanelView.MemberClass));
        }

        [Test]
        public void Render_Multi_HidesAssemblyButtons()
        {
            _view.Render(new InfoPanelContent(SelectionKind.Multi, "2 selected", null, null, new[] { new InfoRow("Stud", "2") }, false, false));

            Assert.AreEqual(DisplayStyle.None, _view.Root.Q("info-select-assembly").style.display.value);
            Assert.AreEqual(DisplayStyle.None, _view.Root.Q("info-isolate-assembly").style.display.value);
            Assert.AreEqual(1, _view.Root.Q("info-breakdown").childCount);
        }
    }
}
