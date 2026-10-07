using System;
using StructureViewer.Presentation.Contracts;
using StructureViewer.Presentation.Display;
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
        public DisplayPaletteAsset Palette { get; private set; }
        public VisualTreeAsset InfoLayout { get; private set; }
        public VisualTreeAsset LayersLayout { get; private set; }
        public VisualTreeAsset LegendLayout { get; private set; }
        public VisualTreeAsset TakeoffLayout { get; private set; }

        public static SceneAssets Load(SetupPaths paths) =>
            new SceneAssets
            {
                Rendering = Require<RenderingConfig>(paths.RenderingConfig),
                PanelSettings = Require<PanelSettings>(paths.PanelSettings),
                ShellLayout = Require<VisualTreeAsset>(SetupPaths.ShellLayout),
                Structure = Require<TextAsset>(SetupPaths.SampleStructure),
                Palette = Require<DisplayPaletteAsset>(paths.DisplayPalette),
                InfoLayout = Require<VisualTreeAsset>(SetupPaths.InfoLayout),
                LayersLayout = Require<VisualTreeAsset>(SetupPaths.LayersLayout),
                LegendLayout = Require<VisualTreeAsset>(SetupPaths.LegendLayout),
                TakeoffLayout = Require<VisualTreeAsset>(SetupPaths.TakeoffLayout)
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
