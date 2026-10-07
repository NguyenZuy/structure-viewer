using System.Linq;
using NUnit.Framework;
using StructureViewer.Domain.Geometry;

namespace StructureViewer.Tests.EditMode.Geometry
{
    public sealed class ChunkPlannerTests
    {
        private static readonly int[] Group = { 0, 1, 2, 3 };

        [Test]
        public void Plan_SameMaterial_OneChunkWithAllElements()
        {
            var chunks = ChunkPlanner.Plan(Group, new[] { true, true, true, true }, new[] { 0, 0, 0, 0 });

            Assert.AreEqual(1, chunks.Count);
            CollectionAssert.AreEqual(Group, chunks[0].Elements);
        }

        [Test]
        public void Plan_HiddenElements_AreExcluded()
        {
            var chunks = ChunkPlanner.Plan(Group, new[] { true, false, true, false }, new[] { 0, 0, 0, 0 });

            CollectionAssert.AreEqual(new[] { 0, 2 }, chunks.Single().Elements);
        }

        [Test]
        public void Plan_OneElementWithOtherMaterial_SplitsIntoTwoChunks()
        {
            var chunks = ChunkPlanner.Plan(Group, new[] { true, true, true, true }, new[] { 0, 0, 5, 0 });

            Assert.AreEqual(2, chunks.Count);
            CollectionAssert.AreEqual(new[] { 0, 1, 3 }, chunks[0].Elements);
            Assert.AreEqual(5, chunks[1].MaterialId);
            CollectionAssert.AreEqual(new[] { 2 }, chunks[1].Elements);
        }

        [Test]
        public void Plan_AllHidden_ProducesNoChunks()
        {
            var chunks = ChunkPlanner.Plan(Group, new[] { false, false, false, false }, new[] { 0, 0, 0, 0 });

            CollectionAssert.IsEmpty(chunks);
        }

        [Test]
        public void Plan_OnlyLooksAtTheGroupsElements()
        {
            var visible = new[] { true, true, true, true, true, true };
            var materials = new[] { 9, 9, 1, 1, 9, 9 };

            var chunks = ChunkPlanner.Plan(new[] { 2, 3 }, visible, materials);

            Assert.AreEqual(1, chunks.Single().MaterialId);
            CollectionAssert.AreEqual(new[] { 2, 3 }, chunks.Single().Elements);
        }
    }
}
