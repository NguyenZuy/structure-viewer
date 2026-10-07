using NUnit.Framework;
using StructureViewer.Presentation.Layers;
using UnityEditor;
using UnityEngine.UIElements;

namespace StructureViewer.Tests.EditMode.Visibility
{
    // Pins the element names shared by LayerPanel.uxml and LayerPanelView, and the "update, don't rebuild" rule.
    public sealed class LayerPanelViewTests
    {
        private const string LayoutPath = "Assets/_Project/UI/Layers/LayerPanel.uxml";

        private LayerPanelView _view;

        [SetUp]
        public void SetUp() => _view = new LayerPanelView(AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LayoutPath));

        private static LayerPanelContent Content(bool wallsOn, bool filtered) =>
            new LayerPanelContent(
                new[] { new LayerRow(0, "Walls", 14, wallsOn), new LayerRow(4, "Slab", 1, true) },
                new[] { new LayerRow(0, "Ground Floor", 15, true) },
                filtered);

        [Test]
        public void Render_BuildsOneRowPerCategoryAndLevel()
        {
            _view.Render(Content(wallsOn: true, filtered: false));

            Assert.AreEqual(2, _view.Root.Q("layers-categories").childCount);
            Assert.AreEqual(1, _view.Root.Q("layers-levels").childCount);
            Assert.AreEqual(DisplayStyle.None, _view.Root.Q("layers-filter").style.display.value);
            Assert.AreEqual(DisplayStyle.None, _view.Root.Q("layers-empty").style.display.value);
        }

        [Test]
        public void Render_SameRows_UpdatesTogglesWithoutRebuilding()
        {
            _view.Render(Content(wallsOn: true, filtered: false));
            var toggle = _view.Root.Q("layers-categories").Q<Toggle>();

            _view.Render(Content(wallsOn: false, filtered: true));

            Assert.AreSame(toggle, _view.Root.Q("layers-categories").Q<Toggle>());
            Assert.IsFalse(toggle.value);
            Assert.AreEqual(DisplayStyle.Flex, _view.Root.Q("layers-filter").style.display.value);
        }

        [Test]
        public void Render_Empty_ShowsHint()
        {
            _view.Render(LayerPanelContent.Empty);

            Assert.AreEqual(DisplayStyle.Flex, _view.Root.Q("layers-empty").style.display.value);
            Assert.AreEqual(DisplayStyle.None, _view.Root.Q("layers-content").style.display.value);
        }
    }
}
