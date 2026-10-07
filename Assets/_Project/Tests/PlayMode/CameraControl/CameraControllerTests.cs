using System.Collections;
using NUnit.Framework;
using StructureViewer.Presentation.CameraControl;
using StructureViewer.Tests.Fixtures;
using UnityEngine;
using UnityEngine.TestTools;

namespace StructureViewer.Tests.PlayMode.CameraControl
{
    public sealed class CameraControllerTests
    {
        private static readonly Bounds House = new Bounds(new Vector3(2f, 3f, -1f), new Vector3(12f, 6f, 9f));

        private GameObject _go;
        private CameraController _controller;
        private FakePointerEvents _pointer;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("Test Camera", typeof(Camera));
            _controller = _go.AddComponent<CameraController>();
            _pointer = new FakePointerEvents();
            _controller.Bind(_pointer);
        }

        [TearDown]
        public void TearDown() => Object.Destroy(_go);

        [UnityTest]
        public IEnumerator FitAll_EndsWithBoundsFullyVisible()
        {
            _controller.FitAll(House);
            yield return Settle();

            var e = House.extents;
            for (int i = 0; i < 8; i++)
            {
                var corner = House.center + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
                var v = _controller.Camera.WorldToViewportPoint(corner);
                Assert.That(v.z, Is.GreaterThan(0f));
                Assert.That(v.x, Is.InRange(-0.001f, 1.001f), $"corner {i}");
                Assert.That(v.y, Is.InRange(-0.001f, 1.001f), $"corner {i}");
            }
        }

        [UnityTest]
        public IEnumerator Orbit_RotatesCameraAroundPivot()
        {
            _controller.FitAll(House);
            yield return Settle();
            var before = _go.transform.position;
            float radius = Vector3.Distance(before, House.center);

            _pointer.RaiseOrbit(new Vector2(200f, 0f));
            yield return Settle();

            Assert.That(Vector3.Distance(before, _go.transform.position), Is.GreaterThan(1f));
            Assert.AreEqual(radius, Vector3.Distance(_go.transform.position, House.center), 0.01f);
            Assert.That(Vector3.Dot(_go.transform.forward, (House.center - _go.transform.position).normalized), Is.GreaterThan(0.9999f));
        }

        [UnityTest]
        public IEnumerator Destroy_UnsubscribesFromPointer()
        {
            Object.Destroy(_go);
            yield return null;

            Assert.IsFalse(_pointer.HasSubscribers);
        }

        private static IEnumerator Settle()
        {
            yield return new WaitForSecondsRealtime(1.2f);
        }
    }
}
