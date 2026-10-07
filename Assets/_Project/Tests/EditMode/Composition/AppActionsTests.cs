using System.Collections.Generic;
using NUnit.Framework;
using StructureViewer.Application.Commands;
using StructureViewer.Application.Display;
using StructureViewer.Application.Events;
using StructureViewer.Application.Loading;
using StructureViewer.Application.Selection;
using StructureViewer.Application.Visibility;
using StructureViewer.Bootstrap;
using StructureViewer.Domain.Display;
using StructureViewer.Domain.Interaction;
using StructureViewer.Domain.Labels;
using StructureViewer.Domain.Measure;
using StructureViewer.Domain.Structure;
using StructureViewer.Presentation.Input;
using StructureViewer.Presentation.Labels;
using StructureViewer.Presentation.Measure;
using StructureViewer.Presentation.Viewport;
using StructureViewer.Tests.Fixtures;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace StructureViewer.Tests.EditMode.Composition
{
    public sealed class AppActionsTests
    {
        private StructureModel _model;
        private EventBus _bus;
        private FakeShell _shell;
        private FakeCameraControl _camera;
        private FakeStructureRenderer _renderer;
        private FakePointerEvents _pointer;
        private SelectionService _selection;
        private VisibilityService _visibility;
        private DisplaySettings _display;
        private MeasurePresenter _measure;
        private LabelsPresenter _labels;
        private readonly List<System.IDisposable> _owned = new List<System.IDisposable>();
        private AppActions _actions;

        [SetUp]
        public void SetUp()
        {
            _model = TestStructures.MiniHouse();
            _bus = new EventBus();
            var session = new StructureSession();
            _shell = new FakeShell();
            _camera = new FakeCameraControl();
            _renderer = new FakeStructureRenderer(_model);
            _pointer = new FakePointerEvents();
            var history = new CommandHistory();

            _selection = Own(new SelectionService(session, _bus));
            _visibility = Own(new VisibilityService(_bus));
            var visibilityActions = Own(new VisibilityActions(_visibility, history, session, _bus));
            _display = new DisplaySettings(_bus);
            var viewport = Own(new ViewportPresenter(_pointer, _renderer, _camera, _selection, _bus));
            _measure = Own(new MeasurePresenter(_pointer, _renderer, new StubMeasureView(), session, _bus));
            _labels = Own(new LabelsPresenter(new StubLabelsView(), _bus));
            var panels = Own(new PanelLayout(_shell, new VisualElement(), new VisualElement(), new VisualElement(), new VisualElement(), new VisualElement()));
            _actions = Own(new AppActions(_shell, _camera, _renderer, session, viewport, visibilityActions, _visibility, history,
                _display, _measure, _labels, panels, _bus));
            _actions.RegisterToolbar();

            session.Set(_model);
            _bus.Publish(new StructureLoaded(_model));
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _owned.Count - 1; i >= 0; i--)
                _owned[i].Dispose();
            _owned.Clear();
        }

        private T Own<T>(T disposable) where T : System.IDisposable
        {
            _owned.Add(disposable);
            return disposable;
        }

        private int Stud => _model.IndexOf(TestStructures.VerticalStudId);

        [Test]
        public void RegisterToolbar_AddsEveryActionOnce()
        {
            CollectionAssert.IsSupersetOf(_shell.ToolbarItems.Keys, new[]
            {
                ShortcutMap.FitAll, ShortcutMap.Focus, ShortcutMap.Undo, ShortcutMap.Redo, AppActions.Display, AppActions.Layers,
                ShortcutMap.Isolate, ShortcutMap.Hide, ShortcutMap.ShowAll, ShortcutMap.Measure, AppActions.Takeoff,
                AppActions.LabelsToggle, AppActions.MultiSelect
            });
        }

        // Every shortcut must have an on-screen button (CLAUDE.md › PC + mobile); Esc and 1–4 map to Measure/Display.
        [Test]
        public void EveryShortcut_HasAToolbarButton()
        {
            foreach (var key in ShortcutMap.Keys)
            {
                foreach (var (ctrl, shift) in new[] { (false, false), (false, true), (true, false), (true, true) })
                {
                    string action = ShortcutMap.Resolve(key, ctrl, shift);
                    if (action == null || action == ShortcutMap.Escape || action.StartsWith("mode-"))
                        continue;
                    Assert.IsTrue(_shell.ToolbarItems.ContainsKey(action), $"{key} → {action}");
                }
            }
        }

        [Test]
        public void UndoRedo_EnabledStateFollowsHistory()
        {
            Assert.IsFalse(_shell.ToolbarStates[ShortcutMap.Undo].Enabled);

            _selection.Select(Stud);
            _shell.Click(ShortcutMap.Hide);
            Assert.IsTrue(_shell.ToolbarStates[ShortcutMap.Undo].Enabled);
            Assert.IsTrue(_shell.ToolbarStates[ShortcutMap.ShowAll].Enabled);

            _actions.Execute(ShortcutMap.Undo);
            Assert.IsTrue(_shell.ToolbarStates[ShortcutMap.Redo].Enabled);
            Assert.IsTrue(_visibility.Current.IsVisible(Stud));
        }

        [Test]
        public void IsolateWithoutSelection_ShowsToast()
        {
            _shell.Click(ShortcutMap.Isolate);

            Assert.AreEqual(AppActions.NothingSelected, _shell.Toasts[0].Message);
            Assert.IsFalse(_visibility.Current.IsIsolating);
        }

        [Test]
        public void Isolate_MarksTheButtonActive()
        {
            _selection.Select(Stud);

            _actions.Execute(ShortcutMap.Isolate);

            Assert.IsTrue(_shell.ToolbarStates[ShortcutMap.Isolate].Active);
        }

        [Test]
        public void HidingTheSelection_DeselectsIt_ViaBootstrapReaction()
        {
            // AppBootstrap wires VisibilityChanged → RemoveWhere; mirrored here to pin the contract between B09 and B10.
            using var reaction = _bus.Subscribe<VisibilityChanged>(evt => _selection.RemoveWhere(i => !evt.Visibility.IsVisible(i)));
            _selection.Select(Stud);

            _actions.Execute(ShortcutMap.Hide);

            Assert.IsTrue(_selection.Current.IsEmpty);
        }

        [Test]
        public void Escape_StepsBack_MeasurementThenMeasureModeThenSelection()
        {
            _selection.Select(Stud);
            _actions.Execute(ShortcutMap.Measure);
            Assert.IsTrue(_measure.IsActive);
            _renderer.PickAlways(Stud, Vector3.one);
            _pointer.RaiseTap(Vector2.zero);
            Assert.AreEqual(MeasureState.AwaitingB, _measure.State);

            _actions.Execute(ShortcutMap.Escape);
            Assert.AreEqual(MeasureState.AwaitingA, _measure.State);
            _actions.Execute(ShortcutMap.Escape);
            Assert.IsFalse(_measure.IsActive);
            Assert.IsFalse(_selection.Current.IsEmpty, "measuring never touched the selection");
            _actions.Execute(ShortcutMap.Escape);
            Assert.IsTrue(_selection.Current.IsEmpty);
        }

        [Test]
        public void ModeShortcuts_SwitchDisplayMode()
        {
            _actions.Execute(ShortcutMap.Mode3);
            Assert.AreEqual(DisplayMode.XRay, _display.Mode);

            _actions.Execute(ShortcutMap.Mode1);
            Assert.AreEqual(DisplayMode.Realistic, _display.Mode);
        }

        [Test]
        public void FitAll_FramesModelBounds_FocusWithoutSelectionToasts()
        {
            _actions.Execute(ShortcutMap.FitAll);
            _actions.Execute(ShortcutMap.Focus);

            Assert.AreEqual(_renderer.ModelBounds, _camera.FitAllCalls[0]);
            Assert.AreEqual(AppActions.NothingToFocus, _shell.Toasts[0].Message);
        }

        [Test]
        public void Labels_OffOnCompact_UntilTheUserChooses()
        {
            Assert.IsTrue(_labels.IsEnabled);

            _shell.SetCompact(true);
            Assert.IsFalse(_labels.IsEnabled);

            _shell.Click(AppActions.LabelsToggle);
            _shell.SetCompact(false);
            _shell.SetCompact(true);
            Assert.IsTrue(_labels.IsEnabled);
        }

        [Test]
        public void MultiSelectToggle_MakesTapsAdditive()
        {
            _shell.Click(AppActions.MultiSelect);
            _renderer.PickAlways(Stud);
            _pointer.RaiseTap(Vector2.zero);
            _renderer.PickAlways(_model.IndexOf(TestStructures.JoistId));
            _pointer.RaiseTap(Vector2.zero);

            Assert.AreEqual(2, _selection.Current.Count);
            Assert.IsTrue(_shell.ToolbarStates[AppActions.MultiSelect].Active);
        }

        [Test]
        public void ShortcutMap_ResolvesModifiers()
        {
            Assert.AreEqual(ShortcutMap.Undo, ShortcutMap.Resolve(Key.Z, ctrl: true, shift: false));
            Assert.AreEqual(ShortcutMap.Redo, ShortcutMap.Resolve(Key.Z, ctrl: true, shift: true));
            Assert.AreEqual(ShortcutMap.Redo, ShortcutMap.Resolve(Key.Y, ctrl: true, shift: false));
            Assert.AreEqual(ShortcutMap.ShowAll, ShortcutMap.Resolve(Key.H, ctrl: false, shift: true));
            Assert.AreEqual(ShortcutMap.Hide, ShortcutMap.Resolve(Key.H, ctrl: false, shift: false));
            Assert.AreEqual(ShortcutMap.Mode2, ShortcutMap.Resolve(Key.Numpad2, ctrl: false, shift: false));
            Assert.IsNull(ShortcutMap.Resolve(Key.Z, ctrl: false, shift: false));
            Assert.IsNull(ShortcutMap.Resolve(Key.F, ctrl: true, shift: false));
        }

        [Test]
        public void MeasureMode_PublishesInteractionModeSoTapsDontSelect()
        {
            using var modes = new EventRecorder<InteractionModeChanged>(_bus);
            _shell.Click(ShortcutMap.Measure);
            _renderer.PickAlways(Stud);

            _pointer.RaiseTap(Vector2.zero);

            Assert.AreEqual(InteractionMode.Measure, modes.Last.Mode);
            Assert.IsTrue(_selection.Current.IsEmpty);
            Assert.AreEqual(AppActions.MeasureHint, _shell.Toasts[0].Message);
        }

        private sealed class StubMeasureView : IMeasureView
        {
            public float Dpi => 96f;

            public bool TryWorldToScreen(Vector3 world, out Vector2 screen)
            {
                screen = new Vector2(world.x, world.y);
                return true;
            }

            public void Render(MeasureVisual visual)
            {
            }
        }

        private sealed class StubLabelsView : ILabelsView
        {
            public void ShowAnchors(IReadOnlyList<AssemblyAnchor> anchors, float nearFade, float farFade)
            {
            }

            public void SetGroupVisible(int anchorIndex, bool visible)
            {
            }

            public void SetEmphasized(int anchorIndex)
            {
            }

            public void SetEnabled(bool enabled)
            {
            }
        }
    }
}
