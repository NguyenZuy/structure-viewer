using System;
using NUnit.Framework;
using StructureViewer.Application.Commands;
using StructureViewer.Application.Events;
using StructureViewer.Application.Loading;
using StructureViewer.Application.Visibility;
using StructureViewer.Domain.Selection;
using StructureViewer.Domain.Structure;
using StructureViewer.Domain.Visibility;
using StructureViewer.Tests.Fixtures;

namespace StructureViewer.Tests.EditMode.Visibility
{
    public sealed class VisibilityActionsTests
    {
        private StructureModel _model;
        private EventBus _bus;
        private StructureSession _session;
        private VisibilityService _service;
        private CommandHistory _history;
        private VisibilityActions _actions;

        [SetUp]
        public void SetUp()
        {
            _model = TestStructures.MiniHouse();
            _bus = new EventBus();
            _session = new StructureSession();
            _service = new VisibilityService(_bus);
            _history = new CommandHistory();
            _actions = new VisibilityActions(_service, _history, _session, _bus);
            Load();
        }

        [TearDown]
        public void TearDown()
        {
            _actions.Dispose();
            _service.Dispose();
        }

        private void Load()
        {
            _session.Set(_model);
            _bus.Publish(new StructureLoaded(_model));
        }

        private int Index(string id) => _model.IndexOf(id);

        private void SelectMembers(params int[] indices) =>
            _bus.Publish(new SelectionChanged(new SelectionSnapshot(indices, indices.Length > 1 ? SelectionKind.Multi : SelectionKind.Member)));

        private static readonly (string Name, Func<VisibilityActionsTests, bool> Run)[] Actions =
        {
            ("category", t => t._actions.SetCategoryVisible(ElementCategory.Wall, false)),
            ("level", t => t._actions.SetLevelVisible(TestStructures.FirstLevel, false)),
            ("isolate selection", t => { t.SelectMembers(t.Index(TestStructures.JoistId)); return t._actions.IsolateSelection(); }),
            ("isolate level", t => t._actions.IsolateLevel(TestStructures.GroundLevel)),
            ("isolate group", t => t._actions.IsolateGroup(TestStructures.Truss1)),
            ("hide selection", t => { t.SelectMembers(t.Index(TestStructures.JoistId)); return t._actions.HideSelection(); })
        };

        [Test]
        public void EveryAction_UndoRestoresExactState_RedoReapplies()
        {
            foreach (var (name, run) in Actions)
            {
                var before = _service.Current;

                Assert.IsTrue(run(this), name);
                var after = _service.Current;
                Assert.AreNotEqual(before, after, name);

                _actions.Undo();
                Assert.AreSame(before, _service.Current, name);

                _actions.Redo();
                Assert.AreSame(after, _service.Current, name);

                _actions.Undo();
            }
        }

        [Test]
        public void ShowAll_IsUndoable()
        {
            _actions.SetCategoryVisible(ElementCategory.Roof, false);
            var filtered = _service.Current;

            Assert.IsTrue(_actions.ShowAll());
            Assert.AreEqual(VisibilityState.AllVisible(_model), _service.Current);

            _actions.Undo();
            Assert.AreSame(filtered, _service.Current);
        }

        [Test]
        public void NoOp_IsNotRecorded()
        {
            Assert.IsFalse(_actions.SetCategoryVisible(ElementCategory.Wall, true));
            Assert.IsFalse(_actions.ShowAll());
            Assert.IsFalse(_history.CanUndo);
        }

        [Test]
        public void NewActionAfterUndo_ClearsRedo()
        {
            _actions.SetCategoryVisible(ElementCategory.Wall, false);
            _actions.Undo();

            _actions.SetCategoryVisible(ElementCategory.Roof, false);

            Assert.IsFalse(_history.CanRedo);
        }

        [Test]
        public void IsolateSelection_WithoutSelection_DoesNothing()
        {
            Assert.IsFalse(_actions.IsolateSelection());
            Assert.IsFalse(_actions.HideSelection());
            Assert.IsFalse(_history.CanUndo);
        }

        [Test]
        public void IsolateSelection_ShowsOnlySelected()
        {
            int joist = Index(TestStructures.JoistId);
            SelectMembers(joist);

            _actions.IsolateSelection();

            for (int i = 0; i < _model.Elements.Count; i++)
                Assert.AreEqual(i == joist, _service.Current.IsVisible(i));
        }

        [Test]
        public void IsolateLevel_SwitchesTheLevelBackOn()
        {
            _actions.SetLevelVisible(TestStructures.FirstLevel, false);

            _actions.IsolateLevel(TestStructures.FirstLevel);

            Assert.IsTrue(_service.Current.IsLevelOn(TestStructures.FirstLevel));
            Assert.IsTrue(_service.Current.IsVisible(Index(TestStructures.JoistId)));
            Assert.IsFalse(_service.Current.IsVisible(Index(TestStructures.SlabId)));
        }

        [Test]
        public void IsolateGroup_UnknownGroupOrInvalidLevel_DoesNothing()
        {
            Assert.IsFalse(_actions.IsolateGroup("NOPE"));
            Assert.IsFalse(_actions.IsolateLevel(99));
            Assert.IsFalse(_actions.SetLevelVisible(-1, false));
            Assert.IsFalse(_history.CanUndo);
        }

        [Test]
        public void HideSelection_HidesSelectedElements()
        {
            int stud = Index(TestStructures.VerticalStudId);
            int joist = Index(TestStructures.JoistId);
            SelectMembers(stud, joist);

            _actions.HideSelection();

            Assert.IsFalse(_service.Current.IsVisible(stud));
            Assert.IsFalse(_service.Current.IsVisible(joist));
            Assert.IsTrue(_service.Current.HasHidden);
        }

        [Test]
        public void BeforeLoad_ActionsDoNothing()
        {
            var bus = new EventBus();
            var service = new VisibilityService(bus);
            var history = new CommandHistory();
            var actions = new VisibilityActions(service, history, new StructureSession(), bus);

            Assert.IsFalse(actions.SetCategoryVisible(ElementCategory.Wall, false));
            Assert.IsFalse(actions.ShowAll());
            Assert.IsFalse(history.CanUndo);
            actions.Dispose();
            service.Dispose();
        }

        [Test]
        public void Service_PublishesOnlyRealChanges_AndResetsOnLoad()
        {
            using var changes = new EventRecorder<VisibilityChanged>(_bus);

            Assert.IsFalse(_service.Set(_service.Current));
            Assert.IsTrue(_service.Set(_service.Current.WithCategory(ElementCategory.Slab, false)));
            Load();

            Assert.AreEqual(2, changes.Count);
            Assert.AreEqual(VisibilityState.AllVisible(_model), _service.Current);
        }
    }
}
