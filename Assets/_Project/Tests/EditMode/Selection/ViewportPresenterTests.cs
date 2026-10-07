using NUnit.Framework;
using StructureViewer.Application.Events;
using StructureViewer.Application.Loading;
using StructureViewer.Application.Selection;
using StructureViewer.Domain.Interaction;
using StructureViewer.Domain.Selection;
using StructureViewer.Domain.Structure;
using StructureViewer.Presentation.Contracts;
using StructureViewer.Presentation.Viewport;
using StructureViewer.Tests.Fixtures;
using UnityEngine;

namespace StructureViewer.Tests.EditMode.Selection
{
    public sealed class ViewportPresenterTests
    {
        private StructureModel _model;
        private EventBus _bus;
        private FakePointerEvents _pointer;
        private FakeStructureRenderer _renderer;
        private FakeCameraControl _camera;
        private SelectionService _selection;
        private ViewportPresenter _presenter;
        private EventRecorder<HoverChanged> _hovers;

        private int Stud => _model.IndexOf(TestStructures.VerticalStudId);
        private int Joist => _model.IndexOf(TestStructures.JoistId);

        [SetUp]
        public void SetUp()
        {
            _model = TestStructures.MiniHouse();
            _bus = new EventBus();
            var session = new StructureSession();
            session.Set(_model);
            _pointer = new FakePointerEvents();
            _renderer = new FakeStructureRenderer(_model);
            _camera = new FakeCameraControl();
            _selection = new SelectionService(session, _bus);
            _presenter = new ViewportPresenter(_pointer, _renderer, _camera, _selection, _bus);
            _hovers = new EventRecorder<HoverChanged>(_bus);
        }

        [TearDown]
        public void TearDown()
        {
            _hovers.Dispose();
            _presenter.Dispose();
            _selection.Dispose();
        }

        [Test]
        public void Tap_OnElement_SelectsMember()
        {
            _renderer.PickAlways(Stud);

            _pointer.RaiseTap(Vector2.one);

            Assert.AreEqual(SelectionKind.Member, _selection.Current.Kind);
            CollectionAssert.AreEqual(new[] { Stud }, _selection.Current.Indices);
        }

        [Test]
        public void DoubleTap_OnElement_SelectsItsAssembly()
        {
            _renderer.PickAlways(Stud);

            _pointer.RaiseTap(Vector2.one);
            _pointer.RaiseTap(Vector2.one, isDouble: true);

            Assert.AreEqual(SelectionKind.Assembly, _selection.Current.Kind);
            CollectionAssert.AreEquivalent(_model.ElementsInGroup(TestStructures.NorthWall), _selection.Current.Indices);
        }

        [Test]
        public void AdditiveTap_TogglesElement()
        {
            _renderer.PickAlways(Stud);
            _pointer.RaiseTap(Vector2.one);
            _renderer.PickAlways(Joist);

            _pointer.RaiseTap(Vector2.one, additive: true);
            Assert.AreEqual(SelectionKind.Multi, _selection.Current.Kind);

            _pointer.RaiseTap(Vector2.one, additive: true);
            CollectionAssert.AreEqual(new[] { Stud }, _selection.Current.Indices);
        }

        [Test]
        public void MultiSelectMode_MakesPlainTapsAdditive()
        {
            _pointer.Current = PointerDevice.Touch;
            _presenter.SetMultiSelect(true);
            _renderer.PickAlways(Stud);
            _pointer.RaiseTap(Vector2.one);
            _renderer.PickAlways(Joist);

            _pointer.RaiseTap(Vector2.one);

            CollectionAssert.AreEqual(new[] { Stud, Joist }, _selection.Current.Indices);
        }

        [Test]
        public void Tap_OnEmptySpace_ClearsSelection()
        {
            _selection.Select(Stud);

            _pointer.RaiseTap(Vector2.one);

            Assert.IsTrue(_selection.Current.IsEmpty);
        }

        [Test]
        public void AdditiveTap_OnEmptySpace_KeepsSelection()
        {
            _selection.Select(Stud);

            _pointer.RaiseTap(Vector2.one, additive: true);

            Assert.IsFalse(_selection.Current.IsEmpty);
        }

        [Test]
        public void Tap_InMeasureMode_IsIgnored()
        {
            _renderer.PickAlways(Stud);
            _bus.Publish(new InteractionModeChanged(InteractionMode.Measure));

            _pointer.RaiseTap(Vector2.one);

            Assert.IsTrue(_selection.Current.IsEmpty);
            Assert.IsEmpty(_renderer.PickRequests);
        }

        [Test]
        public void Tap_BackInSelectMode_SelectsAgain()
        {
            _renderer.PickAlways(Stud);
            _bus.Publish(new InteractionModeChanged(InteractionMode.Measure));
            _bus.Publish(new InteractionModeChanged(InteractionMode.Select));

            _pointer.RaiseTap(Vector2.one);

            Assert.IsFalse(_selection.Current.IsEmpty);
        }

        [Test]
        public void Hover_FromMouse_PublishesOnlyOnChange()
        {
            _renderer.PickAlways(Stud);
            _pointer.RaiseHover(Vector2.one);
            _pointer.RaiseHover(Vector2.one * 2f);
            _renderer.PickNothing();
            _pointer.RaiseHover(Vector2.one * 3f);

            Assert.AreEqual(2, _hovers.Count);
            Assert.AreEqual(Stud, _hovers.Events[0].Index);
            Assert.IsFalse(_hovers.Last.HasHover);
        }

        [Test]
        public void Hover_WhenCurrentDeviceIsTouch_DoesNotPick()
        {
            _pointer.Current = PointerDevice.Touch;
            _renderer.PickAlways(Stud);

            _pointer.RaiseHover(Vector2.one);

            Assert.IsEmpty(_renderer.PickRequests);
            Assert.AreEqual(0, _hovers.Count);
        }

        [Test]
        public void EnteringMeasureMode_ClearsHover()
        {
            _renderer.PickAlways(Stud);
            _pointer.RaiseHover(Vector2.one);

            _bus.Publish(new InteractionModeChanged(InteractionMode.Measure));

            Assert.IsFalse(_hovers.Last.HasHover);
        }

        [Test]
        public void FocusSelection_FocusesUnionOfSelectedBounds()
        {
            _selection.Toggle(Stud);
            _selection.Toggle(Joist);
            var expected = _renderer.GetWorldBounds(Stud);
            expected.Encapsulate(_renderer.GetWorldBounds(Joist));

            Assert.IsTrue(_presenter.FocusSelection());

            Assert.AreEqual(1, _camera.FocusCalls.Count);
            Assert.AreEqual(expected, _camera.FocusCalls[0]);
        }

        [Test]
        public void FocusSelection_Empty_ReturnsFalseAndLeavesCamera()
        {
            Assert.IsFalse(_presenter.FocusSelection());
            Assert.IsEmpty(_camera.FocusCalls);
        }

        [Test]
        public void Dispose_UnsubscribesFromPointerAndBus()
        {
            _presenter.Dispose();

            Assert.IsFalse(_pointer.HasSubscribers);
            _renderer.PickAlways(Stud);
            _pointer.RaiseHover(Vector2.one);
            _bus.Publish(new InteractionModeChanged(InteractionMode.Measure));
            Assert.AreEqual(0, _hovers.Count);
        }
    }
}
