using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StructureViewer.Application.Commands;
using StructureViewer.Application.Events;
using StructureViewer.Application.Loading;
using StructureViewer.Application.Visibility;
using StructureViewer.Domain.Structure;
using StructureViewer.Presentation.Layers;
using StructureViewer.Tests.Fixtures;

namespace StructureViewer.Tests.EditMode.Visibility
{
    public sealed class LayerPanelPresenterTests
    {
        private StructureModel _model;
        private EventBus _bus;
        private StructureSession _session;
        private VisibilityService _service;
        private VisibilityActions _actions;
        private FakeLayerPanelView _view;
        private LayerPanelPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _model = TestStructures.MiniHouse();
            _bus = new EventBus();
            _session = new StructureSession();
            _service = new VisibilityService(_bus);
            _actions = new VisibilityActions(_service, new CommandHistory(), _session, _bus);
            _view = new FakeLayerPanelView();
            _presenter = new LayerPanelPresenter(_view, _actions, _service, _session, _bus);
        }

        [TearDown]
        public void TearDown()
        {
            _presenter.Dispose();
            _actions.Dispose();
            _service.Dispose();
        }

        private void Load()
        {
            _session.Set(_model);
            _bus.Publish(new StructureLoaded(_model));
        }

        private static LayerRow Category(LayerPanelContent content, ElementCategory category) =>
            content.Categories.Single(r => r.Key == (int)category);

        [Test]
        public void BeforeLoad_RendersEmpty()
        {
            Assert.IsEmpty(_view.Last.Categories);
            Assert.IsEmpty(_view.Last.Levels);
        }

        [Test]
        public void Load_RendersCategoriesWithCountsAndLevels()
        {
            Load();

            var content = _view.Last;
            int walls = _model.Elements.Count(e => e.Info.Category == ElementCategory.Wall);
            Assert.AreEqual(walls, Category(content, ElementCategory.Wall).Count);
            Assert.AreEqual("Walls", Category(content, ElementCategory.Wall).Label);
            Assert.IsTrue(content.Categories.All(r => r.IsOn && r.Count > 0));
            CollectionAssert.AreEqual(new[] { "Ground Floor", "First Floor" }, content.Levels.Select(r => r.Label));
            Assert.AreEqual(_model.ElementsInLevel(0).Count, content.Levels[0].Count);
            Assert.IsFalse(content.IsFiltered);
        }

        [Test]
        public void CategoryToggled_ChangesVisibility_AndUndoIsReflected()
        {
            Load();

            _view.ToggleCategory(ElementCategory.Wall, false);
            Assert.IsFalse(_service.Current.IsCategoryOn(ElementCategory.Wall));
            Assert.IsFalse(Category(_view.Last, ElementCategory.Wall).IsOn);

            _actions.Undo();
            Assert.IsTrue(Category(_view.Last, ElementCategory.Wall).IsOn);
        }

        [Test]
        public void LevelToggled_ChangesVisibility()
        {
            Load();

            _view.ToggleLevel(TestStructures.FirstLevel, false);

            Assert.IsFalse(_view.Last.Levels[TestStructures.FirstLevel].IsOn);
        }

        [Test]
        public void ToggleThatChangesNothing_RendersAgainSoTheControlSnapsBack()
        {
            int renders = _view.Renders.Count;

            _view.ToggleCategory(ElementCategory.Wall, false);

            Assert.AreEqual(renders + 1, _view.Renders.Count);
        }

        [Test]
        public void IsolateLevel_ThenShowAll_TogglesFilteredState()
        {
            Load();

            _view.ClickIsolateLevel(TestStructures.GroundLevel);
            Assert.IsTrue(_view.Last.IsFiltered);

            _view.ClickShowAll();
            Assert.IsFalse(_view.Last.IsFiltered);
        }

        [Test]
        public void Dispose_UnsubscribesViewAndBus()
        {
            _presenter.Dispose();
            int renders = _view.Renders.Count;

            Load();

            Assert.IsFalse(_view.HasSubscribers);
            Assert.AreEqual(renders, _view.Renders.Count);
        }

        private sealed class FakeLayerPanelView : ILayerPanelView
        {
            public event Action<int, bool> CategoryToggled;
            public event Action<int, bool> LevelToggled;
            public event Action<int> IsolateLevelClicked;
            public event Action ShowAllClicked;

            public List<LayerPanelContent> Renders { get; } = new List<LayerPanelContent>();
            public LayerPanelContent Last => Renders[Renders.Count - 1];

            public bool HasSubscribers =>
                CategoryToggled != null || LevelToggled != null || IsolateLevelClicked != null || ShowAllClicked != null;

            public void Render(LayerPanelContent content) => Renders.Add(content);

            public void ToggleCategory(ElementCategory category, bool on) => CategoryToggled?.Invoke((int)category, on);
            public void ToggleLevel(int level, bool on) => LevelToggled?.Invoke(level, on);
            public void ClickIsolateLevel(int level) => IsolateLevelClicked?.Invoke(level);
            public void ClickShowAll() => ShowAllClicked?.Invoke();
        }
    }
}
