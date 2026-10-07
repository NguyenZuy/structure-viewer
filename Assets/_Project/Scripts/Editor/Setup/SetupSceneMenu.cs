using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StructureViewer.Editor.Setup
{
    public static class SetupSceneMenu
    {
        [MenuItem("Tools/Structure Viewer/Setup Scene")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Apply(SetupPaths.Project, SetupPaths.MainScene);
            Debug.Log($"Scene set up: {SetupPaths.MainScene}");
        }

        public static void Apply(SetupPaths paths, string scenePath)
        {
            AssetSetup.Run(paths);
            // Opening a scene unloads unused assets, so anything loaded before this point is stale: load the scene's assets after.
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            SceneLayout.Configure(scene, SceneAssets.Load(paths));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
