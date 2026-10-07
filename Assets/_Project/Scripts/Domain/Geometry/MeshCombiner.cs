namespace StructureViewer.Domain.Geometry
{
    public static class MeshCombiner
    {
        // Appends source transformed by pose (positions and normals); UVs are kept so texture scale stays in metres.
        public static void Append(MeshDataBuilder target, MeshData source, in MemberPose pose)
        {
            int offset = target.Vertices.Count;
            var vertices = source.Vertices;
            var normals = source.Normals;
            var uvs = source.Uvs;
            for (int i = 0; i < vertices.Length; i++)
            {
                target.Vertices.Add(pose.TransformPoint(vertices[i]));
                target.Normals.Add(pose.TransformDirection(normals[i]));
                target.Uvs.Add(uvs[i]);
            }

            var triangles = source.Triangles;
            for (int i = 0; i < triangles.Length; i++)
                target.Triangles.Add(triangles[i] + offset);
        }
    }
}
