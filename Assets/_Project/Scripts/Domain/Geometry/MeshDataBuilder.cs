using System.Collections.Generic;
using UnityEngine;

namespace StructureViewer.Domain.Geometry
{
    // Reusable vertex/index lists. Faces get flat normals and are wound so that the outward hint is the front side
    // (Unity treats clockwise-from-the-front, i.e. Cross(b - a, c - a) pointing at the viewer, as front-facing).
    public sealed class MeshDataBuilder
    {
        public List<Vector3> Vertices { get; } = new List<Vector3>();
        public List<Vector3> Normals { get; } = new List<Vector3>();
        public List<Vector2> Uvs { get; } = new List<Vector2>();
        public List<int> Triangles { get; } = new List<int>();

        public void Clear()
        {
            Vertices.Clear();
            Normals.Clear();
            Uvs.Clear();
            Triangles.Clear();
        }

        // a → b → c → d around the perimeter, either winding.
        public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud, Vector3 outward)
        {
            var normal = Vector3.Cross(b - a, c - a).normalized;
            bool flip = Vector3.Dot(normal, outward) < 0f;
            if (flip)
                normal = -normal;

            int i = Vertices.Count;
            Add(a, normal, ua);
            Add(b, normal, ub);
            Add(c, normal, uc);
            Add(d, normal, ud);
            if (flip)
                AddTriangles(i, i + 2, i + 1, i, i + 3, i + 2);
            else
                AddTriangles(i, i + 1, i + 2, i, i + 2, i + 3);
        }

        // Convex polygon as a fan.
        public void AddPolygon(IReadOnlyList<Vector3> points, IReadOnlyList<Vector2> uvs, Vector3 outward)
        {
            var normal = Vector3.Cross(points[1] - points[0], points[2] - points[0]).normalized;
            bool flip = Vector3.Dot(normal, outward) < 0f;
            if (flip)
                normal = -normal;

            int i = Vertices.Count;
            for (int p = 0; p < points.Count; p++)
                Add(points[p], normal, uvs[p]);
            for (int p = 1; p + 1 < points.Count; p++)
            {
                if (flip)
                    AddTriangles(i, i + p + 1, i + p);
                else
                    AddTriangles(i, i + p, i + p + 1);
            }
        }

        public MeshData ToMeshData() =>
            new MeshData(Vertices.ToArray(), Normals.ToArray(), Uvs.ToArray(), Triangles.ToArray());

        private void Add(Vector3 position, Vector3 normal, Vector2 uv)
        {
            Vertices.Add(position);
            Normals.Add(normal);
            Uvs.Add(uv);
        }

        private void AddTriangles(params int[] indices) => Triangles.AddRange(indices);
    }
}
