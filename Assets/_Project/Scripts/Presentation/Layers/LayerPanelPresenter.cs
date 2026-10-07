using System;
using System.Collections.Generic;
using StructureViewer.Application.Events;
using StructureViewer.Application.Loading;
using StructureViewer.Application.Visibility;
using StructureViewer.Domain.Structure;

namespace StructureViewer.Presentation.Layers
{
    public sealed class LayerPanelPresenter : IDisposable
    {
        private static readonly ElementCategory[] Categories = (ElementCategory[])Enum.GetValues(typeof(ElementCategory));

        private readonly ILayerPanelView _view;
        private readonly VisibilityActions _actions;
        private readonly VisibilityService _visibility;
        private readonly StructureSession _session;
        private readonly IDisposable _changed;

        private StructureModel _countedModel;
        private readonly int[] _categoryCounts = new int[Categories.Length];

        public LayerPanelPresenter(ILayerPanelView view, VisibilityActions actions, VisibilityService visibility,
            StructureSession session, EventBus bus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _actions = actions ?? throw new ArgumentNullException(nameof(actions));
            _visibility = visibility ?? throw new ArgumentNullException(nameof(visibility));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            if (bus == null)
                throw new ArgumentNullException(nameof(bus));

            _view.CategoryToggled += OnCategoryToggled;
            _view.LevelToggled += OnLevelToggled;
            _view.IsolateLevelClicked += OnIsolateLevel;
            _view.ShowAllClicked += OnShowAll;
            // A load also publishes VisibilityChanged, so this one subscription covers both.
            _changed = bus.Subscribe<VisibilityChanged>(_ => Render());
            Render();
        }

        public void Dispose()
        {
            _view.CategoryToggled -= OnCategoryToggled;
            _view.LevelToggled -= OnLevelToggled;
            _view.IsolateLevelClicked -= OnIsolateLevel;
            _view.ShowAllClicked -= OnShowAll;
            _changed.Dispose();
        }

        // A toggle that changed nothing (e.g. no model yet) re-renders so the control snaps back to the real state.
        private void OnCategoryToggled(int key, bool on)
        {
            if (!_actions.SetCategoryVisible((ElementCategory)key, on))
                Render();
        }

        private void OnLevelToggled(int levelIndex, bool on)
        {
            if (!_actions.SetLevelVisible(levelIndex, on))
                Render();
        }

        private void OnIsolateLevel(int levelIndex) => _actions.IsolateLevel(levelIndex);

        private void OnShowAll() => _actions.ShowAll();

        private void Render()
        {
            var state = _visibility.Current;
            if (state == null || !_session.HasModel)
            {
                _view.Render(LayerPanelContent.Empty);
                return;
            }

            var model = _session.Current;
            CountCategories(model);

            var categories = new List<LayerRow>();
            for (int i = 0; i < Categories.Length; i++)
            {
                if (_categoryCounts[i] > 0)
                    categories.Add(new LayerRow(i, CategoryLabel(Categories[i]), _categoryCounts[i], state.IsCategoryOn(Categories[i])));
            }

            var levels = new List<LayerRow>(model.Levels.Count);
            for (int i = 0; i < model.Levels.Count; i++)
                levels.Add(new LayerRow(i, model.Levels[i].Name, model.ElementsInLevel(i).Count, state.IsLevelOn(i)));

            _view.Render(new LayerPanelContent(categories, levels, state.IsIsolating || state.HasHidden));
        }

        private void CountCategories(StructureModel model)
        {
            if (ReferenceEquals(model, _countedModel))
                return;

            _countedModel = model;
            Array.Clear(_categoryCounts, 0, _categoryCounts.Length);
            for (int i = 0; i < model.Elements.Count; i++)
                _categoryCounts[(int)model.Elements[i].Info.Category]++;
        }

        private static string CategoryLabel(ElementCategory category) =>
            category switch
            {
                ElementCategory.Wall => "Walls",
                ElementCategory.Floor => "Floors",
                ElementCategory.Opening => "Doors & windows",
                _ => category.ToString()
            };
    }
}
