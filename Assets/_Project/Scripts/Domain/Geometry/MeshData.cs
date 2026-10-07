using UnityEngine;

namespace StructureViewer.Domain.Geometry
{
    // Engine-free mesh arrays; the renderer copies them into UnityEngine.Mesh.
    public sealed class MeshData
    {
        public MeshData(Vector3[] vertices, Vector3[] normals, Vector2[] uvs, int[] triangles)
        {
            Vertices = vertices;
            Normals = normals;
            Uvs = uvs;
            Triangles = triangles;
        }

        public Vector3[] Vertices { get; }
        public Vector3[] Normals { get; }
        public Vector2[] Uvs { get; }
        public int[] Triangles { get; }
    }
}
