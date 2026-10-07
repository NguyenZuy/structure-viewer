#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using StructureViewer.Presentation.Contracts;
using StructureViewer.Presentation.Shell;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace StructureViewer.Tests.PlayMode.Shell
{
    public sealed class ShellViewTests
    {
        private const string LayoutPath = "Assets/_Project/UI/Shell/MainLayout.uxml";
        private const string ThemePath = "Assets/_Project/UI/RuntimeTheme.tss";

        private GameObject _go;
        private PanelSettings _panelSettings;
        private UIDocument _document;
        private ShellView _shell;

        [SetUp]
        public void SetUp()
        {
            _panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            _panelSettings.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            _panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;

            _go = new GameObject("Shell");
            _go.SetActive(false);
            _document = _go.AddComponent<UIDocument>();
            _document.panelSettings = _panelSettings;
            _document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LayoutPath);
            _shell = _go.AddComponent<ShellView>();
            _go.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_go);
            Object.Destroy(_panelSettings);
        }

        [UnityTest]
        public IEnumerator AddFloatingOverlay_DrawsAboveSidePanelsButUnderTheSheet()
        {
            yield return Frames();
            var card = new VisualElement();

            _shell.AddFloatingOverlay(card);

            var overlay = Root.Q("overlay");
            Assert.AreSame(overlay, card.parent);
            var layers = overlay.parent;
            Assert.Less(layers.IndexOf(Root.Q("shell-root")), layers.IndexOf(overlay), "the overlay layer is drawn after the panels");
            Assert.Less(overlay.IndexOf(card), overlay.IndexOf(Root.Q("sheet")));
        }

        [UnityTest]
        public IEnumerator Toolbar_ButtonsAndOverflow_ShowVectorIcons()
        {
            yield return SetPanelWidth(1280f);
            _shell.AddToolbarItem(Item("fit", 0));
            yield return Frames();

            var glyph = Root.Q<Button>("toolbar-fit").Q(className: "sv-toolbar__glyph");
            Assert.IsNotNull(glyph.resolvedStyle.backgroundImage.vectorImage);
            Assert.That(glyph.resolvedStyle.width, Is.GreaterThan(0f));
            Assert.IsNotNull(Root.Q<Button>("toolbar-overflow").Q(className: "sv-toolbar__glyph").style.backgroundImage.value.vectorImage);
            Assert.IsNotNull(Root.Q<Button>("left-toggle").Q(className: "sv-icon").style.backgroundImage.value.vectorImage);
        }

        [UnityTest]
        public IEnumerator AddToolbarItem_CreatesTouchSizedButton()
        {
            yield return SetPanelWidth(1280f);
            _shell.AddToolbarItem(Item("fit", 0));
            yield return Frames();

            var button = Root.Q<Button>("toolbar-fit");
            Assert.That(button.resolvedStyle.width, Is.GreaterThanOrEqualTo(44f));
            Assert.That(button.resolvedStyle.height, Is.GreaterThanOrEqualTo(44f));
        }

        [UnityTest]
        public IEnumerator Compact_MovesLowPriorityItemsToOverflow()
        {
            yield return SetPanelWidth(1280f);
            for (int i = 0; i < 8; i++)
                _shell.AddToolbarItem(Item($"item{i}", priority: i));
            yield return Frames();
            Assert.IsFalse(_shell.IsCompact);
            Assert.That(VisibleItems(), Has.Length.EqualTo(8));
            Assert.IsTrue(Root.Q("toolbar-overflow").ClassListContains("sv-hidden"));

            bool? changed = null;
            _shell.CompactChanged += compact => changed = compact;
            yield return SetPanelWidth(360f);

            Assert.IsTrue(_shell.IsCompact);
            Assert.AreEqual(true, changed);
            Assert.IsTrue(Root.ClassListContains(ShellView.CompactClass));
            Assert.IsFalse(Root.Q("toolbar-overflow").ClassListContains("sv-hidden"));
            var visible = VisibleItems();
            Assert.That(visible.Length, Is.InRange(1, 7));
            // The highest priorities stay on the bar.
            CollectionAssert.Contains(visible, "toolbar-item7");
            CollectionAssert.DoesNotContain(visible, "toolbar-item0");
        }

        [UnityTest]
        public IEnumerator ShowSheet_ReplacesPreviousContent_AndHideRaisesEvent()
        {
            yield return SetPanelWidth(360f);
            var first = new Label("first");
            var second = new Label("second");
            int hidden = 0;
            _shell.SheetHidden += () => hidden++;

            _shell.ShowSheet(first, "First");
            _shell.ShowSheet(second, "Second");
            yield return Frames();

            Assert.IsNull(first.parent);
            Assert.IsNotNull(second.panel);
            Assert.IsFalse(Root.Q("sheet").ClassListContains("sv-hidden"));
            Assert.AreEqual("Second", Root.Q<Label>("sheet-title").text);
            Assert.AreEqual(0, hidden);

            _shell.HideSheet();
            _shell.HideSheet();

            Assert.IsNull(second.parent);
            Assert.IsTrue(Root.Q("sheet").ClassListContains("sv-hidden"));
            Assert.AreEqual(1, hidden);
        }

        [UnityTest]
        public IEnumerator ShowToast_KeepsAtMostThree()
        {
            yield return Frames();
            for (int i = 0; i < 5; i++)
                _shell.ShowToast($"toast {i}", isError: i == 4);

            var toasts = Root.Q("toasts");
            Assert.AreEqual(3, toasts.childCount);
            Assert.AreEqual("toast 4", ((Label)toasts[2]).text);
            Assert.IsTrue(toasts[2].ClassListContains("sv-toast--error"));
        }

        [UnityTest]
        public IEnumerator LayoutContainers_DoNotBlockTheViewport()
        {
            yield return SetPanelWidth(1280f);

            // The middle of the screen is the 3D view: nothing pickable may sit there.
            var centre = Root.worldBound.center;
            Assert.IsNull(Root.panel.Pick(centre));
        }

        private VisualElement Root => _document.rootVisualElement;

        private string[] VisibleItems() =>
            Root.Q("toolbar-items").Children()
                .Where(e => !e.ClassListContains("sv-hidden"))
                .Select(e => e.name)
                .ToArray();

        // ConstantPixelSize with scale = screen / width gives a panel exactly `width` points wide.
        private IEnumerator SetPanelWidth(float width)
        {
            _panelSettings.scale = Screen.width / width;
            yield return Frames();
        }

        private static IEnumerator Frames()
        {
            for (int i = 0; i < 3; i++)
                yield return null;
        }

        private static ToolbarItem Item(string id, int priority) =>
            new ToolbarItem(id, id, ToolbarIcon.Isolate, $"{id} tooltip", () => { }, priority: priority);
    }
}
#endif
