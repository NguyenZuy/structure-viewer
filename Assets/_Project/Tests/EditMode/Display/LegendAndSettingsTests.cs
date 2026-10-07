using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StructureViewer.Application.Display;
using StructureViewer.Application.Events;
using StructureViewer.Application.Loading;
using StructureViewer.Domain.Display;
using StructureViewer.Domain.Structure;
using StructureViewer.Presentation.Display;
using StructureViewer.Tests.Fixtures;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace StructureViewer.Tests.EditMode.Display
{
    public sealed class LegendAndSettingsTests
    {
        private const string LegendLayoutPath = "Assets/_Project/UI/Legend/Legend.uxml";

        private StructureModel _model;
        private EventBus _bus;
        private StructureSession _session;
        private DisplaySettings _settings;
        private DisplayPaletteAsset _palette;
        private FakeLegendView _view;
        private LegendPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _model = TestStructures.MiniHouse();
            _bus = new EventBus();
            _session = new StructureSession();
            _settings = new DisplaySettings(_bus);
            _palette = ScriptableObject.CreateInstance<DisplayPaletteAsset>();
            _view = new FakeLegendView();
            _presenter = new LegendPresenter(_view, _settings, _session, _palette, _bus);
            _session.Set(_model);
            _bus.Publish(new StructureLoaded(_model));
        }

        [TearDown]
        public void TearDown()
        {
            _presenter.Dispose();
            Object.DestroyImmediate(_palette);
        }

        [Test]
        public void Settings_PublishOnlyOnRealChange()
        {
            using var changes = new EventRecorder<DisplayModeChanged>(_bus);

            _settings.SetMode(DisplayMode.Realistic);
            _settings.SetField(ColorByField.Type);
            _settings.SetMode(DisplayMode.XRay);
            _settings.SetField(ColorByField.Level);

            Assert.AreEqual(2, changes.Count);
            Assert.AreEqual(DisplayMode.XRay, changes.Last.Mode);
            Assert.AreEqual(ColorByField.Level, changes.Last.Field);
        }

        [Test]
        public void Legend_HiddenOutsideColorBy()
        {
            Assert.IsFalse(_view.Last.IsVisible);

            _settings.SetMode(DisplayMode.Clay);

            Assert.IsFalse(_view.Last.IsVisible);
        }

        [Test]
        public void Legend_ColorBy_ShowsOneEntryPerKeyWithPaletteColours()
        {
            _settings.SetMode(DisplayMode.ColorBy);
            _settings.SetField(ColorByField.Category);

            var content = _view.Last;
            Assert.IsTrue(content.IsVisible);
            Assert.AreEqual("Color by Category", content.Title);
            Assert.AreEqual(5, content.Entries.Count);
            Assert.AreEqual(_palette.TypeColor("NotInTable", ElementCategory.Wall), content.Entries[0].Color);
        }

        [Test]
        public void Legend_VisibilityChange_GreysOutHiddenKeys()
        {
            _settings.SetMode(DisplayMode.ColorBy);
            var joists = _model.ElementsInGroup(TestStructures.JoistBay).ToArray();

            _bus.Publish(new VisibilityChanged(new FakeVisibility(joists)));

            Assert.IsTrue(_view.Last.Entries.Single(e => e.Label == "Joist").IsHidden);
        }

        [Test]
        public void TypeColorProvider_KnownTypeHasColour_UnknownIsNull()
        {
            var provider = new TypeColorProvider(_palette, _session);

            Assert.AreEqual(_palette.TypeColor("Stud", ElementCategory.Wall), provider.ColorOf("Stud"));
            Assert.IsNull(provider.ColorOf("NotInModel"));
        }

        [Test]
        public void LegendView_RendersRowsAndHidesOutsideColorBy()
        {
            var view = new LegendView(AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LegendLayoutPath));
            var entries = new[] { new LegendEntry(Color.red, "Stud", 3, false), new LegendEntry(Color.green, "Joist", 2, true) };

            view.Render(new LegendContent(true, "Color by Type", entries));
            Assert.AreEqual(2, view.Root.Q("legend-rows").childCount);
            Assert.IsTrue(view.Root.Q("legend-rows")[1].ClassListContains(LegendView.HiddenRowClass));

            view.Render(LegendContent.Hidden);
            Assert.AreEqual(DisplayStyle.None, view.Root.style.display.value);
        }

        private sealed class FakeLegendView : ILegendView
        {
            public List<LegendContent> Renders { get; } = new List<LegendContent>();
            public LegendContent Last => Renders[Renders.Count - 1];

            public void Render(LegendContent content) => Renders.Add(content);
        }
    }
}
