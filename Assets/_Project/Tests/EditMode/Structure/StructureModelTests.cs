using System;
using System.Linq;
using NUnit.Framework;
using StructureViewer.Domain.Structure;
using StructureViewer.Tests.Fixtures;
using UnityEngine;

namespace StructureViewer.Tests.EditMode.Structure
{
    public sealed class StructureModelTests
    {
        private const float Eps = 1e-5f;

        private StructureModel _model;

        [SetUp]
        public void SetUp() => _model = TestStructures.MiniHouse();

        [Test]
        public void ElementsInGroup_KnownGroup_ReturnsExactlyItsElements()
        {
            var indices = _model.ElementsInGroup(TestStructures.Truss1);

            Assert.AreEqual(4, indices.Count);
            Assert.IsTrue(indices.All(i => _model.Elements[i].Info.Group == TestStructures.Truss1));
        }

        [Test]
        public void ElementsInGroup_UnknownOrNullGroup_ReturnsEmpty()
        {
            CollectionAssert.IsEmpty(_model.ElementsInGroup("NOPE"));
            CollectionAssert.IsEmpty(_model.ElementsInGroup(null));
        }

        [Test]
        public void Groups_EveryGroupedElement_AppearsOnceInFirstAppearanceOrder()
        {
            CollectionAssert.AllItemsAreUnique(_model.Groups);
            Assert.AreEqual(TestStructures.NorthWall, _model.Groups[0]);
            Assert.AreEqual(TestStructures.SlabGroup, _model.Groups[_model.Groups.Count - 1]);

            int grouped = _model.Groups.Sum(g => _model.ElementsInGroup(g).Count);
            Assert.AreEqual(_model.Elements.Count, grouped);
        }

        [Test]
        public void ElementsInLevel_PartitionsAllElementsByLevel()
        {
            var ground = _model.ElementsInLevel(TestStructures.GroundLevel);
            var first = _model.ElementsInLevel(TestStructures.FirstLevel);

            Assert.AreEqual(_model.Elements.Count, ground.Count + first.Count);
            Assert.IsTrue(ground.All(i => _model.Elements[i].Info.LevelIndex == TestStructures.GroundLevel));
            Assert.IsTrue(first.All(i => _model.Elements[i].Info.LevelIndex == TestStructures.FirstLevel));
            Assert.Contains(_model.IndexOf(TestStructures.SlabId), ground.ToList());
            CollectionAssert.IsEmpty(_model.ElementsInLevel(5));
            CollectionAssert.IsEmpty(_model.ElementsInLevel(-1));
        }

        [Test]
        public void IndexOf_KnownAndUnknownIds_ReturnsIndexOrMinusOne()
        {
            int index = _model.IndexOf(TestStructures.VerticalStudId);

            Assert.AreEqual(TestStructures.VerticalStudId, _model.Elements[index].Info.Id);
            Assert.AreEqual(-1, _model.IndexOf("missing"));
            Assert.AreEqual(-1, _model.IndexOf(null));
        }

        [Test]
        public void Bounds_EncapsulatesEveryElement()
        {
            var outer = _model.Bounds;
            outer.Expand(Eps); // Bounds stores centre + extents, so min/max round-trip with float error.
            foreach (var element in _model.Elements)
            {
                var inner = element.Bounds;
                bool inside = inner.min.x >= outer.min.x && inner.min.y >= outer.min.y && inner.min.z >= outer.min.z
                              && inner.max.x <= outer.max.x && inner.max.y <= outer.max.y && inner.max.z <= outer.max.z;
                Assert.IsTrue(inside, $"{element.Info.Id} {inner} outside {outer}");
            }
        }

        [Test]
        public void Bounds_BottomIsSlabUnderside()
        {
            Assert.AreEqual(-TestStructures.SlabThickness, _model.Bounds.min.y, Eps);
        }

        [Test]
        public void Bounds_EmptyModel_IsZeroSize()
        {
            var empty = TestStructures.Empty();

            Assert.AreEqual(Vector3.zero, empty.Bounds.size);
            CollectionAssert.IsEmpty(empty.Groups);
        }

        [Test]
        public void Constructor_DuplicateId_Throws()
        {
            var info = new ElementInfo("A", ElementCategory.Wall, "Stud", "G", 0);
            var member = new Member(Vector3.zero, Vector3.up, 0f, new Section(0.035f, 0.09f), "MGP10");
            var levels = new[] { new Level(0, "L0", "Ground", 0f) };
            var elements = new[] { Element.ForMember(0, info, member), Element.ForMember(1, info, member) };

            Assert.Throws<ArgumentException>(() => new StructureModel("Dup", levels, elements));
        }

        [Test]
        public void Constructor_MissingLevel_Throws()
        {
            var info = new ElementInfo("A", ElementCategory.Wall, "Stud", "G", 1);
            var member = new Member(Vector3.zero, Vector3.up, 0f, new Section(0.035f, 0.09f), "MGP10");
            var levels = new[] { new Level(0, "L0", "Ground", 0f) };

            Assert.Throws<ArgumentException>(() => new StructureModel("Bad", levels, new[] { Element.ForMember(0, info, member) }));
        }
    }
}
