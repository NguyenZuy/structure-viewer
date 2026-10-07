using System;
using StructureViewer.Presentation.Contracts;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace StructureViewer.Editor.Setup
{
    // Everything the scene layout references from the project.
    public sealed class SceneAssets
    {
        public RenderingConfig Rendering { get; private set; }
        public PanelSettings PanelSettings { get; private set; }
        public VisualTreeAsset ShellLayout { get; private set; }
        public TextAsset Structure { get; private set; }

        public static SceneAssets Load(SetupPaths paths) =>
            new SceneAssets
            {
                Rendering = Require<RenderingConfig>(paths.RenderingConfig),
                PanelSettings = Require<PanelSettings>(paths.PanelSettings),
                ShellLayout = Require<VisualTreeAsset>(SetupPaths.ShellLayout),
                Structure = Require<TextAsset>(SetupPaths.SampleStructure)
            };

        private static T Require<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new InvalidOperationException($"Missing {typeof(T).Name} at {path}.");
            return asset;
        }
    }
}
