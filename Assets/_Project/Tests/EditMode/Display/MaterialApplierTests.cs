using System.Linq;
using NUnit.Framework;
using StructureViewer.Application.Display;
using StructureViewer.Application.Events;
using StructureViewer.Domain.Display;
using StructureViewer.Domain.Selection;
using StructureViewer.Domain.Structure;
using StructureViewer.Presentation.Contracts;
using StructureViewer.Presentation.Display;
using StructureViewer.Tests.Fixtures;
using UnityEditor;
using UnityEngine;

namespace StructureViewer.Tests.EditMode.Display
{
    public sealed class MaterialApplierTests
    {
        private StructureModel _model;
        private EventBus _bus;
        private FakeStructureRenderer _renderer;
        private RenderingConfig _config;
        private DisplayPaletteAsset _palette;
        private DisplaySettings _settings;
        private MaterialApplier _applier;

        private int Stud => _model.IndexOf(TestStructures.VerticalStudId);

        [SetUp]
        public void SetUp()
        {
            _model = TestStructures.MiniHouse();
            _bus = new EventBus();
            _renderer = new FakeStructureRenderer(_model);
            _config = AssetDatabase.LoadAssetAtPath<RenderingConfig>(PaletteTests.ConfigPath);
            _palette = ScriptableObject.CreateInstance<DisplayPaletteAsset>();
            _settings = new DisplaySettings(_bus);
            _applier = new MaterialApplier(_renderer, _config, _palette, _settings, _bus);
            _bus.Publish(new StructureLoaded(_model));
        }

        [TearDown]
        public void TearDown()
        {
            _applier.Dispose();
            Object.DestroyImmediate(_palette);
        }

        [Test]
        public void Load_AppliesTheBaseMaterialToEveryElement()
        {
            for (int i = 0; i < _model.Elements.Count; i++)
                Assert.AreSame(_applier.BaseMaterial(_model.Elements[i]), _renderer.MaterialOf(i));
            Assert.AreSame(_config.Wood, _renderer.MaterialOf(Stud));
        }

        [Test]
        public void ModeSwitch_UpdatesMaterials_AndNeverTouchesVisibility()
        {
            _renderer.ClearCalls();

            _settings.SetMode(DisplayMode.Clay);

            Assert.AreEqual(_model.Elements.Count, _renderer.MaterialCalls.Count);
            Assert.IsEmpty(_renderer.VisibleCalls);
            Assert.AreNotSame(_config.Wood, _renderer.MaterialOf(Stud));
        }

        [Test]
        public void FieldSwitch_OutsideColorBy_ChangesNothing()
        {
            _renderer.ClearCalls();

            _settings.SetField(ColorByField.Level);

            Assert.IsEmpty(_renderer.MaterialCalls);
        }

        [Test]
        public void SelectionChange_TouchesOnlyAffectedIndices()
        {
            int joist = _model.IndexOf(TestStructures.JoistId);
            _bus.Publish(new SelectionChanged(new SelectionSnapshot(new[] { Stud }, SelectionKind.Member)));
            _renderer.ClearCalls();

            _bus.Publish(new SelectionChanged(new SelectionSnapshot(new[] { joist }, SelectionKind.Member)));

            CollectionAssert.AreEquivalent(new[] { Stud, joist }, _renderer.MaterialCalls.Select(c => c.Index));
            Assert.AreSame(_config.Wood, _renderer.MaterialOf(Stud));
            Assert.AreNotSame(_config.Wood, _renderer.MaterialOf(joist));
        }

        [Test]
        public void SelectionBeatsHover()
        {
            _bus.Publish(new SelectionChanged(new SelectionSnapshot(new[] { Stud }, SelectionKind.Member)));
            var selected = _renderer.MaterialOf(Stud);

            _bus.Publish(new HoverChanged(Stud));

            Assert.AreSame(selected, _renderer.MaterialOf(Stud));
        }

        [Test]
        public void HoverMovesBetweenElements_RestoresThePreviousOne()
        {
            int joist = _model.IndexOf(TestStructures.JoistId);

            _bus.Publish(new HoverChanged(Stud));
            Assert.AreNotSame(_config.Wood, _renderer.MaterialOf(Stud));

            _bus.Publish(new HoverChanged(joist));
            Assert.AreSame(_config.Wood, _renderer.MaterialOf(Stud));
            Assert.AreNotSame(_config.Wood, _renderer.MaterialOf(joist));
        }

        [Test]
        public void RendererNotBuiltYet_AppliesOnceItMatchesTheModel()
        {
            var bus = new EventBus();
            var empty = new FakeStructureRenderer(0);
            var applier = new MaterialApplier(empty, _config, _palette, new DisplaySettings(bus), bus);

            Assert.DoesNotThrow(() => bus.Publish(new StructureLoaded(_model)));
            Assert.DoesNotThrow(() => bus.Publish(new SelectionChanged(new SelectionSnapshot(new[] { Stud }, SelectionKind.Member))));
            Assert.IsEmpty(empty.MaterialCalls);
            applier.Dispose();
        }

        [Test]
        public void Dispose_StopsListeningAndDestroysClones()
        {
            _settings.SetMode(DisplayMode.XRay);
            var clone = _renderer.MaterialOf(Stud);
            _applier.Dispose();
            _renderer.ClearCalls();

            _settings.SetMode(DisplayMode.Clay);

            Assert.IsEmpty(_renderer.MaterialCalls);
            Assert.IsTrue(clone == null);
        }
    }
}
