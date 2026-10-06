using System;
using System.Collections.Generic;
using UnityEngine;

namespace StructureViewer.Domain.Structure
{
    // Immutable. Elements are addressed by their int index everywhere in the app.
    public sealed class StructureModel
    {
        private static readonly IReadOnlyList<int> NoIndices = Array.Empty<int>();

        private readonly Level[] _levels;
        private readonly Element[] _elements;
        private readonly List<string> _groups = new List<string>();
        private readonly Dictionary<string, List<int>> _byGroup = new Dictionary<string, List<int>>();
        private readonly List<int>[] _byLevel;
        private readonly Dictionary<string, int> _byId = new Dictionary<string, int>();

        public StructureModel(string name, IReadOnlyList<Level> levels, IReadOnlyList<Element> elements)
        {
            Name = name;
            _levels = Copy(levels ?? throw new ArgumentNullException(nameof(levels)));
            _elements = Copy(elements ?? throw new ArgumentNullException(nameof(elements)));

            for (int i = 0; i < _levels.Length; i++)
            {
                if (_levels[i].Index != i)
                    throw new ArgumentException($"Level '{_levels[i].Id}' has index {_levels[i].Index}, expected {i}.", nameof(levels));
            }

            _byLevel = new List<int>[_levels.Length];
            for (int i = 0; i < _byLevel.Length; i++)
                _byLevel[i] = new List<int>();

            var bounds = new Bounds();
            for (int i = 0; i < _elements.Length; i++)
            {
                var element = _elements[i];
                var info = element.Info;
                if (element.Index != i)
                    throw new ArgumentException($"Element '{info.Id}' has index {element.Index}, expected {i}.", nameof(elements));
                if (info.LevelIndex < 0 || info.LevelIndex >= _levels.Length)
                    throw new ArgumentException($"Element '{info.Id}' references missing level {info.LevelIndex}.", nameof(elements));
                if (_byId.ContainsKey(info.Id))
                    throw new ArgumentException($"Duplicate element id '{info.Id}'.", nameof(elements));

                _byId.Add(info.Id, i);
                _byLevel[info.LevelIndex].Add(i);

                if (!string.IsNullOrEmpty(info.Group))
                {
                    if (!_byGroup.TryGetValue(info.Group, out var members))
                    {
                        members = new List<int>();
                        _byGroup.Add(info.Group, members);
                        _groups.Add(info.Group);
                    }
                    members.Add(i);
                }

                if (i == 0)
                    bounds = element.Bounds;
                else
                    bounds.Encapsulate(element.Bounds);
            }
            Bounds = bounds;
        }

        public string Name { get; }
        public IReadOnlyList<Level> Levels => _levels;
        public IReadOnlyList<Element> Elements => _elements;

        // Union of all element bounds; zero-size at the origin for an empty model.
        public Bounds Bounds { get; }

        // In order of first appearance, so UI lists are stable across loads.
        public IReadOnlyList<string> Groups => _groups;

        public IReadOnlyList<int> ElementsInGroup(string group) =>
            group != null && _byGroup.TryGetValue(group, out var indices) ? indices : NoIndices;

        public IReadOnlyList<int> ElementsInLevel(int levelIndex) =>
            levelIndex >= 0 && levelIndex < _byLevel.Length ? _byLevel[levelIndex] : NoIndices;

        public int IndexOf(string elementId) =>
            elementId != null && _byId.TryGetValue(elementId, out int index) ? index : -1;

        private static T[] Copy<T>(IReadOnlyList<T> source)
        {
            var copy = new T[source.Count];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = source[i];
            return copy;
        }
    }
}
