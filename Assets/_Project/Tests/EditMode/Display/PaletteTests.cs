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

        // Colours read back from a material differ from what was set in the last float bits.
        private static void AssertColor(Color expected, Color actual)
        {
            Assert.AreEqual(expected.r, actual.r, 1e-4f, "r");
            Assert.AreEqual(expected.g, actual.g, 1e-4f, "g");
            Assert.AreEqual(expected.b, actual.b, 1e-4f, "b");
            Assert.AreEqual(expected.a, actual.a, 1e-4f, "a");
        }

        [Test]
        public void Realistic_None_UsesTemplates_HighlightTintsAClone()
        {
            var realistic = new RealisticPalette(_library, _palette);
            var stud = ById(TestStructures.VerticalStudId);

            Assert.AreSame(_config.Wood, realistic.Resolve(stud, HighlightState.None));
            Assert.AreSame(_config.Concrete, realistic.Resolve(ById(TestStructures.SlabId), HighlightState.None));

            var member = realistic.Resolve(stud, HighlightState.Member);
            Assert.AreNotSame(_config.Wood, member);
            Assert.AreSame(_config.Wood.mainTexture, member.mainTexture);
            Assert.AreNotSame(member, realistic.Resolve(stud, HighlightState.Assembly));
        }

        [Test]
        public void Realistic_Sheathing_IsTranslucentPerTypeToneAndKeepsItsAlphaWhenHighlighted()
        {
            var realistic = new RealisticPalette(_library, _palette);
            var panel = ById(TestStructures.RoofPanelId);

            var normal = realistic.Resolve(panel, HighlightState.None);
            var selected = realistic.Resolve(panel, HighlightState.Assembly);

            Assert.IsFalse(IsOpaque(normal));
            AssertColor(_palette.RealisticRoofSheathing, normal.GetColor("_BaseColor"));
            Assert.AreEqual(_palette.RealisticRoofSheathing.a, selected.GetColor("_BaseColor").a, 1e-5f);
            Assert.AreNotSame(normal, selected);
        }

        [Test]
        public void Realistic_PanelTone_DependsOnType()
        {
            Assert.AreEqual(_palette.RealisticWallSheathing, _palette.RealisticPanelFor("WallSheathing"));
            Assert.AreEqual(_palette.RealisticRoofSheathing, _palette.RealisticPanelFor("RoofSheathing"));
            Assert.AreEqual(_palette.RealisticSheathing, _palette.RealisticPanelFor("FloorSheathing"));
            Assert.AreEqual(_palette.RealisticDoor, _palette.RealisticPanelFor("Door"));
            Assert.AreEqual(_palette.RealisticGlass, _palette.RealisticPanelFor("Window"));
            Assert.AreNotEqual(_palette.RealisticWallSheathing, _palette.RealisticRoofSheathing);
        }

        [Test]
        public void Realistic_Glazing_IsTranslucent_DoorIsSolid()
        {
            Assert.Less(_palette.RealisticGlass.a, 1f);
            Assert.AreEqual(1f, _palette.RealisticDoor.a);
        }

        [Test]
        public void Realistic_Window_UsesTheGlossyGlassTemplate()
        {
            var info = new ElementInfo("W-WN01", ElementCategory.Opening, "Window", "W", 0);
            var corners = new[] { Vector3.zero, Vector3.right, new Vector3(1f, 1f, 0f), Vector3.up };
            var window = Element.ForPanel(0, info, new Panel(corners, 0.006f));

            var material = new RealisticPalette(_library, _palette).Resolve(window, HighlightState.None);

            Assert.AreEqual(_config.Glass.GetFloat("_Smoothness"), material.GetFloat("_Smoothness"));
            AssertColor(_palette.RealisticGlass, material.GetColor("_BaseColor"));
            Assert.IsFalse(IsOpaque(material));
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
