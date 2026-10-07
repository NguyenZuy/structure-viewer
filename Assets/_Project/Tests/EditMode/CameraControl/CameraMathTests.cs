using NUnit.Framework;
using StructureViewer.Presentation.CameraControl;
using UnityEngine;

namespace StructureViewer.Tests.EditMode.CameraControl
{
    public sealed class CameraMathTests
    {
        private const float Fov = 50f;

        // Long low house, like the sample: the wide axis decides the distance on a portrait phone.
        private static readonly Bounds House = new Bounds(new Vector3(2f, 3f, -1f), new Vector3(12f, 6f, 9f));

        private static OrbitState DefaultState(float distance = 20f) =>
            new OrbitState(House.center, CameraController.DefaultYaw, CameraController.DefaultPitch, distance, 0.3f, 500f);

        [TestCase(16f / 9f)]
        [TestCase(9f / 19.5f)]
        public void FitDistance_AllCornersInsideFrustum(float aspect)
        {
            var state = DefaultState();
            float distance = CameraFraming.FitDistance(House, state.Rotation, Fov, aspect);

            Assert.That(MaxFrustumRatio(state.WithFrame(House.center, distance), aspect), Is.LessThanOrEqualTo(1.0001f));
        }

        [TestCase(16f / 9f)]
        [TestCase(9f / 19.5f)]
        public void FitDistance_IsTight_TenPercentCloserClipsACorner(float aspect)
        {
            var state = DefaultState();
            float distance = CameraFraming.FitDistance(House, state.Rotation, Fov, aspect, margin: 1f);

            Assert.That(MaxFrustumRatio(state.WithFrame(House.center, distance * 0.9f), aspect), Is.GreaterThan(1f));
        }

        [Test]
        public void FitDistance_PortraitNeedsMoreDistanceThanLandscape()
        {
            var rotation = DefaultState().Rotation;

            Assert.That(CameraFraming.FitDistance(House, rotation, Fov, 9f / 19.5f),
                Is.GreaterThan(CameraFraming.FitDistance(House, rotation, Fov, 16f / 9f)));
        }

        [TestCase(1000f, OrbitState.MinPitch)]
        [TestCase(-1000f, OrbitState.MaxPitch)]
        public void Orbit_PitchIsClamped(float dragUp, float expected)
        {
            Assert.AreEqual(expected, DefaultState().Orbit(new Vector2(0f, dragUp)).Pitch, 1e-4f);
        }

        [Test]
        public void Orbit_KeepsPivotAndDistance()
        {
            var state = DefaultState();
            var orbited = state.Orbit(new Vector2(40f, 10f));

            Assert.AreEqual(state.Pivot, orbited.Pivot);
            Assert.AreEqual(state.Distance, Vector3.Distance(orbited.Position, orbited.Pivot), 1e-3f);
            Assert.That(Vector3.Distance(state.Position, orbited.Position), Is.GreaterThan(1f));
        }

        [Test]
        public void Rotation_MatchesYawThenPitchConvention()
        {
            // Yaw 90 looks along +X; pitch 90 looks straight down.
            var side = new OrbitState(Vector3.zero, 90f, OrbitState.MinPitch, 10f, 1f, 100f).Forward;
            var top = new OrbitState(Vector3.zero, 0f, OrbitState.MaxPitch, 10f, 1f, 100f).Forward;

            Assert.That(side.x, Is.GreaterThan(0.99f));
            Assert.That(top.y, Is.LessThan(-0.99f));
        }

        [TestCase(1000f, 0.3f)]
        [TestCase(0.0001f, 500f)]
        public void Zoom_DistanceIsClamped(float amount, float expected)
        {
            var state = DefaultState();

            Assert.AreEqual(expected, state.Zoom(amount, state.Forward).Distance, 1e-3f);
        }

        [Test]
        public void Zoom_TowardCentre_KeepsPivot()
        {
            var state = DefaultState();
            var zoomed = state.Zoom(2f, state.Forward);

            Assert.AreEqual(10f, zoomed.Distance, 1e-4f);
            Assert.That(Vector3.Distance(state.Pivot, zoomed.Pivot), Is.LessThan(1e-4f));
        }

        [Test]
        public void Zoom_TowardOffCentrePoint_MovesPivotTowardItAndKeepsItUnderThePointer()
        {
            var state = DefaultState();
            var direction = state.ViewportDirection(new Vector2(0.9f, 0.5f), Fov, 16f / 9f);
            // A point on the pointer ray, on the pivot's depth plane.
            var target = state.Position + direction * (state.Distance / Vector3.Dot(direction, state.Forward));

            var zoomed = state.Zoom(2f, direction);

            var right = state.Rotation * Vector3.right;
            Assert.That(Vector3.Dot(zoomed.Pivot - state.Pivot, right), Is.GreaterThan(0.5f));
            var toTarget = (target - zoomed.Position).normalized;
            Assert.That(Vector3.Dot(toTarget, direction), Is.GreaterThan(0.99999f));
        }

        [Test]
        public void Pan_DragRight_MovesPivotLeft()
        {
            var state = DefaultState();
            var panned = state.Pan(new Vector2(100f, 0f), 0.01f);

            var right = state.Rotation * Vector3.right;
            Assert.AreEqual(-1f, Vector3.Dot(panned.Pivot - state.Pivot, right), 1e-4f);
            Assert.AreEqual(state.Distance, panned.Distance);
        }

        [Test]
        public void Lerp_EndpointsMatchInputs()
        {
            var a = DefaultState(5f);
            var b = DefaultState(50f).Orbit(new Vector2(30f, 0f)).WithPivot(Vector3.one);

            var end = OrbitState.Lerp(a, b, 1f);

            Assert.AreEqual(b.Distance, end.Distance, 1e-3f);
            Assert.AreEqual(b.Yaw, end.Yaw, 1e-4f);
            Assert.AreEqual(b.Pivot, end.Pivot);
            Assert.AreEqual(a.Distance, OrbitState.Lerp(a, b, 0f).Distance, 1e-3f);
        }

        // Largest |x|/(z tanH) or |y|/(z tanV) over the bound corners; > 1 means a corner is outside.
        private static float MaxFrustumRatio(OrbitState state, float aspect)
        {
            float tanV = Mathf.Tan(Fov * 0.5f * Mathf.Deg2Rad);
            float tanH = tanV * aspect;
            var r = state.Rotation;
            var toCamera = new Quaternion(-r.x, -r.y, -r.z, r.w);
            float max = 0f;
            for (int i = 0; i < 8; i++)
            {
                var e = House.extents;
                var corner = House.center + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
                var p = toCamera * (corner - state.Position);
                Assert.That(p.z, Is.GreaterThan(0f), "corner behind the camera");
                max = Mathf.Max(max, Mathf.Abs(p.x) / (p.z * tanH), Mathf.Abs(p.y) / (p.z * tanV));
            }
            return max;
        }
    }
}
