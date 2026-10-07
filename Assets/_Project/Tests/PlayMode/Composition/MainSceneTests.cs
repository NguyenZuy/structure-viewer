#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StructureViewer.Infrastructure.Parsing;
using StructureViewer.Presentation.CameraControl;
using StructureViewer.Presentation.Structure;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StructureViewer.Tests.PlayMode.Composition
{
    // Runs the real composition root. Any error logged during load fails the test (Unity Test Framework default).
    public sealed class MainSceneTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main.unity";
        private const string SamplePath = "Assets/_Project/Data/sample-house.json";

        private Scene _scene;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var load = EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Additive));
            yield return load;
            _scene = SceneManager.GetSceneByPath(ScenePath);
            // Start, build, then the FitAll animation.
            yield return new WaitForSecondsRealtime(1.5f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_scene.IsValid() && _scene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(_scene);
        }

        [Test]
        public void MainScene_BuildsEveryElementOfTheSample()
        {
            var model = new JsonStructureParser().Parse(File.ReadAllText(SamplePath)).Model;

            var renderer = Find<StructureRenderer>();

            Assert.AreEqual(model.Elements.Count, renderer.Count);
            Assert.That(renderer.ChunkRendererCount, Is.GreaterThan(0));
        }

        [Test]
        public void MainScene_CameraFramesTheWholeModel()
        {
            var bounds = Find<StructureRenderer>().ModelBounds;
            var camera = Find<CameraController>().Camera;

            var e = bounds.extents;
            for (int i = 0; i < 8; i++)
            {
                var corner = bounds.center + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
                var v = camera.WorldToViewportPoint(corner);
                Assert.That(v.z, Is.GreaterThan(0f), $"corner {i}");
                Assert.That(v.x, Is.InRange(-0.01f, 1.01f), $"corner {i}");
                Assert.That(v.y, Is.InRange(-0.01f, 1.01f), $"corner {i}");
            }
        }

        private T Find<T>() where T : Component =>
            _scene.GetRootGameObjects().Select(go => go.GetComponentInChildren<T>()).Single(c => c != null);
    }
}
#endif
