using NUnit.Framework;
using StructureViewer.Bootstrap;
using StructureViewer.Tests.Fixtures;
using UnityEngine.UIElements;

namespace StructureViewer.Tests.EditMode.Composition
{
    public sealed class PanelLayoutTests
    {
        private FakeShell _shell;
        private VisualElement _info;
        private VisualElement _layers;
        private VisualElement _legend;
        private VisualElement _takeoff;
        private VisualElement _display;
        private PanelLayout _layout;

        [SetUp]
        public void SetUp()
        {
            _shell = new FakeShell();
            _info = new VisualElement();
            _layers = new VisualElement();
            _legend = new VisualElement();
            _takeoff = new VisualElement();
            _display = new VisualElement();
            _layout = new PanelLayout(_shell, _info, _layers, _legend, _takeoff, _display);
        }

        [TearDown]
        public void TearDown() => _layout.Dispose();

        [Test]
        public void Desktop_LayersAndLegendLeft_InfoRight_TakeoffClosed()
        {
            Assert.AreEqual(_shell.LeftSlot, _layers.parent.parent);
            Assert.AreEqual(_layers.parent, _legend.parent);
            Assert.AreEqual(_shell.RightSlot, _info.parent);
            Assert.AreEqual(0, _shell.BottomSlot.childCount);
        }

        [Test]
        public void Desktop_ToggleTakeoffAndLayers_MountAndUnmount()
        {
            _layout.ToggleTakeoff();
            _layout.ToggleLayers();

            Assert.AreEqual(_shell.BottomSlot, _takeoff.parent);
            Assert.AreEqual(0, _shell.LeftSlot.childCount);
            Assert.IsTrue(_layout.IsTakeoffActive);
            Assert.IsFalse(_layout.IsLayersActive);
        }

        [Test]
        public void Compact_EmptiesSlots_AndOpensPanelsAsSheets()
        {
            _shell.SetCompact(true);

            Assert.AreEqual(0, _shell.LeftSlot.childCount);
            Assert.AreEqual(0, _shell.RightSlot.childCount);

            _layout.ToggleLayers();
            Assert.AreEqual(PanelLayout.LayersTitle, _shell.SheetTitle);
            Assert.IsTrue(_layout.IsLayersActive);

            _layout.ToggleLayers();
            Assert.IsNull(_shell.SheetContent);
        }

        [Test]
        public void Compact_SelectionOpensDetails_ClearingClosesThem()
        {
            _shell.SetCompact(true);

            _layout.OnSelectionChanged(true);
            Assert.AreEqual(_info, _shell.SheetContent);

            _layout.OnSelectionChanged(false);
            Assert.IsNull(_shell.SheetContent);
        }

        [Test]
        public void Compact_ClearingSelection_LeavesOtherSheetsOpen()
        {
            _shell.SetCompact(true);
            _layout.ToggleTakeoff();

            _layout.OnSelectionChanged(false);

            Assert.AreEqual(PanelLayout.TakeoffTitle, _shell.SheetTitle);
        }

        [Test]
        public void SwitchingBackToDesktop_ClosesSlotPanelSheetAndRemounts()
        {
            _shell.SetCompact(true);
            _layout.OnSelectionChanged(true);

            _shell.SetCompact(false);

            Assert.IsNull(_shell.SheetContent);
            Assert.AreEqual(_shell.RightSlot, _info.parent);
        }

        [Test]
        public void Display_OpensAsSheetOnDesktopToo()
        {
            _layout.ToggleDisplay();

            Assert.AreEqual(_display, _shell.SheetContent);
            Assert.IsTrue(_layout.IsDisplayOpen);
        }
    }
}
