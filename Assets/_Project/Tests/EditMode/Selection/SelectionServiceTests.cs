using NUnit.Framework;
using StructureViewer.Application.Events;
using StructureViewer.Application.Loading;
using StructureViewer.Application.Selection;
using StructureViewer.Domain.Selection;
using StructureViewer.Domain.Structure;
using StructureViewer.Tests.Fixtures;

namespace StructureViewer.Tests.EditMode.Selection
{
    public sealed class SelectionServiceTests
    {
        private StructureModel _model;
        private EventBus _bus;
        private StructureSession _session;
        private SelectionService _selection;
        private EventRecorder<SelectionChanged> _changes;

        [SetUp]
        public void SetUp()
        {
            _model = TestStructures.MiniHouse();
            _bus = new EventBus();
            _session = new StructureSession();
            _session.Set(_model);
            _selection = new SelectionService(_session, _bus);
            _changes = new EventRecorder<SelectionChanged>(_bus);
        }

        [TearDown]
        public void TearDown()
        {
            _changes.Dispose();
            _selection.Dispose();
        }

        private int Index(string id) => _model.IndexOf(id);

        [Test]
        public void Select_ReplacesPreviousSelection()
        {
            _selection.Select(Index(TestStructures.VerticalStudId));
            _selection.Select(Index(TestStructures.JoistId));

            Assert.AreEqual(SelectionKind.Member, _selection.Current.Kind);
            CollectionAssert.AreEqual(new[] { Index(TestStructures.JoistId) }, _selection.Current.Indices);
            Assert.AreEqual(2, _changes.Count);
            Assert.AreSame(_selection.Current, _changes.Last.Selection);
        }

        [Test]
        public void Select_SameElementTwice_PublishesOnce()
        {
            _selection.Select(Index(TestStructures.VerticalStudId));
            _selection.Select(Index(TestStructures.VerticalStudId));

            Assert.AreEqual(1, _changes.Count);
        }

        [Test]
        public void SelectAssembly_SelectsWholeGroup()
        {
            _selection.SelectAssembly(Index(TestStructures.VerticalStudId));

            Assert.AreEqual(SelectionKind.Assembly, _selection.Current.Kind);
            CollectionAssert.AreEquivalent(_model.ElementsInGroup(TestStructures.NorthWall), _selection.Current.Indices);
        }

        [Test]
        public void SelectAssembly_ElementWithoutGroup_SelectsJustTheElement()
        {
            var info = new ElementInfo("LOOSE", ElementCategory.Floor, "Blocking", string.Empty, 0);
            var member = new Member(UnityEngine.Vector3.zero, UnityEngine.Vector3.right, 0f, new Section(0.035f, 0.09f), "MGP10");
            _session.Set(new StructureModel("Loose", new[] { new Level(0, "L0", "Ground", 0f) }, new[] { Element.ForMember(0, info, member) }));

            _selection.SelectAssembly(0);

            Assert.AreEqual(SelectionKind.Member, _selection.Current.Kind);
            CollectionAssert.AreEqual(new[] { 0 }, _selection.Current.Indices);
        }

        [Test]
        public void Toggle_AddsThenRemoves_KindFollowsCount()
        {
            int stud = Index(TestStructures.VerticalStudId);
            int joist = Index(TestStructures.JoistId);

            _selection.Toggle(stud);
            Assert.AreEqual(SelectionKind.Member, _selection.Current.Kind);

            _selection.Toggle(joist);
            Assert.AreEqual(SelectionKind.Multi, _selection.Current.Kind);
            CollectionAssert.AreEqual(new[] { stud, joist }, _selection.Current.Indices);

            _selection.Toggle(joist);
            Assert.AreEqual(SelectionKind.Member, _selection.Current.Kind);
            CollectionAssert.AreEqual(new[] { stud }, _selection.Current.Indices);

            _selection.Toggle(stud);
            Assert.AreEqual(SelectionKind.None, _selection.Current.Kind);
            Assert.AreEqual(4, _changes.Count);
        }

        [Test]
        public void Toggle_AddingToAssembly_BecomesMulti()
        {
            _selection.SelectAssembly(Index(TestStructures.VerticalStudId));

            _selection.Toggle(Index(TestStructures.JoistId));

            Assert.AreEqual(SelectionKind.Multi, _selection.Current.Kind);
            Assert.AreEqual(_model.ElementsInGroup(TestStructures.NorthWall).Count + 1, _selection.Current.Count);
        }

        [Test]
        public void Clear_EmptiesSelection_AndDoesNothingWhenAlreadyEmpty()
        {
            _selection.Select(Index(TestStructures.VerticalStudId));

            _selection.Clear();
            _selection.Clear();

            Assert.IsTrue(_selection.Current.IsEmpty);
            Assert.AreEqual(2, _changes.Count);
        }

        [Test]
        public void RemoveWhere_MultiLeftWithOne_BecomesMember()
        {
            int stud = Index(TestStructures.VerticalStudId);
            int joist = Index(TestStructures.JoistId);
            _selection.Toggle(stud);
            _selection.Toggle(joist);

            _selection.RemoveWhere(i => i == joist);

            Assert.AreEqual(SelectionKind.Member, _selection.Current.Kind);
            CollectionAssert.AreEqual(new[] { stud }, _selection.Current.Indices);
        }

        [Test]
        public void RemoveWhere_PartOfAssembly_StaysAssembly()
        {
            int stud = Index(TestStructures.VerticalStudId);
            _selection.SelectAssembly(stud);

            _selection.RemoveWhere(i => i == stud);

            Assert.AreEqual(SelectionKind.Assembly, _selection.Current.Kind);
            Assert.IsFalse(_selection.Current.Contains(stud));
        }

        [Test]
        public void RemoveWhere_Everything_BecomesNone()
        {
            _selection.SelectAssembly(Index(TestStructures.VerticalStudId));

            _selection.RemoveWhere(_ => true);

            Assert.AreEqual(SelectionKind.None, _selection.Current.Kind);
        }

        [Test]
        public void RemoveWhere_NothingMatches_PublishesNothing()
        {
            _selection.Select(Index(TestStructures.VerticalStudId));
            _changes.Clear();

            _selection.RemoveWhere(_ => false);

            Assert.AreEqual(0, _changes.Count);
        }

        [Test]
        public void Select_InvalidIndexOrNoModel_IsIgnored()
        {
            _selection.Select(-1);
            _selection.Select(_model.Elements.Count);
            var empty = new SelectionService(new StructureSession(), _bus);
            empty.Select(0);
            empty.Dispose();

            Assert.AreEqual(0, _changes.Count);
        }

        [Test]
        public void StructureLoaded_ClearsSelection()
        {
            _selection.Select(Index(TestStructures.VerticalStudId));

            _bus.Publish(new StructureLoaded(_model));

            Assert.IsTrue(_selection.Current.IsEmpty);
        }

        [Test]
        public void Dispose_StopsListeningForLoads()
        {
            _selection.Select(Index(TestStructures.VerticalStudId));
            _selection.Dispose();

            _bus.Publish(new StructureLoaded(_model));

            Assert.IsFalse(_selection.Current.IsEmpty);
        }
    }
}
