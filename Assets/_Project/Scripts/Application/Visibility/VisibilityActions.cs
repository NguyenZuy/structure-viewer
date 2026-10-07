using System;
using System.Collections.Generic;
using StructureViewer.Application.Commands;
using StructureViewer.Application.Events;
using StructureViewer.Application.Loading;
using StructureViewer.Domain.Selection;
using StructureViewer.Domain.Structure;
using StructureViewer.Domain.Visibility;

namespace StructureViewer.Application.Visibility
{
    // The undoable visibility API for panels, toolbar and shortcuts. Each method returns false when nothing changed
    // (no model, empty selection, already in that state); no-ops are never recorded in the history.
    public sealed class VisibilityActions : IDisposable
    {
        private readonly VisibilityService _service;
        private readonly CommandHistory _history;
        private readonly StructureSession _session;
        private readonly IDisposable _selectionChanged;

        private SelectionSnapshot _selection = SelectionSnapshot.Empty;

        public VisibilityActions(VisibilityService service, CommandHistory history, StructureSession session, EventBus bus)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _history = history ?? throw new ArgumentNullException(nameof(history));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            if (bus == null)
                throw new ArgumentNullException(nameof(bus));
            _selectionChanged = bus.Subscribe<SelectionChanged>(evt => _selection = evt.Selection);
        }

        public bool HasSelection => !_selection.IsEmpty;

        public bool SetCategoryVisible(ElementCategory category, bool on) =>
            Apply(state => state.WithCategory(category, on));

        public bool SetLevelVisible(int levelIndex, bool on) =>
            Apply(state => IsLevel(levelIndex) ? state.WithLevel(levelIndex, on) : state);

        public bool IsolateSelection() => Isolate(_selection.Indices);

        // Isolating a level also switches it on: the user asked to see it.
        public bool IsolateLevel(int levelIndex) =>
            IsLevel(levelIndex) && Apply(state => state.WithLevel(levelIndex, true).WithIsolated(_session.Current.ElementsInLevel(levelIndex)));

        public bool IsolateGroup(string group) =>
            _session.HasModel && Isolate(_session.Current.ElementsInGroup(group));

        public bool HideSelection() =>
            !_selection.IsEmpty && Apply(state => state.WithHidden(_selection.Indices));

        public bool ShowAll() => Apply(state => state.ShowAll());

        public void Undo() => _history.Undo();

        public void Redo() => _history.Redo();

        public void Dispose() => _selectionChanged.Dispose();

        private bool Isolate(IReadOnlyList<int> indices) =>
            indices.Count > 0 && Apply(state => state.WithIsolated(indices));

        private bool IsLevel(int levelIndex) =>
            _session.HasModel && levelIndex >= 0 && levelIndex < _session.Current.Levels.Count;

        private bool Apply(Func<VisibilityState, VisibilityState> change)
        {
            var before = _service.Current;
            if (before == null)
                return false;

            var after = change(before);
            if (after.Equals(before))
                return false;

            _history.Execute(new VisibilityCommand(_service, before, after));
            return true;
        }
    }
}
