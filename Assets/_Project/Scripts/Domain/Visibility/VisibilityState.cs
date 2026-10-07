using System;
using System.Collections.Generic;
using StructureViewer.Domain.Structure;

namespace StructureViewer.Domain.Visibility
{
    // Immutable: every With* returns a new state (or this when nothing changes), so undo can just keep the old instance.
    // visible(e) = categoryOn[e.category] && levelOn[e.level] && (not isolating || e in isolated) && e not hidden
    public sealed class VisibilityState : IVisibility, IEquatable<VisibilityState>
    {
        private static readonly int CategoryCount = Enum.GetValues(typeof(ElementCategory)).Length;

        private readonly StructureModel _model;
        private readonly bool[] _categoryOn;
        private readonly bool[] _levelOn;

        // Null when not isolating / nothing hidden, so equality never has to tell "empty" from "absent".
        private readonly bool[] _isolated;
        private readonly bool[] _hidden;

        private VisibilityState(StructureModel model, bool[] categoryOn, bool[] levelOn, bool[] isolated, bool[] hidden)
        {
            _model = model;
            _categoryOn = categoryOn;
            _levelOn = levelOn;
            _isolated = isolated;
            _hidden = hidden;
        }

        public bool IsIsolating => _isolated != null;
        public bool HasHidden => _hidden != null;

        public static VisibilityState AllVisible(StructureModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            return new VisibilityState(model, Filled(CategoryCount, true), Filled(model.Levels.Count, true), null, null);
        }

        public bool IsVisible(int index)
        {
            var info = _model.Elements[index].Info;
            return _categoryOn[(int)info.Category]
                   && _levelOn[info.LevelIndex]
                   && (_isolated == null || _isolated[index])
                   && (_hidden == null || !_hidden[index]);
        }

        public bool IsCategoryOn(ElementCategory category) => _categoryOn[(int)category];

        public bool IsLevelOn(int levelIndex) => _levelOn[levelIndex];

        public VisibilityState WithCategory(ElementCategory category, bool on)
        {
            if (_categoryOn[(int)category] == on)
                return this;

            var categories = (bool[])_categoryOn.Clone();
            categories[(int)category] = on;
            return new VisibilityState(_model, categories, _levelOn, _isolated, _hidden);
        }

        public VisibilityState WithLevel(int levelIndex, bool on)
        {
            if (_levelOn[levelIndex] == on)
                return this;

            var levels = (bool[])_levelOn.Clone();
            levels[levelIndex] = on;
            return new VisibilityState(_model, _categoryOn, levels, _isolated, _hidden);
        }

        // Replaces any previous isolation; an empty set ends isolation. Hidden elements stay hidden.
        public VisibilityState WithIsolated(IReadOnlyList<int> indices)
        {
            if (indices == null)
                throw new ArgumentNullException(nameof(indices));

            bool[] isolated = null;
            if (indices.Count > 0)
            {
                isolated = new bool[_model.Elements.Count];
                for (int i = 0; i < indices.Count; i++)
                    isolated[indices[i]] = true;
            }

            return SameFlags(isolated, _isolated)
                ? this
                : new VisibilityState(_model, _categoryOn, _levelOn, isolated, _hidden);
        }

        // Adds to the hidden set.
        public VisibilityState WithHidden(IReadOnlyList<int> indices)
        {
            if (indices == null)
                throw new ArgumentNullException(nameof(indices));

            bool[] hidden = null;
            for (int i = 0; i < indices.Count; i++)
            {
                int index = indices[i];
                if ((_hidden != null && _hidden[index]) || (hidden != null && hidden[index]))
                    continue;
                hidden ??= _hidden != null ? (bool[])_hidden.Clone() : new bool[_model.Elements.Count];
                hidden[index] = true;
            }

            return hidden == null ? this : new VisibilityState(_model, _categoryOn, _levelOn, _isolated, hidden);
        }

        public VisibilityState ShowAll()
        {
            var all = AllVisible(_model);
            return Equals(all) ? this : all;
        }

        public bool Equals(VisibilityState other) =>
            other != null
            && ReferenceEquals(_model, other._model)
            && SameFlags(_categoryOn, other._categoryOn)
            && SameFlags(_levelOn, other._levelOn)
            && SameFlags(_isolated, other._isolated)
            && SameFlags(_hidden, other._hidden);

        public override bool Equals(object obj) => obj is VisibilityState other && Equals(other);

        public override int GetHashCode()
        {
            int hash = Hash(_categoryOn, 17);
            hash = Hash(_levelOn, hash);
            hash = Hash(_isolated, hash);
            return Hash(_hidden, hash);
        }

        private static bool SameFlags(bool[] a, bool[] b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (a == null || b == null || a.Length != b.Length)
                return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                    return false;
            }
            return true;
        }

        private static int Hash(bool[] flags, int hash)
        {
            if (flags == null)
                return hash * 31;
            for (int i = 0; i < flags.Length; i++)
                hash = hash * 31 + (flags[i] ? 1 : 0);
            return hash;
        }

        private static bool[] Filled(int length, bool value)
        {
            var flags = new bool[length];
            for (int i = 0; i < length; i++)
                flags[i] = value;
            return flags;
        }
    }
}
