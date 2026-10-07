using NUnit.Framework;
using StructureViewer.Presentation.Shell;
using UnityEngine;

namespace StructureViewer.Tests.EditMode.Shell
{
    public sealed class ShellRulesTests
    {
        [TestCase(767f, true)]
        [TestCase(768f, false)]
        [TestCase(360f, true)]
        [TestCase(1920f, false)]
        public void IsCompact_BelowBreakpoint(float width, bool expected)
        {
            Assert.AreEqual(expected, ResponsiveRules.IsCompact(width));
        }

        [Test]
        public void SafeArea_NotchAndHomeBar_ConvertsToTopLeftPoints()
        {
            // 1080 × 2400 px portrait, 3 px per point; notch 120 px at the top, home bar 60 px at the bottom.
            var safe = new Rect(0f, 60f, 1080f, 2400f - 60f - 120f);

            var insets = ResponsiveRules.SafeArea(safe, new Vector2(1080f, 2400f), 3f);

            Assert.AreEqual(0f, insets.Left, 1e-4f);
            Assert.AreEqual(0f, insets.Right, 1e-4f);
            Assert.AreEqual(40f, insets.Top, 1e-4f);
            Assert.AreEqual(20f, insets.Bottom, 1e-4f);
        }

        [Test]
        public void SafeArea_LandscapeNotch_PadsTheSide()
        {
            var safe = new Rect(132f, 0f, 2400f - 132f, 1080f);

            var insets = ResponsiveRules.SafeArea(safe, new Vector2(2400f, 1080f), 2f);

            Assert.AreEqual(66f, insets.Left, 1e-4f);
            Assert.AreEqual(0f, insets.Right, 1e-4f);
        }

        [Test]
        public void Overflow_EverythingFits_AllVisibleNoOverflowButton()
        {
            var visible = new bool[3];

            bool overflow = ToolbarOverflow.Split(new[] { 1, 2, 3 }, 300f, 100f, 44f, visible);

            Assert.IsFalse(overflow);
            CollectionAssert.AreEqual(new[] { true, true, true }, visible);
        }

        [Test]
        public void Overflow_TooNarrow_KeepsHighestPrioritiesAndReservesOverflowSlot()
        {
            var visible = new bool[5];

            // Room for 3 items, but one slot goes to the overflow button.
            bool overflow = ToolbarOverflow.Split(new[] { 1, 5, 3, 4, 2 }, 3 * 56f, 56f, 44f, visible);

            Assert.IsTrue(overflow);
            CollectionAssert.AreEqual(new[] { false, true, false, true, false }, visible);
        }

        [Test]
        public void Overflow_EqualPriorities_EarlierItemWins()
        {
            var visible = new bool[3];

            ToolbarOverflow.Split(new[] { 0, 0, 0 }, 150f, 56f, 44f, visible);

            CollectionAssert.AreEqual(new[] { true, false, false }, visible);
        }

        [Test]
        public void Overflow_NoRoomAtAll_EverythingInMenu()
        {
            var visible = new bool[2];

            Assert.IsTrue(ToolbarOverflow.Split(new[] { 1, 2 }, 50f, 56f, 44f, visible));
            CollectionAssert.AreEqual(new[] { false, false }, visible);
        }

        [TestCase(SheetHeight.Half, 0f, SheetHeight.Full)]
        [TestCase(SheetHeight.Full, 5f, SheetHeight.Half)]
        [TestCase(SheetHeight.Half, 100f, SheetHeight.Full)]
        [TestCase(SheetHeight.Full, -100f, SheetHeight.Half)]
        [TestCase(SheetHeight.Half, -100f, SheetHeight.Closed)]
        public void SheetSnap_AfterDrag(SheetHeight current, float draggedUp, SheetHeight expected)
        {
            Assert.AreEqual(expected, SheetSnap.AfterDrag(current, draggedUp, 24f));
        }
    }
}
