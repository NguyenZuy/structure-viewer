using System;
using NUnit.Framework;
using StructureViewer.Domain.Geometry;
using UnityEngine;

namespace StructureViewer.Tests.EditMode.Geometry
{
    public sealed class MemberGeometryTests
    {
        private const float Eps = 1e-4f;

        [Test]
        public void ComputePose_Horizontal_DepthIsVerticalAndPoseAtMidpoint()
        {
            var pose = MemberGeometry.ComputePose(new Vector3(0f, 1f, 0f), new Vector3(4f, 1f, 0f), 0f);

            AssertVector(new Vector3(2f, 1f, 0f), pose.Position);
            Assert.AreEqual(4f, pose.Length, Eps);
            AssertVector(Vector3.right, pose.Forward);
            AssertVector(Vector3.up, pose.Up);
            AssertOrthonormal(pose);
        }

        [Test]
        public void ComputePose_Sloped_UpStaysInVerticalPlaneAndPointsUp()
        {
            var pose = MemberGeometry.ComputePose(new Vector3(0f, 2.4f, 0f), new Vector3(0f, 3.4f, 1.5f), 0f);

            AssertVector(new Vector3(0f, 1f, 1.5f).normalized, pose.Forward);
            Assert.AreEqual(0f, pose.Right.y, Eps, "width axis should stay horizontal");
            Assert.Greater(pose.Up.y, 0f);
            AssertOrthonormal(pose);
        }

        [Test]
        public void ComputePose_VerticalStud_HasNoNaNAndHorizontalSectionAxes()
        {
            var pose = MemberGeometry.ComputePose(new Vector3(1f, 0f, 1f), new Vector3(1f, 2.4f, 1f), 0f);

            AssertVector(Vector3.up, pose.Forward);
            Assert.AreEqual(0f, pose.Right.y, Eps);
            Assert.AreEqual(0f, pose.Up.y, Eps);
            // Roll 0 keeps the section depth along world Z (model Y): right for walls running along model X.
            AssertVector(Vector3.forward, pose.Up);
            AssertOrthonormal(pose);
        }

        [Test]
        public void ComputePose_VerticalStudRoll90_DepthAlongWorldX()
        {
            var pose = MemberGeometry.ComputePose(Vector3.zero, new Vector3(0f, 2.4f, 0f), 90f);

            Assert.AreEqual(1f, Mathf.Abs(pose.Up.x), Eps);
            Assert.AreEqual(0f, pose.Up.y, Eps);
            AssertOrthonormal(pose);
        }

        [Test]
        public void ComputePose_Roll90_RotatesSectionAxesAboutTheMemberAxis()
        {
            var flat = MemberGeometry.ComputePose(Vector3.zero, new Vector3(3f, 0f, 0f), 0f);
            var rolled = MemberGeometry.ComputePose(Vector3.zero, new Vector3(3f, 0f, 0f), 90f);

            AssertVector(flat.Up, rolled.Right);
            AssertVector(-flat.Right, rolled.Up);
            AssertVector(flat.Forward, rolled.Forward);
        }

        [TestCase(0f, 0f, 0f, 4f, 0f, 0f, 0f)]
        [TestCase(0f, 0f, 0f, 0f, 3f, 0f, 0f)]
        [TestCase(0f, 0f, 0f, 0f, 3f, 0f, 90f)]
        [TestCase(0f, 2.4f, 0f, 0f, 3.4f, 1.5f, 0f)]
        [TestCase(1f, 0f, 2f, -2f, 1f, 5f, 33f)]
        public void Rotation_MatchesPoseAxes(float sx, float sy, float sz, float ex, float ey, float ez, float roll)
        {
            var pose = MemberGeometry.ComputePose(new Vector3(sx, sy, sz), new Vector3(ex, ey, ez), roll);

            var rotation = pose.Rotation;

            AssertVector(pose.Right, rotation * Vector3.right);
            AssertVector(pose.Up, rotation * Vector3.up);
            AssertVector(pose.Forward, rotation * Vector3.forward);
        }

        [Test]
        public void ComputePose_ZeroLength_Throws()
        {
            Assert.Throws<ArgumentException>(() => MemberGeometry.ComputePose(Vector3.one, Vector3.one, 0f));
        }

        private static void AssertOrthonormal(MemberPose pose)
        {
            Assert.AreEqual(1f, pose.Right.magnitude, Eps);
            Assert.AreEqual(1f, pose.Up.magnitude, Eps);
            Assert.AreEqual(0f, Vector3.Dot(pose.Right, pose.Up), Eps);
            Assert.AreEqual(0f, Vector3.Dot(pose.Right, pose.Forward), Eps);
            Assert.AreEqual(0f, Vector3.Dot(pose.Up, pose.Forward), Eps);
            // Left-handed basis like Unity's: right × up = forward.
            AssertVector(pose.Forward, Vector3.Cross(pose.Right, pose.Up));
        }

        private static void AssertVector(Vector3 expected, Vector3 actual)
        {
            Assert.IsFalse(float.IsNaN(actual.x) || float.IsNaN(actual.y) || float.IsNaN(actual.z), "NaN");
            Assert.AreEqual(expected.x, actual.x, Eps, $"x of {actual}");
            Assert.AreEqual(expected.y, actual.y, Eps, $"y of {actual}");
            Assert.AreEqual(expected.z, actual.z, Eps, $"z of {actual}");
        }
    }
}
