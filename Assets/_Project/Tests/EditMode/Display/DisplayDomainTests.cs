using System.Linq;
using NUnit.Framework;
using StructureViewer.Domain.Display;
using StructureViewer.Domain.Selection;
using StructureViewer.Domain.Structure;
using StructureViewer.Tests.Fixtures;

namespace StructureViewer.Tests.EditMode.Display
{
    public sealed class DisplayDomainTests
    {
        private StructureModel _model;

        [SetUp]
        public void SetUp() => _model = TestStructures.MiniHouse();

        private Element ById(string id) => _model.Elements[_model.IndexOf(id)];

        [TestCase(SelectionKind.Member, true, true, HighlightState.Member)]
        [TestCase(SelectionKind.Multi, true, false, HighlightState.Member)]
        [TestCase(SelectionKind.Assembly, true, true, HighlightState.Assembly)]
        [TestCase(SelectionKind.Member, false, true, HighlightState.Hover)]
        [TestCase(SelectionKind.Member, false, false, HighlightState.None)]
        public void HighlightResolver_SelectionBeatsHoverBeatsNone(SelectionKind kind, bool selected, bool hovered, HighlightState expected)
        {
            const int index = 2;
            var selection = new SelectionSnapshot(selected ? new[] { index, 5 } : new[] { 5 }, kind);

            Assert.AreEqual(expected, HighlightResolver.Resolve(index, selection, hovered ? index : -1));
        }

        [Test]
        public void ColorKeyResolver_MemberPanelSlab_UnderEveryField()
        {
            var stud = ById(TestStructures.VerticalStudId);
            var panel = ById(TestStructures.RoofPanelId);
            var slab = ById(TestStructures.SlabId);

            Assert.AreEqual(new ColorKey(ColorByField.Category, (int)ElementCategory.Wall, "Wall"), ColorKeyResolver.KeyOf(stud, ColorByField.Category));
            Assert.AreEqual(new ColorKey(ColorByField.Category, (int)ElementCategory.Sheathing, "Sheathing"), ColorKeyResolver.KeyOf(panel, ColorByField.Category));
            Assert.AreEqual(new ColorKey(ColorByField.Category, (int)ElementCategory.Slab, "Slab"), ColorKeyResolver.KeyOf(slab, ColorByField.Category));

            Assert.AreEqual(new ColorKey(ColorByField.Type, -1, "Stud"), ColorKeyResolver.KeyOf(stud, ColorByField.Type));
            Assert.AreEqual(new ColorKey(ColorByField.Type, -1, "RoofSheathing"), ColorKeyResolver.KeyOf(panel, ColorByField.Type));
            Assert.AreEqual(new ColorKey(ColorByField.Type, -1, "Slab"), ColorKeyResolver.KeyOf(slab, ColorByField.Type));

            Assert.AreEqual(new ColorKey(ColorByField.Level, TestStructures.GroundLevel, null), ColorKeyResolver.KeyOf(stud, ColorByField.Level));
            Assert.AreEqual(new ColorKey(ColorByField.Level, TestStructures.FirstLevel, null), ColorKeyResolver.KeyOf(panel, ColorByField.Level));
        }

        [Test]
        public void LegendBuilder_Category_PresentKeysInOrdinalOrderWithCounts()
        {
            var rows = LegendBuilder.Build(_model, ColorByField.Category, null);

            CollectionAssert.AreEqual(new[] { "Wall", "Floor", "Roof", "Sheathing", "Slab" }, rows.Select(r => r.Label));
            Assert.AreEqual(_model.Elements.Count, rows.Sum(r => r.Count));
            Assert.AreEqual(_model.Elements.Count(e => e.Info.Category == ElementCategory.Wall), rows[0].Count);
        }

        [Test]
        public void LegendBuilder_Type_GroupedByCategoryThenName()
        {
            var rows = LegendBuilder.Build(_model, ColorByField.Type, null);

            for (int i = 1; i < rows.Count; i++)
            {
                var a = rows[i - 1];
                var b = rows[i];
                Assert.That(a.Category < b.Category || (a.Category == b.Category && string.CompareOrdinal(a.Label, b.Label) < 0),
                    $"{a.Label} before {b.Label}");
            }
            Assert.AreEqual(rows.Count, rows.Select(r => r.Label).Distinct().Count());
        }

        [Test]
        public void LegendBuilder_Level_UsesLevelNames()
        {
            var rows = LegendBuilder.Build(_model, ColorByField.Level, null);

            CollectionAssert.AreEqual(new[] { "Ground Floor", "First Floor" }, rows.Select(r => r.Label));
        }

        [Test]
        public void LegendBuilder_HiddenOnlyWhenEveryElementOfTheKeyIsHidden()
        {
            var joists = _model.ElementsInGroup(TestStructures.JoistBay).ToArray();
            var oneJoistHidden = new FakeVisibility(joists[0]);
            var allJoistsHidden = new FakeVisibility(joists);

            Assert.IsFalse(LegendBuilder.Build(_model, ColorByField.Type, oneJoistHidden).Single(r => r.Label == "Joist").IsHidden);
            Assert.IsTrue(LegendBuilder.Build(_model, ColorByField.Type, allJoistsHidden).Single(r => r.Label == "Joist").IsHidden);
        }
    }
}
