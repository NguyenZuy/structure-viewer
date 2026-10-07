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

            var config = AssetSetup.Run(SetupPaths.Project);
            var scene = EditorSceneManager.OpenScene(SetupPaths.MainScene, OpenSceneMode.Single);
            SceneLayout.Configure(scene, config);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Scene set up: {SetupPaths.MainScene}");
        }
    }
}
