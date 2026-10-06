using NUnit.Framework;
using StructureViewer.Domain.Structure;
using UnityEngine;

namespace StructureViewer.Tests.EditMode.Structure
{
    public sealed class ElementGeometryTests
    {
        private const float Eps = 1e-5f;
        private static readonly Section Stud = new Section(0.035f, 0.09f);

        [Test]
        public void MemberBounds_VerticalStud_AxialExtentIsExactlyTheEndpoints()
        {
            var member = new Member(new Vector3(1f, 0f, 2f), new Vector3(1f, 2.4f, 2f), 0f, Stud, "MGP10");

            var bounds = member.ComputeBounds();

            Assert.AreEqual(0f, bounds.min.y, Eps);
            Assert.AreEqual(2.4f, bounds.max.y, Eps);
            float halfDiagonal = 0.5f * Mathf.Sqrt(Stud.Width * Stud.Width + Stud.Depth * Stud.Depth);
            Assert.AreEqual(halfDiagonal, bounds.extents.x, Eps);
            Assert.AreEqual(halfDiagonal, bounds.extents.z, Eps);
        }

        [Test]
        public void MemberBounds_ContainsAllBoxCornersForAnyRoll([Values(0f, 30f, 90f)] float roll)
        {
            var start = new Vector3(0f, 2.4f, 0f);
            var end = new Vector3(0f, 3.4f, 1.5f);
            var member = new Member(start, end, roll, Stud, "MGP10");
            var rotation = Quaternion.LookRotation(end - start, Vector3.up) * Quaternion.AngleAxis(roll, Vector3.forward);

            var bounds = member.ComputeBounds();
            bounds.Expand(Eps);

            foreach (var origin in new[] { start, end })
            {
                for (int sx = -1; sx <= 1; sx += 2)
                for (int sy = -1; sy <= 1; sy += 2)
                {
                    var corner = origin + rotation * new Vector3(sx * Stud.Width * 0.5f, sy * Stud.Depth * 0.5f, 0f);
                    Assert.IsTrue(bounds.Contains(corner), $"corner {corner} outside {bounds}");
                }
            }
        }

        [Test]
        public void MemberLength_SlopedChord_IsEuclideanDistance()
        {
            var member = new Member(new Vector3(0f, 2.4f, 0f), new Vector3(0f, 3.4f, 1.5f), 0f, Stud, "MGP10");

            Assert.AreEqual(Mathf.Sqrt(1f + 2.25f), member.Length, Eps);
        }

        [Test]
        public void PanelArea_SlopedRectangle_IsWidthTimesSlopeLength()
        {
            var panel = new Panel(new[]
            {
                new Vector3(0f, 2.4f, 0f), new Vector3(3.6f, 2.4f, 0f),
                new Vector3(3.6f, 3.4f, 1.5f), new Vector3(0f, 3.4f, 1.5f)
            }, 0.012f);

            Assert.AreEqual(3.6f * Mathf.Sqrt(1f + 2.25f), panel.Area, 1e-4f);
        }

        [Test]
        public void SlabAreaAndBounds_Rectangle_MatchOutlineAndThickness()
        {
            var slab = new Slab(new[] { new Vector2(0f, 0f), new Vector2(10f, 0f), new Vector2(10f, 8f), new Vector2(0f, 8f) }, 0f, 0.3f);

            var bounds = slab.ComputeBounds();

            Assert.AreEqual(80f, slab.Area, Eps);
            Assert.AreEqual(24f, slab.Volume, 1e-4f);
            Assert.AreEqual(new Vector3(0f, -0.3f, 0f), bounds.min);
            Assert.AreEqual(new Vector3(10f, 0f, 8f), bounds.max);
        }

        [Test]
        public void SectionLabel_Metres_FormatsAsMillimetres()
        {
            Assert.AreEqual("35 × 90", Stud.Label);
        }
    }
}
