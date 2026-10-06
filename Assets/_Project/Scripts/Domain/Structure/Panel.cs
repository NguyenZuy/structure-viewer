using System;
using System.Collections.Generic;
using UnityEngine;

namespace StructureViewer.Domain.Structure
{
    public sealed class Panel
    {
        private readonly Vector3[] _corners;

        // Four coplanar corners in Unity space (metres), in perimeter order. Thickness in metres.
        public Panel(IReadOnlyList<Vector3> corners, float thickness)
        {
            if (corners == null || corners.Count != 4)
                throw new ArgumentException("A panel needs exactly 4 corners.", nameof(corners));

            _corners = new Vector3[4];
            for (int i = 0; i < 4; i++)
                _corners[i] = corners[i];
            Thickness = thickness;
        }

        public IReadOnlyList<Vector3> Corners => _corners;
        public float Thickness { get; }

        // Planar quad area = half the magnitude of the diagonals' cross product (works for any winding).
        public float Area => 0.5f * Vector3.Cross(_corners[2] - _corners[0], _corners[3] - _corners[1]).magnitude;

        public Vector3 Normal => Vector3.Cross(_corners[1] - _corners[0], _corners[3] - _corners[0]).normalized;

        // Conservative: extrusion side depends on winding, so pad both sides.
        public Bounds ComputeBounds()
        {
            var offset = Normal * Thickness;
            var bounds = new Bounds(_corners[0], Vector3.zero);
            for (int i = 0; i < 4; i++)
            {
                bounds.Encapsulate(_corners[i] + offset);
                bounds.Encapsulate(_corners[i] - offset);
            }
            return bounds;
        }
    }
}
