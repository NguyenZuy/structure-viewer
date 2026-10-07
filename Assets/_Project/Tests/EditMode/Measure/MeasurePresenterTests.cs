using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using StructureViewer.Application.Events;
using StructureViewer.Application.Loading;
using StructureViewer.Domain.Interaction;
using StructureViewer.Domain.Measure;
using StructureViewer.Domain.Structure;
using StructureViewer.Presentation.Contracts;
using StructureViewer.Presentation.Measure;
using StructureViewer.Tests.Fixtures;
using UnityEngine;

namespace StructureViewer.Tests.EditMode.Measure
{
    public sealed class MeasurePresenterTests
    {
        private StructureModel _model;
        private EventBus _bus;
        private FakePointerEvents _pointer;
        private FakeStructureRenderer _renderer;
        private FakeMeasureView _view;
        private MeasurePresenter _presenter;
        private EventRecorder<InteractionModeChanged> _modes;

        private int StudIndex => _model.IndexOf(TestStructures.VerticalStudId);
        private Member Stud => _model.Elements[StudIndex].Member;

        [SetUp]
        public void SetUp()
        {
            _model = TestStructures.MiniHouse();
            _bus = new EventBus();
            var session = new StructureSession();
            session.Set(_model);
            _pointer = new FakePointerEvents();
            _renderer = new FakeStructureRenderer(_model);
            _view = new FakeMeasureView();
            _presenter = new MeasurePresenter(_pointer, _renderer, _view, session, _bus);
            _modes = new EventRecorder<InteractionModeChanged>(_bus);
        }

        [TearDown]
        public void TearDown()
        {
            _modes.Dispose();
            _presenter.Dispose();
        }

        // The fake view projects world (x, y) straight to screen pixels at 100 px per metre.
        private static Vector2 ToScreen(Vector3 world) => new Vector2(world.x, world.y) * FakeMeasureView.PixelsPerMetre;

        private void TapNear(Vector3 world, Vector2 offsetPx, PointerDevice device = PointerDevice.Mouse)
        {
            _renderer.PickAlways(StudIndex, world);
            _pointer.Current = device;
            _pointer.RaiseTap(ToScreen(world) + offsetPx);
        }

        [Test]
        public void SetActive_PublishesMeasureThenSelectMode()
        {
            _presenter.SetActive(true);
            _presenter.SetActive(true);
            _presenter.SetActive(false);

            Assert.AreEqual(2, _modes.Count);
            Assert.AreEqual(InteractionMode.Measure, _modes.Events[0].Mode);
            Assert.AreEqual(InteractionMode.Select, _modes.Last.Mode);
            Assert.IsFalse(_view.Last.HasA);
        }

        [Test]
        public void Inactive_IgnoresTaps()
        {
            TapNear(Stud.Start, Vector2.zero);

            Assert.IsEmpty(_renderer.PickRequests);
            Assert.AreEqual(MeasureState.Idle, _presenter.State);
        }

        [Test]
        public void TwoTapsNearStudEnds_SnapAndMeasureItsLength()
        {
            _presenter.SetActive(true);

            TapNear(Stud.Start, new Vector2(4f, 0f));
            TapNear(Stud.End, new Vector2(0f, -4f));

            var visual = _view.Last;
            Assert.IsTrue(visual.HasB);
            Assert.AreEqual(SnapKind.Endpoint, visual.A.Kind);
            Assert.AreEqual(Stud.Start, visual.A.World);
            Assert.AreEqual(Stud.End, visual.B.World);
            StringAssert.StartsWith((Stud.Length * 1000f).ToString("N0", CultureInfo.InvariantCulture) + " mm", visual.Label);
            StringAssert.Contains("dx 0 · dy 0", visual.Label);
        }

        [Test]
        public void TapNearMiddle_SnapsToMidpoint()
        {
            _presenter.SetActive(true);

            TapNear(Stud.Midpoint, new Vector2(3f, 3f));

            Assert.AreEqual(SnapKind.Midpoint, _view.Last.A.Kind);
        }

        [Test]
        public void Touch_UsesTheLargerSnapRadius()
        {
            _presenter.SetActive(true);
            var offset = new Vector2(20f, 0f);

            TapNear(Stud.Start, offset, PointerDevice.Mouse);
            Assert.AreEqual(SnapKind.Free, _view.Last.A.Kind);

            _presenter.Clear();
            TapNear(Stud.Start, offset, PointerDevice.Touch);
            Assert.AreEqual(SnapKind.Endpoint, _view.Last.A.Kind);
        }

        [Test]
        public void SnapRadius_ScalesWithDpi()
        {
            _view.Dpi = 192f;

            Assert.AreEqual(MeasurePresenter.TouchSnapRadius * 2f, _presenter.SnapRadius(PointerDevice.Touch), 1e-4f);
            _view.Dpi = 0f;
            Assert.AreEqual(MeasurePresenter.MouseSnapRadius, _presenter.SnapRadius(PointerDevice.Mouse), 1e-4f);
        }

        [Test]
        public void TapOnEmptySpace_IsIgnored()
        {
            _presenter.SetActive(true);
            _renderer.PickNothing();

            _pointer.RaiseTap(Vector2.zero);

            Assert.AreEqual(MeasureState.AwaitingA, _presenter.State);
        }

        [Test]
        public void Clear_DropsTheMeasurementButStaysActive()
        {
            _presenter.SetActive(true);
            TapNear(Stud.Start, Vector2.zero);

            _presenter.Clear();

            Assert.IsTrue(_presenter.IsActive);
            Assert.AreEqual(MeasureState.AwaitingA, _presenter.State);
            Assert.IsFalse(_view.Last.HasA);
        }

        [Test]
        public void Dispose_UnsubscribesFromPointer()
        {
            _presenter.Dispose();

            Assert.IsFalse(_pointer.HasSubscribers);
        }

        private sealed class FakeMeasureView : IMeasureView
        {
            public const float PixelsPerMetre = 100f;

            public float Dpi { get; set; } = 96f;
            public List<MeasureVisual> Renders { get; } = new List<MeasureVisual>();
            public MeasureVisual Last => Renders[Renders.Count - 1];

            public bool TryWorldToScreen(Vector3 world, out Vector2 screen)
            {
                screen = new Vector2(world.x, world.y) * PixelsPerMetre;
                return true;
            }

            public void Render(MeasureVisual visual) => Renders.Add(visual);
        }
    }
}
