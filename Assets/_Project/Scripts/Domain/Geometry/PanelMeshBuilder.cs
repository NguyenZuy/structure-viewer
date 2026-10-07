using System.Collections.Generic;
using UnityEngine;

namespace StructureViewer.Domain.Geometry
{
    // Quad panel extruded by its thickness, in world space. The extrusion side is the model file's right-handed
    // corner normal; after the Y/Z axis swap that is the negated Unity cross product.
    public static class PanelMeshBuilder
    {
        public static Vector3 ExtrusionNormal(IReadOnlyList<Vector3> corners) =>
            -Vector3.Cross(corners[1] - corners[0], corners[3] - corners[0]).normalized;

        public static MeshData Build(IReadOnlyList<Vector3> corners, float thickness)
        {
            var normal = ExtrusionNormal(corners);
            var offset = normal * thickness;
            var bottom = new Vector3[4];
            var top = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                bottom[i] = corners[i];
                top[i] = corners[i] + offset;
            }

            // Planar UVs in metres along the first two edges.
            var uAxis = (corners[1] - corners[0]).normalized;
            var vAxis = Vector3.Cross(normal, uAxis);
            var uvs = new Vector2[4];
            for (int i = 0; i < 4; i++)
            {
                var d = corners[i] - corners[0];
                uvs[i] = new Vector2(Vector3.Dot(d, uAxis), Vector3.Dot(d, vAxis));
            }

            var b = new MeshDataBuilder();
            b.AddQuad(top[0], top[1], top[2], top[3], uvs[0], uvs[1], uvs[2], uvs[3], normal);
            b.AddQuad(bottom[0], bottom[1], bottom[2], bottom[3], uvs[0], uvs[1], uvs[2], uvs[3], -normal);

            var centre = (corners[0] + corners[1] + corners[2] + corners[3]) * 0.25f + offset * 0.5f;
            for (int i = 0; i < 4; i++)
            {
                int j = (i + 1) % 4;
                float edge = Vector3.Distance(corners[i], corners[j]);
                var outward = (bottom[i] + bottom[j]) * 0.5f + offset * 0.5f - centre;
                b.AddQuad(bottom[i], bottom[j], top[j], top[i],
                    new Vector2(0f, 0f), new Vector2(edge, 0f), new Vector2(edge, thickness), new Vector2(0f, thickness), outward);
            }
            return b.ToMeshData();
        }
    }
}
