using NUnit.Framework;
using StructureViewer.Domain.Display;
using StructureViewer.Domain.Structure;
using StructureViewer.Presentation.Contracts;
using StructureViewer.Presentation.Display;
using StructureViewer.Tests.Fixtures;
using UnityEditor;
using UnityEngine;

namespace StructureViewer.Tests.EditMode.Display
{
    public sealed class PaletteTests
    {
        public const string ConfigPath = "Assets/_Project/Data/RenderingConfig.asset";

        private StructureModel _model;
        private RenderingConfig _config;
        private DisplayPaletteAsset _palette;
        private MaterialLibrary _library;

        [SetUp]
        public void SetUp()
        {
            _model = TestStructures.MiniHouse();
            _config = AssetDatabase.LoadAssetAtPath<RenderingConfig>(ConfigPath);
            _palette = ScriptableObject.CreateInstance<DisplayPaletteAsset>();
            _library = new MaterialLibrary(_config);
        }

        [TearDown]
        public void TearDown()
        {
            _library.Dispose();
            Object.DestroyImmediate(_palette);
        }

        private Element ById(string id) => _model.Elements[_model.IndexOf(id)];

        private static bool IsOpaque(Material material) => material.renderQueue < 2500;

        [Test]
        public void Realistic_None_UsesTemplates_HighlightTintsAClone()
        {
            var realistic = new RealisticPalette(_library, _palette);
            var stud = ById(TestStructures.VerticalStudId);

            Assert.AreSame(_config.Wood, realistic.Resolve(stud, HighlightState.None));
            Assert.AreSame(_config.Concrete, realistic.Resolve(ById(TestStructures.SlabId), HighlightState.None));
            Assert.AreSame(_config.Sheathing, realistic.Resolve(ById(TestStructures.RoofPanelId), HighlightState.None));

            var member = realistic.Resolve(stud, HighlightState.Member);
            Assert.AreNotSame(_config.Wood, member);
            Assert.AreSame(_config.Wood.mainTexture, member.mainTexture);
            Assert.AreNotSame(member, realistic.Resolve(stud, HighlightState.Assembly));
        }

        [Test]
        public void XRay_GhostsAreTranslucent_HighlightsAreOpaque()
        {
            var xRay = new XRayPalette(_library, _palette);
            var stud = ById(TestStructures.VerticalStudId);
            var panel = ById(TestStructures.RoofPanelId);

            Assert.IsFalse(IsOpaque(xRay.Resolve(stud, HighlightState.None)));
            Assert.IsTrue(IsOpaque(xRay.Resolve(stud, HighlightState.Member)));
            Assert.IsTrue(IsOpaque(xRay.Resolve(panel, HighlightState.Assembly)));
        }

        [Test]
        public void ColorBy_SameKeySameInstance_FieldChangeDifferentMaterial()
        {
            var colorBy = new ColorByPalette(_library, _palette) { Field = ColorByField.Type };
            var stud = ById(TestStructures.VerticalStudId);
            var otherStud = ById("W-L0-N-ST02");

            var byType = colorBy.Resolve(stud, HighlightState.None);
            Assert.AreSame(byType, colorBy.Resolve(otherStud, HighlightState.None));

            colorBy.Field = ColorByField.Category;
            Assert.AreNotSame(byType, colorBy.Resolve(stud, HighlightState.None));
        }

        [Test]
        public void ColorBy_And_Clay_PanelsStayTranslucentEvenWhenHighlighted()
        {
            var panel = ById(TestStructures.RoofPanelId);
            var colorBy = new ColorByPalette(_library, _palette);
            var clay = new ClayPalette(_library, _palette);

            Assert.IsFalse(IsOpaque(colorBy.Resolve(panel, HighlightState.Member)));
            Assert.IsFalse(IsOpaque(clay.Resolve(panel, HighlightState.None)));
            Assert.IsTrue(IsOpaque(clay.Resolve(ById(TestStructures.SlabId), HighlightState.None)));
        }

        [Test]
        public void Library_Dispose_DestroysClones()
        {
            var clone = _library.Flat(Color.red);

            _library.Dispose();

            Assert.IsTrue(clone == null);
            Assert.AreEqual(0, _library.Count);
        }

        [Test]
        public void Palette_UnknownTypeFallsBackToCategoryColour()
        {
            Assert.AreEqual(
                _palette.ColorOf(new ColorKey(ColorByField.Category, (int)ElementCategory.Floor, "Floor"), ElementCategory.Floor),
                _palette.TypeColor("MadeUpType", ElementCategory.Floor));
        }
    }
}
