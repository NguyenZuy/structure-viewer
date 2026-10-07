using System.Linq;
using NUnit.Framework;
using StructureViewer.Domain.Geometry;
using UnityEngine;

namespace StructureViewer.Tests.EditMode.Geometry
{
    public sealed class MeshBuilderTests
    {
        private const float Eps = 1e-4f;

        [Test]
        public void Box_Has24VerticesAnd12Triangles()
        {
            var mesh = BoxMeshBuilder.Build(0.035f, 0.09f, 2.4f);

            Assert.AreEqual(24, mesh.Vertices.Length);
            Assert.AreEqual(36, mesh.Triangles.Length);
            Assert.AreEqual(24, mesh.Normals.Length);
            Assert.AreEqual(24, mesh.Uvs.Length);
        }

        [Test]
        public void Box_BoundsMatchSectionAndLength()
        {
            var mesh = BoxMeshBuilder.Build(0.035f, 0.09f, 2.4f);

            var min = mesh.Vertices.Aggregate(Vector3.Min);
            var max = mesh.Vertices.Aggregate(Vector3.Max);
            Assert.AreEqual(0.035f, max.x - min.x, Eps);
            Assert.AreEqual(0.09f, max.y - min.y, Eps);
            Assert.AreEqual(2.4f, max.z - min.z, Eps);
            Assert.AreEqual(0f, (min + max).magnitude, Eps, "box should be centred");
        }

        [Test]
        public void Box_UvVRangeIsLengthInMetres()
        {
            var mesh = BoxMeshBuilder.Build(0.035f, 0.09f, 2.4f);

            Assert.AreEqual(0f, mesh.Uvs.Min(uv => uv.y), Eps);
            Assert.AreEqual(2.4f, mesh.Uvs.Max(uv => uv.y), Eps);
        }

        [Test]
        public void Box_EveryTriangleFacesOutward()
        {
            AssertClosedAndOutward(BoxMeshBuilder.Build(0.035f, 0.09f, 2.4f), Vector3.zero);
        }

        [Test]
        public void Panel_RoofSlope_ExtrudesUpwardAndFacesOutward()
        {
            // Generator-style roof corners (model right-handed normal points up) after the Y/Z swap to Unity.
            var corners = new[]
            {
                new Vector3(0f, 5.4f, 0f), new Vector3(10f, 5.4f, 0f),
                new Vector3(10f, 7f, 4f), new Vector3(0f, 7f, 4f)
            };

            var normal = PanelMeshBuilder.ExtrusionNormal(corners);
            var mesh = PanelMeshBuilder.Build(corners, 0.012f);

            Assert.Greater(normal.y, 0f, "sheathing must sit on top of the rafters");
            Assert.AreEqual(24, mesh.Vertices.Length);
            Assert.AreEqual(36, mesh.Triangles.Length);
            var centre = mesh.Vertices.Aggregate(Vector3.zero, (a, v) => a + v) / mesh.Vertices.Length;
            AssertClosedAndOutward(mesh, centre);
        }

        [Test]
        public void Slab_TopAndBottomAtExpectedHeightsAndFacesOutward()
        {
            var outline = new[] { new Vector2(0f, 0f), new Vector2(10f, 0f), new Vector2(10f, 8f), new Vector2(0f, 8f) };

            var mesh = SlabMeshBuilder.Build(outline, 0f, 0.3f);

            Assert.AreEqual(0f, mesh.Vertices.Max(v => v.y), Eps);
            Assert.AreEqual(-0.3f, mesh.Vertices.Min(v => v.y), Eps);
            Assert.AreEqual(4 + 4 + 4 * 4, mesh.Vertices.Length);
            Assert.AreEqual((2 + 2 + 4 * 2) * 3, mesh.Triangles.Length);
            AssertClosedAndOutward(mesh, new Vector3(5f, -0.15f, 4f));
        }

        [Test]
        public void Combiner_AppendTwice_OffsetsIndicesAndTransformsVertices()
        {
            var box = BoxMeshBuilder.Build(0.1f, 0.2f, 1f);
            var target = new MeshDataBuilder();
            var pose = MemberGeometry.ComputePose(new Vector3(5f, 0f, 0f), new Vector3(5f, 0f, 1f), 0f);

            MeshCombiner.Append(target, box, MemberPose.Identity);
            MeshCombiner.Append(target, box, pose);

            Assert.AreEqual(48, target.Vertices.Count);
            Assert.AreEqual(72, target.Triangles.Count);
            Assert.IsTrue(target.Triangles.Skip(36).All(i => i >= 24 && i < 48), "second mesh indices must be offset");
            var second = target.Vertices.Skip(24).ToList();
            var centre = second.Aggregate(Vector3.zero, (a, v) => a + v) / second.Count;
            Assert.AreEqual(5f, centre.x, Eps);
            Assert.AreEqual(0.5f, centre.z, Eps);
            Assert.AreEqual(1f, target.Normals[30].magnitude, Eps);
        }

        // Convex meshes only: every triangle's winding normal and stored normals point away from an interior point.
        private static void AssertClosedAndOutward(MeshData mesh, Vector3 interior)
        {
            var t = mesh.Triangles;
            for (int i = 0; i < t.Length; i += 3)
            {
                var a = mesh.Vertices[t[i]];
                var b = mesh.Vertices[t[i + 1]];
                var c = mesh.Vertices[t[i + 2]];
                var winding = Vector3.Cross(b - a, c - a);
                var away = (a + b + c) / 3f - interior;
                Assert.Greater(Vector3.Dot(winding, away), 0f, $"triangle {i / 3} winds inward");
                Assert.Greater(Vector3.Dot(mesh.Normals[t[i]], away), 0f, $"triangle {i / 3} normal points inward");
            }
        }
    }
}
