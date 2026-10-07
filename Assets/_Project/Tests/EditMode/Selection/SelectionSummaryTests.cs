using NUnit.Framework;
using StructureViewer.Domain.Selection;
using StructureViewer.Tests.Fixtures;

namespace StructureViewer.Tests.EditMode.Selection
{
    public sealed class SelectionSummaryTests
    {
        [Test]
        public void Of_Members_SumsLengthAndGroupsByType()
        {
            var model = TestStructures.MiniHouse();

            var summary = SelectionSummary.Of(model, model.ElementsInGroup(TestStructures.JoistBay));

            Assert.AreEqual(2, summary.Count);
            Assert.AreEqual(2f * TestStructures.HouseDepth, summary.TotalLength, 1e-4f);
            Assert.AreEqual(0f, summary.TotalArea);
            Assert.AreEqual(1, summary.ByType.Count);
            Assert.AreEqual("Joist", summary.ByType[0].Type);
            Assert.AreEqual(2, summary.ByType[0].Count);
        }

        [Test]
        public void Of_PanelAndSlab_SumsAreaButNoLength()
        {
            var model = TestStructures.MiniHouse();
            int panel = model.IndexOf(TestStructures.RoofPanelId);
            int slab = model.IndexOf(TestStructures.SlabId);

            var summary = SelectionSummary.Of(model, new[] { panel, slab });

            Assert.AreEqual(0f, summary.TotalLength);
            Assert.AreEqual(model.Elements[panel].Panel.Area + model.Elements[slab].Slab.Area, summary.TotalArea, 1e-4f);
        }

        [Test]
        public void Of_Wall_ByTypeIsMostFrequentFirstThenByName()
        {
            var model = TestStructures.MiniHouse();

            var summary = SelectionSummary.Of(model, model.ElementsInGroup(TestStructures.NorthWall));

            Assert.AreEqual("Stud", summary.ByType[0].Type);
            Assert.AreEqual("TrimmerStud", summary.ByType[1].Type);
            for (int i = 1; i < summary.ByType.Count; i++)
                Assert.That(summary.ByType[i].Count, Is.LessThanOrEqualTo(summary.ByType[i - 1].Count));

            int total = 0;
            foreach (var type in summary.ByType)
                total += type.Count;
            Assert.AreEqual(summary.Count, total);
        }

        [Test]
        public void Of_Empty_IsZero()
        {
            var summary = SelectionSummary.Of(TestStructures.MiniHouse(), new int[0]);

            Assert.AreEqual(0, summary.Count);
            Assert.AreEqual(0f, summary.TotalLength);
            Assert.IsEmpty(summary.ByType);
        }
    }
}
