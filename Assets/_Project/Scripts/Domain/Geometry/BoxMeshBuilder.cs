using UnityEngine;

namespace StructureViewer.Domain.Geometry
{
    // Member box centred on the origin: X = width, Y = depth, Z = length. UVs are in metres with V along the
    // length, so a tiling wood texture follows each member's grain and never stretches.
    public static class BoxMeshBuilder
    {
        public static MeshData Build(float width, float depth, float length)
        {
            float x = width * 0.5f, y = depth * 0.5f, z = length * 0.5f;
            var b = new MeshDataBuilder();

            // Long faces: U across the face, V along the member.
            b.AddQuad(new Vector3(x, -y, -z), new Vector3(x, y, -z), new Vector3(x, y, z), new Vector3(x, -y, z),
                new Vector2(0f, 0f), new Vector2(depth, 0f), new Vector2(depth, length), new Vector2(0f, length), Vector3.right);
            b.AddQuad(new Vector3(-x, -y, -z), new Vector3(-x, y, -z), new Vector3(-x, y, z), new Vector3(-x, -y, z),
                new Vector2(0f, 0f), new Vector2(depth, 0f), new Vector2(depth, length), new Vector2(0f, length), Vector3.left);
            b.AddQuad(new Vector3(-x, y, -z), new Vector3(x, y, -z), new Vector3(x, y, z), new Vector3(-x, y, z),
                new Vector2(0f, 0f), new Vector2(width, 0f), new Vector2(width, length), new Vector2(0f, length), Vector3.up);
            b.AddQuad(new Vector3(-x, -y, -z), new Vector3(x, -y, -z), new Vector3(x, -y, z), new Vector3(-x, -y, z),
                new Vector2(0f, 0f), new Vector2(width, 0f), new Vector2(width, length), new Vector2(0f, length), Vector3.down);

            // End grain.
            b.AddQuad(new Vector3(-x, -y, z), new Vector3(x, -y, z), new Vector3(x, y, z), new Vector3(-x, y, z),
                new Vector2(0f, 0f), new Vector2(width, 0f), new Vector2(width, depth), new Vector2(0f, depth), Vector3.forward);
            b.AddQuad(new Vector3(-x, -y, -z), new Vector3(x, -y, -z), new Vector3(x, y, -z), new Vector3(-x, y, -z),
                new Vector2(0f, 0f), new Vector2(width, 0f), new Vector2(width, depth), new Vector2(0f, depth), Vector3.back);

            return b.ToMeshData();
        }
    }
}
