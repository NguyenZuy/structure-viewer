using System.Linq;
using NUnit.Framework;
using StructureViewer.Bootstrap;
using StructureViewer.Editor.Setup;
using StructureViewer.Presentation.Input;
using StructureViewer.Presentation.Contracts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StructureViewer.Tests.EditMode.Setup
{
    public sealed class SetupToolTests
    {
        private string _root;
        private SetupPaths _paths;
        private Scene _scene;

        [SetUp]
        public void SetUp()
        {
            // Unique per test: re-creating an asset at a just-deleted path in the same session can hand back a stale GUID.
            _root = $"Assets/SetupToolTests_{System.Guid.NewGuid():N}";
            _paths = new SetupPaths($"{_root}/Materials", $"{_root}/Grid.png", $"{_root}/RenderingConfig.asset", $"{_root}/PanelSettings.asset");
            // The test runner restores the previous scene setup after the run.
            _scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [TearDown]
        public void TearDown()
        {
            // Close any scene saved under _root before deleting it.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(_root);
        }

        [Test]
        public void AssetSetup_RunTwice_CreatesNoExtraAssets()
        {
            AssetSetup.Run(_paths);
            int first = AssetDatabase.FindAssets("", new[] { _root }).Length;

            AssetSetup.Run(_paths);

            Assert.That(first, Is.GreaterThan(0));
            Assert.AreEqual(first, AssetDatabase.FindAssets("", new[] { _root }).Length);
        }

        [Test]
        public void AssetSetup_Run_FillsEveryMaterialExceptOverlay()
        {
            var config = AssetSetup.Run(_paths);

            Assert.IsNotNull(config.Wood);
            Assert.IsNotNull(config.Concrete);
            Assert.IsNotNull(config.Sheathing);
            Assert.IsNotNull(config.FlatOpaque);
            Assert.IsNotNull(config.FlatTransparent);
            Assert.IsNotNull(config.Grid);
            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<RenderingConfig>(_paths.RenderingConfig), config);
        }

        [Test]
        public void AssetSetup_Run_TransparentTemplatesRenderAfterOpaques()
        {
            var config = AssetSetup.Run(_paths);

            Assert.That(config.FlatOpaque.renderQueue, Is.LessThan(2500));
            Assert.That(config.FlatTransparent.renderQueue, Is.GreaterThanOrEqualTo(3000));
            Assert.That(config.Sheathing.renderQueue, Is.GreaterThanOrEqualTo(3000));
            Assert.That(config.Grid.renderQueue, Is.LessThan(config.Sheathing.renderQueue));
        }

        [Test]
        public void SceneLayout_ConfigureTwice_SameObjectsAndComponents()
        {
            var assets = LoadSceneAssets();

            SceneLayout.Configure(_scene, assets);
            var first = Snapshot();
            SceneLayout.Configure(_scene, assets);

            CollectionAssert.AreEqual(first, Snapshot());
            CollectionAssert.IsSupersetOf(first.Select(s => s.Split(':')[0]).ToArray(),
                new[] { SceneLayout.CameraName, SceneLayout.LightName, SceneLayout.GridName, SceneLayout.StructureName, SceneLayout.ShellName, SceneLayout.AppName });
        }

        [Test]
        public void SceneLayout_Configure_GridHasNoColliderAndLightHasNoShadows()
        {
            SceneLayout.Configure(_scene, LoadSceneAssets());

            var roots = _scene.GetRootGameObjects();
            var grid = roots.Single(go => go.name == SceneLayout.GridName);
            var light = roots.Single(go => go.name == SceneLayout.LightName).GetComponent<Light>();
            Assert.IsNull(grid.GetComponent<Collider>());
            Assert.AreEqual(LightShadows.None, light.shadows);
        }

        // Regression: the menu opened the scene after loading RenderingConfig, the open unloaded it and _rendering was saved as null.
        [Test]
        public void SetupSceneMenuApply_SavedScene_WiresEveryBootstrapReference()
        {
            string scenePath = $"{_root}/Main.unity";
            AssetSetup.EnsureFolder(_root);
            EditorSceneManager.SaveScene(_scene, scenePath);

            SetupSceneMenu.Apply(_paths, scenePath);

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var app = scene.GetRootGameObjects().Single(go => go.name == SceneLayout.AppName);
            var so = new SerializedObject(app.GetComponent<AppBootstrap>());
            foreach (var field in new[] { "_structureJson", "_rendering", "_renderer", "_camera", "_pointer", "_shell" })
                Assert.IsNotNull(so.FindProperty(field).objectReferenceValue, field);
            Assert.IsNotNull(app.GetComponent<PointerInput>().Ui);
        }

        private SceneAssets LoadSceneAssets()
        {
            AssetSetup.Run(_paths);
            return SceneAssets.Load(_paths);
        }

        private string[] Snapshot() =>
            _scene.GetRootGameObjects()
                .Select(go => $"{go.name}:{string.Join(",", go.GetComponents<Component>().Select(c => c.GetType().Name))}")
                .OrderBy(s => s)
                .ToArray();
    }
}
