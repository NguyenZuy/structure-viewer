#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using StructureViewer.Bootstrap;
using StructureViewer.Domain.Display;
using StructureViewer.Domain.Measure;
using StructureViewer.Domain.Selection;
using StructureViewer.Domain.Structure;
using StructureViewer.Presentation.CameraControl;
using StructureViewer.Presentation.Input;
using StructureViewer.Presentation.Structure;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StructureViewer.Tests.PlayMode.Composition
{
    // The real Main scene and sample: taps go through the same gesture classifier the mouse uses.
    public sealed class FeatureWiringTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main.unity";

        private Scene _scene;
        private AppBootstrap _app;
        private StructureRenderer _renderer;
        private PointerInput _pointer;
        private float _time = 1000f;

        private StructureModel Model => _app.Session.Current;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Additive));
            _scene = SceneManager.GetSceneByPath(ScenePath);
            // Start, build, then the FitAll animation settles.
            yield return new WaitForSecondsRealtime(1.5f);
            _app = Find<AppBootstrap>();
            _renderer = Find<StructureRenderer>();
            _pointer = Find<PointerInput>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_scene.IsValid() && _scene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(_scene);
        }

        [Test]
        public void Tap_SelectsMemberWithHighlight_DoubleTapSelectsItsAssembly()
        {
            var (position, index) = FindVisibleMember();
            string group = Model.Elements[index].Info.Group;
            var groupIndices = Model.ElementsInGroup(group).ToArray();
            var before = groupIndices.Select(_renderer.MaterialOf).ToArray();

            Tap(position);

            Assert.AreEqual(SelectionKind.Member, _app.Selection.Current.Kind);
            CollectionAssert.AreEqual(new[] { index }, _app.Selection.Current.Indices);
            Assert.AreNotSame(before[System.Array.IndexOf(groupIndices, index)], _renderer.MaterialOf(index));

            Tap(position);

            Assert.AreEqual(SelectionKind.Assembly, _app.Selection.Current.Kind);
            CollectionAssert.AreEquivalent(groupIndices, _app.Selection.Current.Indices);
            for (int i = 0; i < groupIndices.Length; i++)
                Assert.AreNotSame(before[i], _renderer.MaterialOf(groupIndices[i]), Model.Elements[groupIndices[i]].Info.Id);
        }

        [Test]
        public void WallCategoryOff_HidesWalls_DropsHiddenSelection_UndoRestores()
        {
            var walls = Enumerable.Range(0, Model.Elements.Count).Where(i => Model.Elements[i].Info.Category == ElementCategory.Wall).ToArray();
            int chunksBefore = _renderer.ChunkRendererCount;
            _app.Selection.Select(walls[0]);

            Assert.IsTrue(_app.Visibility.SetCategoryVisible(ElementCategory.Wall, false));
            _renderer.RebuildDirtyGroups();

            Assert.IsTrue(walls.All(i => !_renderer.IsVisible(i)));
            Assert.That(_renderer.ChunkRendererCount, Is.LessThan(chunksBefore));
            Assert.IsTrue(_app.Selection.Current.IsEmpty);

            _app.Actions.Undo();
            _renderer.RebuildDirtyGroups();

            Assert.IsTrue(walls.All(_renderer.IsVisible));
            Assert.AreEqual(chunksBefore, _renderer.ChunkRendererCount);
        }

        [UnityTest]
        public IEnumerator CyclingDisplayModes_KeepsHiddenSelectionAndCamera()
        {
            var roof = Enumerable.Range(0, Model.Elements.Count).Where(i => Model.Elements[i].Info.Category == ElementCategory.Roof).ToArray();
            var wallGroups = Model.Groups.Where(g => Model.Elements[Model.ElementsInGroup(g)[0]].Info.Category == ElementCategory.Wall).Take(2).ToArray();
            int selected = Model.ElementsInGroup(wallGroups[0])[0];
            int unselected = Model.ElementsInGroup(wallGroups[1])[0];
            _app.Visibility.SetCategoryVisible(ElementCategory.Roof, false);
            _app.Selection.SelectAssembly(selected);
            var camera = Find<CameraController>().transform;
            var position = camera.position;
            var rotation = camera.rotation;

            foreach (var mode in new[] { DisplayMode.ColorBy, DisplayMode.XRay, DisplayMode.Clay, DisplayMode.Realistic })
            {
                _app.Display.SetMode(mode);
                yield return null;

                Assert.IsTrue(roof.All(i => !_renderer.IsVisible(i)), $"{mode}: roof stays hidden");
                Assert.AreNotSame(_renderer.MaterialOf(unselected), _renderer.MaterialOf(selected), $"{mode}: selection highlighted");
                Assert.AreEqual(SelectionKind.Assembly, _app.Selection.Current.Kind, $"{mode}: selection kept");
                Assert.That(Vector3.Distance(position, camera.position), Is.LessThan(1e-4f), $"{mode}: camera position");
                Assert.That(Quaternion.Angle(rotation, camera.rotation), Is.LessThan(1e-3f), $"{mode}: camera rotation");
            }
        }

        [Test]
        public void MeasureMode_TapsMeasureInsteadOfSelecting()
        {
            var (position, _) = FindVisibleMember();
            _app.Actions.ToggleMeasure();

            Tap(position);

            Assert.IsTrue(_app.Selection.Current.IsEmpty);
            Assert.AreEqual(MeasureState.AwaitingB, _app.Measure.State);
        }

        // A screen point whose pick hits a grouped member (not sheathing or slab).
        private (Vector2 Position, int Index) FindVisibleMember()
        {
            for (float y = 0.3f; y <= 0.7f; y += 0.02f)
            {
                for (float x = 0.3f; x <= 0.7f; x += 0.02f)
                {
                    var position = new Vector2(x * Screen.width, y * Screen.height);
                    if (_renderer.TryPick(position, out var hit)
                        && Model.Elements[hit.Index].Kind == ElementKind.Member
                        && !string.IsNullOrEmpty(Model.Elements[hit.Index].Info.Group))
                        return (position, hit.Index);
                }
            }
            Assert.Fail("No member under the central screen area.");
            return default;
        }

        // Consecutive taps at the same spot are 0.1 s apart, inside the double-tap window.
        private void Tap(Vector2 position)
        {
            _time += 0.1f;
            _pointer.Classifier.MouseDown(MouseButton.Left, position);
            _pointer.Classifier.MouseUp(MouseButton.Left, position, additive: false, _time);
        }

        private T Find<T>() where T : Component =>
            _scene.GetRootGameObjects().Select(go => go.GetComponentInChildren<T>()).Single(c => c != null);
    }
}
#endif
