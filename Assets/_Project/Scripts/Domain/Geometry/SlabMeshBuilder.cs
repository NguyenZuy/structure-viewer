using System.Collections.Generic;
using UnityEngine;

namespace StructureViewer.Domain.Geometry
{
    // Convex plan outline (Unity x, z) extruded down from Top, in world space. UVs in metres on the plan.
    public static class SlabMeshBuilder
    {
        public static MeshData Build(IReadOnlyList<Vector2> outline, float top, float thickness)
        {
            int n = outline.Count;
            float bottomY = top - thickness;
            var topPoints = new Vector3[n];
            var bottomPoints = new Vector3[n];
            var uvs = new Vector2[n];
            var centre = Vector3.zero;
            for (int i = 0; i < n; i++)
            {
                topPoints[i] = new Vector3(outline[i].x, top, outline[i].y);
                bottomPoints[i] = new Vector3(outline[i].x, bottomY, outline[i].y);
                uvs[i] = outline[i];
                centre += topPoints[i];
            }
            centre = centre / n + Vector3.down * (thickness * 0.5f);

            var b = new MeshDataBuilder();
            b.AddPolygon(topPoints, uvs, Vector3.up);
            b.AddPolygon(bottomPoints, uvs, Vector3.down);
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                float edge = Vector2.Distance(outline[i], outline[j]);
                var outward = (bottomPoints[i] + bottomPoints[j]) * 0.5f + Vector3.up * (thickness * 0.5f) - centre;
                b.AddQuad(bottomPoints[i], bottomPoints[j], topPoints[j], topPoints[i],
                    new Vector2(0f, 0f), new Vector2(edge, 0f), new Vector2(edge, thickness), new Vector2(0f, thickness), outward);
            }
            return b.ToMeshData();
        }
    }
}
