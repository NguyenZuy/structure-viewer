using System.Linq;
using NUnit.Framework;
using StructureViewer.Application.Loading;
using StructureViewer.Domain.Structure;
using StructureViewer.Infrastructure.Parsing;
using UnityEngine;

namespace StructureViewer.Tests.EditMode.Parsing
{
    public sealed class StructureDtoConverterTests
    {
        private const float Eps = 1e-5f;

        private StructureDto _dto;

        [SetUp]
        public void SetUp() => _dto = SampleDto.Create();

        [Test]
        public void Convert_ValidFile_ProducesAllElements()
        {
            var result = StructureDtoConverter.Convert(_dto);

            Assert.IsTrue(result.Success, string.Join("\n", result.Errors));
            Assert.AreEqual("Sample House", result.Model.Name);
            Assert.AreEqual(3, result.Model.Levels.Count);
            Assert.AreEqual(4, result.Model.Elements.Count);
        }

        [Test]
        public void Convert_MemberPoints_ConvertedFromMillimetresZUpToMetresYUp()
        {
            var model = StructureDtoConverter.Convert(_dto).Model;

            var member = model.Elements[model.IndexOf("T03-TC1")].Member;
            AssertVector(new Vector3(0f, 5.4f, 3.6f), member.Start);
            AssertVector(new Vector3(4.2f, 7.0f, 3.6f), member.End);
            Assert.AreEqual(0.035f, member.Section.Width, Eps);
            Assert.AreEqual(0.09f, member.Section.Depth, Eps);
        }

        [Test]
        public void Convert_Member_KeepsInfoAndLevelIndex()
        {
            var model = StructureDtoConverter.Convert(_dto).Model;

            var info = model.Elements[model.IndexOf("T03-TC1")].Info;
            Assert.AreEqual(ElementCategory.Roof, info.Category);
            Assert.AreEqual("TrussTopChord", info.Type);
            Assert.AreEqual("T03", info.Group);
            Assert.AreEqual(2, info.LevelIndex);
            Assert.AreEqual("MGP10", model.Elements[model.IndexOf("T03-TC1")].Member.Material);
        }

        [Test]
        public void Convert_LevelElevation_ConvertedToMetres()
        {
            var model = StructureDtoConverter.Convert(_dto).Model;

            Assert.AreEqual(2.7f, model.Levels[1].Elevation, Eps);
            Assert.AreEqual("First Floor", model.Levels[1].Name);
        }

        [Test]
        public void Convert_Slab_OutlineOnUnityXZAndOwnGroup()
        {
            var model = StructureDtoConverter.Convert(_dto).Model;

            var element = model.Elements[model.IndexOf("SLAB-1")];
            Assert.AreEqual(ElementKind.Slab, element.Kind);
            Assert.AreEqual(ElementCategory.Slab, element.Info.Category);
            Assert.AreEqual("SLAB-1", element.Info.Group);
            Assert.AreEqual(new Vector2(10f, 8f), element.Slab.Outline[2]);
            Assert.AreEqual(0.3f, element.Slab.Thickness, Eps);
        }

        [Test]
        public void Convert_Panel_CornersConvertedAndAreaPreserved()
        {
            var model = StructureDtoConverter.Convert(_dto).Model;

            var panel = model.Elements[model.IndexOf("RS-N")].Panel;
            AssertVector(new Vector3(10f, 7.0f, 4f), panel.Corners[2]);
            Assert.AreEqual(0.012f, panel.Thickness, Eps);
            Assert.AreEqual(10f * Mathf.Sqrt(16f + 2.56f), panel.Area, 1e-3f);
        }

        [Test]
        public void Convert_MissingRollAndGroup_DefaultToZeroAndNoGroup()
        {
            _dto.members[0].roll = 0f;
            _dto.members[0].group = null;

            var model = StructureDtoConverter.Convert(_dto).Model;

            var element = model.Elements[model.IndexOf("T03-TC1")];
            Assert.AreEqual(0f, element.Member.Roll);
            Assert.AreEqual(string.Empty, element.Info.Group);
        }

        [Test]
        public void Convert_NullDto_FailsWithError()
        {
            var result = StructureDtoConverter.Convert(null);

            Assert.IsFalse(result.Success);
            Assert.IsNull(result.Model);
            Assert.IsNotEmpty(result.Errors);
        }

        [Test]
        public void Convert_WrongUnitsAndAxis_ReportsBoth()
        {
            _dto.units = "m";
            _dto.upAxis = "Y";

            var result = StructureDtoConverter.Convert(_dto);

            AssertError(result, "units");
            AssertError(result, "upAxis");
        }

        [Test]
        public void Convert_NoLevels_Fails()
        {
            _dto.levels = null;

            AssertError(StructureDtoConverter.Convert(_dto), "No levels");
        }

        [Test]
        public void Convert_DuplicateLevelId_Fails()
        {
            _dto.levels[1].id = "L0";

            AssertError(StructureDtoConverter.Convert(_dto), "Level 'L0'", "duplicate");
        }

        [Test]
        public void Convert_DuplicateIdAcrossKinds_Fails()
        {
            _dto.panels[0].id = "T03-TC1";

            AssertError(StructureDtoConverter.Convert(_dto), "Panel 'T03-TC1'", "duplicate");
        }

        [Test]
        public void Convert_UnknownLevel_Fails()
        {
            _dto.members[0].level = "L9";

            AssertError(StructureDtoConverter.Convert(_dto), "T03-TC1", "unknown level 'L9'");
        }

        [Test]
        public void Convert_UnknownCategory_Fails()
        {
            _dto.panels[0].category = "Door";

            AssertError(StructureDtoConverter.Convert(_dto), "RS-N", "unknown category 'Door'");
        }

        [Test]
        public void Convert_MissingType_Fails()
        {
            _dto.members[0].type = "";

            AssertError(StructureDtoConverter.Convert(_dto), "T03-TC1", "missing type");
        }

        [Test]
        public void Convert_MissingId_ReportsPosition()
        {
            _dto.members[1].id = null;

            AssertError(StructureDtoConverter.Convert(_dto), "Member #2", "no id");
        }

        [TestCase(0f, 90f)]
        [TestCase(35f, -1f)]
        public void Convert_NonPositiveSection_Fails(float width, float depth)
        {
            _dto.members[0].section = new SectionDto { width = width, depth = depth };

            AssertError(StructureDtoConverter.Convert(_dto), "T03-TC1", "section");
        }

        [Test]
        public void Convert_MissingSection_Fails()
        {
            _dto.members[0].section = null;

            AssertError(StructureDtoConverter.Convert(_dto), "T03-TC1", "section");
        }

        [Test]
        public void Convert_ZeroLengthMember_Fails()
        {
            _dto.members[0].end = new[] { 0f, 3600f, 5400f };

            AssertError(StructureDtoConverter.Convert(_dto), "T03-TC1", "zero length");
        }

        [Test]
        public void Convert_PointWithWrongArity_Fails()
        {
            _dto.members[0].start = new[] { 0f, 3600f };

            AssertError(StructureDtoConverter.Convert(_dto), "T03-TC1", "start");
        }

        [Test]
        public void Convert_PanelWithWrongCornerCount_Fails()
        {
            _dto.panels[0].corners = new[] { 0f, 0f, 5400f, 10000f, 0f, 5400f, 10000f, 4000f, 7000f };

            AssertError(StructureDtoConverter.Convert(_dto), "RS-N", "4 points");
        }

        [Test]
        public void Convert_NonPlanarPanel_Fails()
        {
            _dto.panels[0].corners[11] = 7100f;

            AssertError(StructureDtoConverter.Convert(_dto), "RS-N", "coplanar");
        }

        [Test]
        public void Convert_PanelWithinPlanarTolerance_Succeeds()
        {
            _dto.panels[0].corners[11] = 7000.5f;

            Assert.IsTrue(StructureDtoConverter.Convert(_dto).Success);
        }

        [Test]
        public void Convert_DegeneratePanel_Fails()
        {
            var c = _dto.panels[0].corners;
            c[3] = c[0];
            c[4] = c[1];
            c[5] = c[2];

            AssertError(StructureDtoConverter.Convert(_dto), "RS-N", "degenerate");
        }

        [Test]
        public void Convert_SlabWithTwoPoints_Fails()
        {
            _dto.slabs[0].outline = new[] { 0f, 0f, 10000f, 0f };

            AssertError(StructureDtoConverter.Convert(_dto), "SLAB-1", "outline");
        }

        [Test]
        public void Convert_NonConvexSlab_Fails()
        {
            _dto.slabs[0].outline = new[] { 0f, 0f, 10000f, 0f, 5000f, 2000f, 10000f, 8000f, 0f, 8000f };

            AssertError(StructureDtoConverter.Convert(_dto), "SLAB-1", "convex");
        }

        [Test]
        public void Convert_SlabWithZeroThickness_Fails()
        {
            _dto.slabs[0].thickness = 0f;

            AssertError(StructureDtoConverter.Convert(_dto), "SLAB-1", "thickness");
        }

        [Test]
        public void Convert_SeveralBadElements_ReportsEveryOne()
        {
            _dto.members[0].level = "nope";
            _dto.members[1].end = _dto.members[1].start;
            _dto.slabs[0].thickness = -1f;

            var result = StructureDtoConverter.Convert(_dto);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(3, result.Errors.Count, string.Join("\n", result.Errors));
            Assert.IsTrue(result.Errors.Any(e => e.Contains("T03-TC1")));
            Assert.IsTrue(result.Errors.Any(e => e.Contains("W-L0-N-ST01")));
            Assert.IsTrue(result.Errors.Any(e => e.Contains("SLAB-1")));
        }

        private static void AssertError(ParseResult result, params string[] fragments)
        {
            Assert.IsFalse(result.Success, "Expected failure");
            Assert.IsNull(result.Model);
            bool found = result.Errors.Any(e => fragments.All(e.Contains));
            Assert.IsTrue(found, $"No error containing [{string.Join(", ", fragments)}] in:\n{string.Join("\n", result.Errors)}");
        }

        private static void AssertVector(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, Eps, "x");
            Assert.AreEqual(expected.y, actual.y, Eps, "y");
            Assert.AreEqual(expected.z, actual.z, Eps, "z");
        }
    }
}
