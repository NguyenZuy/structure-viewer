using NUnit.Framework;
using StructureViewer.Domain.Structure;
using StructureViewer.Domain.Visibility;
using StructureViewer.Tests.Fixtures;

namespace StructureViewer.Tests.EditMode.Visibility
{
    public sealed class VisibilityStateTests
    {
        private StructureModel _model;
        private VisibilityState _all;

        [SetUp]
        public void SetUp()
        {
            _model = TestStructures.MiniHouse();
            _all = VisibilityState.AllVisible(_model);
        }

        private int Index(string id) => _model.IndexOf(id);

        private int VisibleCount(VisibilityState state)
        {
            int count = 0;
            for (int i = 0; i < _model.Elements.Count; i++)
            {
                if (state.IsVisible(i))
                    count++;
            }
            return count;
        }

        [Test]
        public void AllVisible_ShowsEveryElement()
        {
            Assert.AreEqual(_model.Elements.Count, VisibleCount(_all));
            Assert.IsFalse(_all.IsIsolating);
            Assert.IsFalse(_all.HasHidden);
        }

        [Test]
        public void WithCategoryOff_HidesOnlyThatCategory_AndLeavesOriginalUntouched()
        {
            var state = _all.WithCategory(ElementCategory.Wall, false);

            for (int i = 0; i < _model.Elements.Count; i++)
                Assert.AreEqual(_model.Elements[i].Info.Category != ElementCategory.Wall, state.IsVisible(i), _model.Elements[i].Info.Id);
            Assert.AreEqual(_model.Elements.Count, VisibleCount(_all));
        }

        [Test]
        public void WithLevelOff_HidesItsPanelsAndSlab()
        {
            var noFirst = _all.WithLevel(TestStructures.FirstLevel, false);
            var noGround = _all.WithLevel(TestStructures.GroundLevel, false);

            Assert.IsFalse(noFirst.IsVisible(Index(TestStructures.RoofPanelId)));
            Assert.IsFalse(noFirst.IsVisible(Index(TestStructures.JoistId)));
            Assert.IsTrue(noFirst.IsVisible(Index(TestStructures.SlabId)));
            Assert.IsFalse(noGround.IsVisible(Index(TestStructures.SlabId)));
        }

        [Test]
        public void WithIsolated_ShowsOnlyIsolated_AndEmptyEndsIsolation()
        {
            var wall = _model.ElementsInGroup(TestStructures.NorthWall);

            var isolated = _all.WithIsolated(wall);

            Assert.IsTrue(isolated.IsIsolating);
            Assert.AreEqual(wall.Count, VisibleCount(isolated));
            Assert.AreEqual(_all, isolated.WithIsolated(new int[0]));
        }

        [Test]
        public void IsolateWithCategoryOff_ShowsNothingOfThatCategory()
        {
            var state = _all.WithIsolated(_model.ElementsInGroup(TestStructures.NorthWall)).WithCategory(ElementCategory.Wall, false);

            Assert.AreEqual(0, VisibleCount(state));
        }

        [Test]
        public void HideInsideIsolate_HidesJustThatElement()
        {
            var wall = _model.ElementsInGroup(TestStructures.NorthWall);
            int stud = Index(TestStructures.VerticalStudId);

            var state = _all.WithIsolated(wall).WithHidden(new[] { stud });

            Assert.IsFalse(state.IsVisible(stud));
            Assert.AreEqual(wall.Count - 1, VisibleCount(state));
        }

        [Test]
        public void NoOpChanges_ReturnSameInstance()
        {
            int stud = Index(TestStructures.VerticalStudId);
            var hidden = _all.WithHidden(new[] { stud });

            Assert.AreSame(_all, _all.WithCategory(ElementCategory.Wall, true));
            Assert.AreSame(_all, _all.WithLevel(0, true));
            Assert.AreSame(_all, _all.WithIsolated(new int[0]));
            Assert.AreSame(hidden, hidden.WithHidden(new[] { stud }));
            Assert.AreSame(_all, _all.ShowAll());
        }

        [Test]
        public void ShowAll_ResetsLayersIsolationAndHidden()
        {
            var state = _all.WithCategory(ElementCategory.Roof, false)
                .WithLevel(0, false)
                .WithIsolated(_model.ElementsInGroup(TestStructures.Truss1))
                .WithHidden(new[] { Index(TestStructures.JoistId) });

            Assert.AreEqual(_all, state.ShowAll());
        }

        [Test]
        public void Equality_IsStructural()
        {
            var a = _all.WithCategory(ElementCategory.Roof, false).WithLevel(1, false);
            var b = _all.WithLevel(1, false).WithCategory(ElementCategory.Roof, false);
            var c = _all.WithLevel(1, false);

            Assert.AreEqual(a, b);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
            Assert.AreNotEqual(a, c);
            Assert.AreEqual(_all, a.WithCategory(ElementCategory.Roof, true).WithLevel(1, true));
        }
    }
}
