using System.Linq;
using NUnit.Framework;
using StructureViewer.Domain.Structure;
using StructureViewer.Domain.Takeoff;
using StructureViewer.Domain.Visibility;
using StructureViewer.Tests.Fixtures;

namespace StructureViewer.Tests.EditMode.Takeoff
{
    public sealed class TakeoffCalculatorTests
    {
        private StructureModel _model;

        [SetUp]
        public void SetUp() => _model = TestStructures.MiniHouse();

        private TakeoffTable All() => TakeoffCalculator.Calculate(_model, AllVisible.Instance, visibleOnly: false);

        private static TakeoffRow Row(TakeoffTable table, string type) => table.Rows.Single(r => r.Type == type);

        [Test]
        public void Members_GroupByTypeSectionAndMaterial()
        {
            var studs = Row(All(), "Stud");
            var stud = _model.Elements[_model.IndexOf(TestStructures.VerticalStudId)].Member;

            Assert.AreEqual(4, studs.Count);
            Assert.AreEqual("35 × 90", studs.Section);
            Assert.AreEqual("MGP10", studs.Material);
            Assert.AreEqual(4f * stud.Length, studs.Length, 1e-4f);
            Assert.AreEqual(4f * stud.Volume, studs.Volume, 1e-6f);
            Assert.AreEqual(0f, studs.Area);
        }

        [Test]
        public void SameTypeDifferentSection_IsTwoRows()
        {
            var info = new ElementInfo("A", ElementCategory.Floor, "Joist", "G", 0);
            var other = new ElementInfo("B", ElementCategory.Floor, "Joist", "G", 0);
            var model = new StructureModel("Two joists", new[] { new Level(0, "L0", "Ground", 0f) }, new[]
            {
                Element.ForMember(0, info, new Member(UnityEngine.Vector3.zero, UnityEngine.Vector3.right, 0f, new Section(0.045f, 0.24f), "MGP12")),
                Element.ForMember(1, other, new Member(UnityEngine.Vector3.zero, UnityEngine.Vector3.right, 0f, new Section(0.045f, 0.19f), "MGP12"))
            });

            var table = TakeoffCalculator.Calculate(model, AllVisible.Instance, false);

            Assert.AreEqual(2, table.Rows.Count);
        }

        [Test]
        public void Panels_ReportAreaNotLength()
        {
            var sheathing = Row(All(), "RoofSheathing");
            float area = _model.Elements.Where(e => e.Panel != null).Sum(e => e.Panel.Area);

            Assert.AreEqual(2, sheathing.Count);
            Assert.AreEqual("12 mm", sheathing.Section);
            Assert.IsNull(sheathing.Material);
            Assert.AreEqual(0f, sheathing.Length);
            Assert.AreEqual(area, sheathing.Area, 1e-4f);
            Assert.AreEqual(area * 0.012f, sheathing.Volume, 1e-5f);
        }

        [Test]
        public void Slab_ReportsVolumeAndArea()
        {
            var slab = Row(All(), "Slab");
            float expectedArea = TestStructures.WallLength * TestStructures.HouseDepth;

            Assert.AreEqual(expectedArea, slab.Area, 1e-4f);
            Assert.AreEqual(expectedArea * TestStructures.SlabThickness, slab.Volume, 1e-4f);
            Assert.AreEqual("100 mm", slab.Section);
        }

        [Test]
        public void Totals_AreTheSumOfRows()
        {
            var table = All();

            Assert.AreEqual(_model.Elements.Count, table.Totals.Count);
            Assert.AreEqual(table.Rows.Sum(r => r.Count), table.Totals.Count);
            Assert.AreEqual(table.Rows.Sum(r => r.Length), table.Totals.Length, 1e-3f);
            Assert.AreEqual(table.Rows.Sum(r => r.Volume), table.Totals.Volume, 1e-4f);
            Assert.AreEqual(table.Rows.Sum(r => r.Area), table.Totals.Area, 1e-3f);
        }

        [Test]
        public void Rows_SortedByCategoryThenType()
        {
            var rows = All().Rows;

            for (int i = 1; i < rows.Count; i++)
            {
                var a = rows[i - 1];
                var b = rows[i];
                Assert.That(a.Category < b.Category || (a.Category == b.Category && string.CompareOrdinal(a.Type, b.Type) <= 0),
                    $"{a.Type} before {b.Type}");
            }
        }

        [Test]
        public void VisibleOnly_ExcludesHiddenElements()
        {
            var joists = _model.ElementsInGroup(TestStructures.JoistBay).ToArray();
            var hidden = new FakeVisibility(joists).Hide(_model.IndexOf(TestStructures.VerticalStudId));

            var visible = TakeoffCalculator.Calculate(_model, hidden, visibleOnly: true);
            var all = TakeoffCalculator.Calculate(_model, hidden, visibleOnly: false);

            Assert.IsFalse(visible.Rows.Any(r => r.Type == "Joist"));
            Assert.AreEqual(3, Row(visible, "Stud").Count);
            Assert.AreEqual(_model.Elements.Count - joists.Length - 1, visible.Totals.Count);
            Assert.AreEqual(_model.Elements.Count, all.Totals.Count);
        }
    }
}
