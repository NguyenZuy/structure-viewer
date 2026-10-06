using System;
using System.Collections.Generic;
using StructureViewer.Domain.Structure;
using StructureViewer.Presentation.Contracts;
using UnityEngine;

namespace StructureViewer.Tests.Fixtures
{
    public sealed class FakeStructureRenderer : IStructureRenderer
    {
        private readonly bool[] _visible;
        private readonly Material[] _materials;
        private readonly Bounds[] _bounds;

        public FakeStructureRenderer(int count)
        {
            _visible = new bool[count];
            _materials = new Material[count];
            _bounds = new Bounds[count];
            for (int i = 0; i < count; i++)
                _visible[i] = true;
        }

        // Bounds default to the model's element bounds.
        public FakeStructureRenderer(StructureModel model) : this(model.Elements.Count)
        {
            for (int i = 0; i < _bounds.Length; i++)
                _bounds[i] = model.Elements[i].Bounds;
            ModelBounds = model.Bounds;
        }

        public int Count => _visible.Length;
        public Bounds ModelBounds { get; set; }

        // Every call, in order, including no-op ones, so tests can assert "only changed indices touched".
        public List<(int Index, bool Visible)> VisibleCalls { get; } = new List<(int, bool)>();
        public List<(int Index, Material Material)> MaterialCalls { get; } = new List<(int, Material)>();
        public List<Vector2> PickRequests { get; } = new List<Vector2>();

        // Scripted pick: return a hit for the screen position, or null for empty space. Default: always miss.
        public Func<Vector2, PickHit?> PickHandler { get; set; } = _ => null;

        public bool IsVisible(int index) => _visible[index];
        public Material MaterialOf(int index) => _materials[index];

        public void SetVisible(int index, bool visible)
        {
            VisibleCalls.Add((index, visible));
            _visible[index] = visible;
        }

        public void SetMaterial(int index, Material material)
        {
            MaterialCalls.Add((index, material));
            _materials[index] = material;
        }

        public Bounds GetWorldBounds(int index) => _bounds[index];

        public void SetWorldBounds(int index, Bounds bounds) => _bounds[index] = bounds;

        public bool TryPick(Vector2 screenPosition, out PickHit hit)
        {
            PickRequests.Add(screenPosition);
            var result = PickHandler(screenPosition);
            hit = result ?? default;
            return result.HasValue;
        }

        public void PickAlways(int index, Vector3 point = default) =>
            PickHandler = _ => new PickHit(index, point);

        public void PickNothing() => PickHandler = _ => null;

        public void ClearCalls()
        {
            VisibleCalls.Clear();
            MaterialCalls.Clear();
            PickRequests.Clear();
        }
    }
}
