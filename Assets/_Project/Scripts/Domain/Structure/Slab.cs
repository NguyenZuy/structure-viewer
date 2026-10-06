using System;
using System.Collections.Generic;
using UnityEngine;

namespace StructureViewer.Domain.Structure
{
    public sealed class Slab
    {
        private readonly Vector2[] _outline;

        // Outline is a convex polygon on the Unity XZ plane (x, z in metres), extruded down from Top by Thickness.
        public Slab(IReadOnlyList<Vector2> outline, float top, float thickness)
        {
            if (outline == null || outline.Count < 3)
                throw new ArgumentException("A slab outline needs at least 3 points.", nameof(outline));

            _outline = new Vector2[outline.Count];
            for (int i = 0; i < outline.Count; i++)
                _outline[i] = outline[i];
            Top = top;
            Thickness = thickness;
        }

        public IReadOnlyList<Vector2> Outline => _outline;
        public float Top { get; }
        public float Thickness { get; }

        public float Area
        {
            get
            {
                float twiceArea = 0f;
                for (int i = 0; i < _outline.Length; i++)
                {
                    var a = _outline[i];
                    var b = _outline[(i + 1) % _outline.Length];
                    twiceArea += a.x * b.y - b.x * a.y;
                }
                return Mathf.Abs(twiceArea) * 0.5f;
            }
        }

        public float Volume => Area * Thickness;

        public Bounds ComputeBounds()
        {
            var bounds = new Bounds(new Vector3(_outline[0].x, Top, _outline[0].y), Vector3.zero);
            for (int i = 0; i < _outline.Length; i++)
            {
                bounds.Encapsulate(new Vector3(_outline[i].x, Top, _outline[i].y));
                bounds.Encapsulate(new Vector3(_outline[i].x, Top - Thickness, _outline[i].y));
            }
            return bounds;
        }
    }
}
