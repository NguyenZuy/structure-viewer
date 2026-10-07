using System.Collections.Generic;
using NUnit.Framework;
using StructureViewer.Presentation.Contracts;
using StructureViewer.Presentation.Input;
using UnityEngine;

namespace StructureViewer.Tests.EditMode.Input
{
    public sealed class GestureClassifierTests
    {
        private static readonly GestureSettings Settings = GestureSettings.ForDpi(96f);

        private GestureClassifier _classifier;
        private List<TapEvent> _taps;
        private List<Vector2> _orbits;
        private List<Vector2> _pans;
        private List<(float Amount, Vector2 Focus)> _zooms;
        private List<Vector2> _hovers;

        [SetUp]
        public void SetUp()
        {
            _classifier = new GestureClassifier(Settings);
            _taps = new List<TapEvent>();
            _orbits = new List<Vector2>();
            _pans = new List<Vector2>();
            _zooms = new List<(float, Vector2)>();
            _hovers = new List<Vector2>();
            _classifier.Tapped += _taps.Add;
            _classifier.Orbited += _orbits.Add;
            _classifier.Panned += _pans.Add;
            _classifier.Zoomed += (amount, focus) => _zooms.Add((amount, focus));
            _classifier.Hovered += _hovers.Add;
        }

        [Test]
        public void Click_SmallMove_IsATap()
        {
            Click(new Vector2(100f, 100f), new Vector2(103f, 102f), time: 1f);

            Assert.AreEqual(1, _taps.Count);
            Assert.IsFalse(_taps[0].IsDouble);
            Assert.AreEqual(PointerDevice.Mouse, _taps[0].Device);
            CollectionAssert.IsEmpty(_orbits);
        }

        [Test]
        public void LeftDrag_BeyondThreshold_OrbitsAndDoesNotTap()
        {
            _classifier.MouseDown(MouseButton.Left, new Vector2(100f, 100f));
            _classifier.MouseMove(new Vector2(120f, 100f));
            _classifier.MouseMove(new Vector2(130f, 105f));
            _classifier.MouseUp(MouseButton.Left, new Vector2(130f, 105f), false, 1f);

            CollectionAssert.IsEmpty(_taps);
            Assert.AreEqual(2, _orbits.Count);
            Assert.AreEqual(new Vector2(20f, 0f), _orbits[0]);
            Assert.AreEqual(new Vector2(10f, 5f), _orbits[1]);
        }

        [TestCase(MouseButton.Right)]
        [TestCase(MouseButton.Middle)]
        public void RightOrMiddleDrag_Pans(MouseButton button)
        {
            _classifier.MouseDown(button, new Vector2(100f, 100f));
            _classifier.MouseMove(new Vector2(100f, 130f));
            _classifier.MouseUp(button, new Vector2(100f, 130f), false, 1f);

            Assert.AreEqual(1, _pans.Count);
            CollectionAssert.IsEmpty(_orbits);
            CollectionAssert.IsEmpty(_taps);
        }

        [Test]
        public void RightClick_WithoutMove_IsNotATap()
        {
            _classifier.MouseDown(MouseButton.Right, new Vector2(10f, 10f));
            _classifier.MouseUp(MouseButton.Right, new Vector2(10f, 10f), false, 1f);

            CollectionAssert.IsEmpty(_taps);
        }

        [Test]
        public void TwoClicks_InsideWindow_SecondIsDouble()
        {
            Click(new Vector2(100f, 100f), new Vector2(100f, 100f), time: 1f);
            Click(new Vector2(104f, 101f), new Vector2(104f, 101f), time: 1.2f);

            Assert.AreEqual(2, _taps.Count);
            Assert.IsFalse(_taps[0].IsDouble);
            Assert.IsTrue(_taps[1].IsDouble);
        }

        [Test]
        public void TwoClicks_TooSlow_AreTwoSingles()
        {
            Click(new Vector2(100f, 100f), new Vector2(100f, 100f), time: 1f);
            Click(new Vector2(100f, 100f), new Vector2(100f, 100f), time: 1.5f);

            Assert.IsFalse(_taps[1].IsDouble);
        }

        [Test]
        public void TwoClicks_TooFarApart_AreTwoSingles()
        {
            Click(new Vector2(100f, 100f), new Vector2(100f, 100f), time: 1f);
            Click(new Vector2(200f, 100f), new Vector2(200f, 100f), time: 1.1f);

            Assert.IsFalse(_taps[1].IsDouble);
        }

        [Test]
        public void ThreeQuickClicks_AreDoubleThenSingle()
        {
            Click(Vector2.zero, Vector2.zero, time: 1f);
            Click(Vector2.zero, Vector2.zero, time: 1.1f);
            Click(Vector2.zero, Vector2.zero, time: 1.2f);

            Assert.IsTrue(_taps[1].IsDouble);
            Assert.IsFalse(_taps[2].IsDouble);
        }

        [Test]
        public void Click_WithCtrl_IsAdditive()
        {
            _classifier.MouseDown(MouseButton.Left, Vector2.zero);
            _classifier.MouseUp(MouseButton.Left, Vector2.zero, additive: true, time: 1f);

            Assert.IsTrue(_taps[0].Additive);
        }

        [Test]
        public void Wheel_ZoomsInTowardCursor()
        {
            _classifier.Scroll(120f, new Vector2(300f, 200f));
            _classifier.Scroll(-120f, new Vector2(300f, 200f));

            Assert.AreEqual(2, _zooms.Count);
            Assert.Greater(_zooms[0].Amount, 1f);
            Assert.Less(_zooms[1].Amount, 1f);
            Assert.AreEqual(new Vector2(300f, 200f), _zooms[0].Focus);
        }

        [Test]
        public void Wheel_HugeDelta_IsClamped()
        {
            _classifier.Scroll(100000f, Vector2.zero);

            Assert.AreEqual(Mathf.Pow(GestureSettings.DefaultZoomPerNotch, 3f), _zooms[0].Amount, 1e-4f);
        }

        [Test]
        public void MouseMove_WithoutPress_Hovers()
        {
            _classifier.MouseMove(new Vector2(5f, 5f));
            _classifier.MouseMove(new Vector2(5f, 5f));
            _classifier.MouseMove(new Vector2(6f, 5f));

            Assert.AreEqual(2, _hovers.Count, "unchanged position should not re-hover");
        }

        [Test]
        public void Touch_HoldStill_IsATouchTap()
        {
            _classifier.Touches(1, new Vector2(50f, 50f), default, 1f);
            _classifier.Touches(1, new Vector2(52f, 51f), default, 1.05f);
            _classifier.Touches(0, default, default, 1.1f);

            Assert.AreEqual(1, _taps.Count);
            Assert.AreEqual(PointerDevice.Touch, _taps[0].Device);
            Assert.IsFalse(_taps[0].Additive);
            CollectionAssert.IsEmpty(_hovers);
        }

        [Test]
        public void Touch_OneFingerDrag_OrbitsAndDoesNotTap()
        {
            _classifier.Touches(1, new Vector2(50f, 50f), default, 1f);
            _classifier.Touches(1, new Vector2(90f, 50f), default, 1.05f);
            _classifier.Touches(0, default, default, 1.1f);

            Assert.AreEqual(1, _orbits.Count);
            CollectionAssert.IsEmpty(_taps);
        }

        [Test]
        public void Touch_TwoFingersMoveTogether_Pans()
        {
            _classifier.Touches(2, new Vector2(100f, 100f), new Vector2(200f, 100f), 1f);
            _classifier.Touches(2, new Vector2(100f, 130f), new Vector2(200f, 130f), 1.05f);

            Assert.AreEqual(new Vector2(0f, 30f), _pans[0]);
            CollectionAssert.IsEmpty(_zooms);
            CollectionAssert.IsEmpty(_orbits);
        }

        [Test]
        public void Touch_Spread_ZoomsInAtCentroid()
        {
            _classifier.Touches(2, new Vector2(100f, 100f), new Vector2(200f, 100f), 1f);
            _classifier.Touches(2, new Vector2(50f, 100f), new Vector2(250f, 100f), 1.05f);

            Assert.AreEqual(1, _zooms.Count);
            Assert.AreEqual(2f, _zooms[0].Amount, 1e-4f);
            Assert.AreEqual(new Vector2(150f, 100f), _zooms[0].Focus);
        }

        [Test]
        public void Touch_FingerLiftedMidPinch_NoTapAndNoOrbit()
        {
            _classifier.Touches(1, new Vector2(100f, 100f), default, 1f);
            _classifier.Touches(2, new Vector2(100f, 100f), new Vector2(200f, 100f), 1.02f);
            _classifier.Touches(2, new Vector2(90f, 100f), new Vector2(210f, 100f), 1.04f);
            _classifier.Touches(1, new Vector2(90f, 100f), default, 1.06f);
            _classifier.Touches(1, new Vector2(150f, 160f), default, 1.08f);
            _classifier.Touches(0, default, default, 1.1f);

            CollectionAssert.IsEmpty(_taps);
            CollectionAssert.IsEmpty(_orbits);
            Assert.AreEqual(1, _zooms.Count);
        }

        [Test]
        public void Touch_DoubleTap_IsFlaggedDouble()
        {
            TouchTap(new Vector2(50f, 50f), 1f);
            TouchTap(new Vector2(55f, 52f), 1.2f);

            Assert.IsTrue(_taps[1].IsDouble);
        }

        [Test]
        public void Settings_DragThresholdScalesWithDpi()
        {
            var normal = GestureSettings.ForDpi(96f);
            var dense = GestureSettings.ForDpi(288f);
            var unknown = GestureSettings.ForDpi(0f);

            Assert.AreEqual(normal.DragThreshold * 3f, dense.DragThreshold, 1e-4f);
            Assert.AreEqual(normal.DragThreshold, unknown.DragThreshold, 1e-4f);
        }

        [Test]
        public void HighDpi_SameSmallPhysicalMove_StaysATap()
        {
            var classifier = new GestureClassifier(GestureSettings.ForDpi(288f));
            int taps = 0;
            classifier.Tapped += _ => taps++;

            // 15 px is under the 24 px threshold at 3x density.
            classifier.MouseDown(MouseButton.Left, Vector2.zero);
            classifier.MouseMove(new Vector2(15f, 0f));
            classifier.MouseUp(MouseButton.Left, new Vector2(15f, 0f), false, 1f);

            Assert.AreEqual(1, taps);
        }

        private void Click(Vector2 down, Vector2 up, float time)
        {
            _classifier.MouseDown(MouseButton.Left, down);
            _classifier.MouseUp(MouseButton.Left, up, false, time);
        }

        private void TouchTap(Vector2 position, float time)
        {
            _classifier.Touches(1, position, default, time);
            _classifier.Touches(0, default, default, time + 0.05f);
        }
    }
}
