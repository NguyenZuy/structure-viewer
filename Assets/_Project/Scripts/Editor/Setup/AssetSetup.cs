using System;
using System.IO;
using System.Linq;
using StructureViewer.Presentation.Contracts;
using StructureViewer.Presentation.Display;
using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace StructureViewer.Editor.Setup
{
    // Creates or updates every generated rendering/UI asset in place, so running it again never duplicates anything.
    public static class AssetSetup
    {
        public const float GridSizeMetres = 80f;

        private const string LitShader = "Universal Render Pipeline/Lit";
        private const string UnlitShader = "Universal Render Pipeline/Unlit";
        private const int MaxTextureSize = 1024;

        // World size one texture repeat covers; mesh UVs are in metres.
        private const float WoodTileMetres = 0.75f;
        private const float ConcreteTileMetres = 2f;

        // One grid cell = 1 m. Mipmaps average the thin lines away with distance, which fades the grid for free.
        private const int GridPixels = 128;
        private const int GridLinePixels = 2;
        private static readonly Color32 GridLine = new Color32(70, 74, 82, 120);

        private static readonly Color Sheathing = new Color(0.55f, 0.36f, 0.96f, 0.35f);
        private static readonly Color FlatTransparent = new Color(1f, 1f, 1f, 0.3f);

        public static RenderingConfig Run(SetupPaths paths)
        {
            var woodColor = ConfigureTexture(SetupPaths.WoodColor, normalMap: false);
            var woodNormal = ConfigureTexture(SetupPaths.WoodNormal, normalMap: true);
            var concreteColor = ConfigureTexture(SetupPaths.ConcreteColor, normalMap: false);
            var concreteNormal = ConfigureTexture(SetupPaths.ConcreteNormal, normalMap: true);
            var grid = EnsureGridTexture(paths.GridTexture);

            var wood = EnsureMaterial(paths.Material("Wood"), LitShader);
            SetupLit(wood, Color.white, 0.25f, transparent: false, woodColor, woodNormal, WoodTileMetres);

            var concrete = EnsureMaterial(paths.Material("Concrete"), LitShader);
            SetupLit(concrete, Color.white, 0.15f, transparent: false, concreteColor, concreteNormal, ConcreteTileMetres);

            var sheathing = EnsureMaterial(paths.Material("Sheathing"), LitShader);
            SetupLit(sheathing, Sheathing, 0.2f, transparent: true);

            var flatOpaque = EnsureMaterial(paths.Material("FlatOpaque"), LitShader);
            SetupLit(flatOpaque, Color.white, 0.2f, transparent: false);

            var flatTransparent = EnsureMaterial(paths.Material("FlatTransparent"), LitShader);
            SetupLit(flatTransparent, FlatTransparent, 0.2f, transparent: true);

            var gridMaterial = EnsureMaterial(paths.Material("Grid"), UnlitShader);
            SetupGrid(gridMaterial, grid);

            var config = EnsureAsset(paths.RenderingConfig, ScriptableObject.CreateInstance<RenderingConfig>);
            var so = new SerializedObject(config);
            so.FindProperty("_wood").objectReferenceValue = wood;
            so.FindProperty("_concrete").objectReferenceValue = concrete;
            so.FindProperty("_sheathing").objectReferenceValue = sheathing;
            so.FindProperty("_flatOpaque").objectReferenceValue = flatOpaque;
            so.FindProperty("_flatTransparent").objectReferenceValue = flatTransparent;
            so.FindProperty("_grid").objectReferenceValue = gridMaterial;
            // _overlay is created by the measure feature (B12).
            so.ApplyModifiedPropertiesWithoutUndo();

            EnsurePanelSettings(paths.PanelSettings);
            // Created once with the coded defaults; later runs keep any colours tweaked in the inspector.
            EnsureAsset(paths.DisplayPalette, ScriptableObject.CreateInstance<DisplayPaletteAsset>);

            AssetDatabase.SaveAssets();
            return config;
        }

        private static Texture2D ConfigureTexture(string path, bool normalMap,
            TextureImporterCompression compression = TextureImporterCompression.Compressed, int anisoLevel = 2)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter
                           ?? throw new InvalidOperationException($"Texture not found: {path}");
            var type = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;

            if (importer.textureType != type || importer.maxTextureSize != MaxTextureSize ||
                importer.textureCompression != compression || !importer.mipmapEnabled ||
                importer.wrapMode != TextureWrapMode.Repeat || importer.anisoLevel != anisoLevel)
            {
                importer.textureType = type;
                importer.maxTextureSize = MaxTextureSize;
                importer.textureCompression = compression;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.anisoLevel = anisoLevel;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Texture2D EnsureGridTexture(string path)
        {
            var png = GridPng();
            if (!File.Exists(path) || !File.ReadAllBytes(path).SequenceEqual(png))
            {
                EnsureFolder(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, png);
                AssetDatabase.ImportAsset(path);
            }
            // Uncompressed: block compression smears the 2 px alpha lines; the texture is only 64 KB.
            return ConfigureTexture(path, normalMap: false, TextureImporterCompression.Uncompressed, anisoLevel: 8);
        }

        // Lines sit on the texture edges so they meet across the repeat seam; transparent pixels keep the line colour
        // to avoid dark fringes when filtering.
        private static byte[] GridPng()
        {
            var pixels = new Color32[GridPixels * GridPixels];
            int half = GridLinePixels / 2;
            for (int y = 0; y < GridPixels; y++)
            for (int x = 0; x < GridPixels; x++)
            {
                bool line = x < half || x >= GridPixels - half || y < half || y >= GridPixels - half;
                pixels[y * GridPixels + x] = line ? GridLine : new Color32(GridLine.r, GridLine.g, GridLine.b, 0);
            }

            var texture = new Texture2D(GridPixels, GridPixels, TextureFormat.RGBA32, false);
            try
            {
                texture.SetPixels32(pixels);
                return texture.EncodeToPNG();
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        private static Material EnsureMaterial(string path, string shaderName)
        {
            var shader = Shader.Find(shaderName) ?? throw new InvalidOperationException($"Shader not found: {shaderName}");
            var material = EnsureAsset(path, () => new Material(shader));
            if (material.shader != shader)
                material.shader = shader;
            return material;
        }

        private static void SetupLit(Material material, Color color, float smoothness, bool transparent,
            Texture2D albedo = null, Texture2D normal = null, float tileMetres = 1f)
        {
            material.SetFloat("_WorkflowMode", 1f);
            material.SetColor("_BaseColor", color);
            material.SetTexture("_BaseMap", albedo);
            material.SetTextureScale("_BaseMap", Vector2.one / tileMetres);
            material.SetTexture("_BumpMap", normal);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_ReceiveShadows", 0f);
            // No reflection probes and a solid-colour background: environment reflections would only add a sky tint.
            material.SetFloat("_EnvironmentReflections", 0f);
            SetSurface(material, transparent);
            BaseShaderGUI.SetMaterialKeywords(material, LitGUI.SetMaterialKeywords);
            EditorUtility.SetDirty(material);
        }

        private static void SetupGrid(Material material, Texture2D grid)
        {
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BaseMap", grid);
            material.SetTextureScale("_BaseMap", Vector2.one * GridSizeMetres);
            SetSurface(material, transparent: true);
            // Draw before other transparents (sheathing) so the ground never blends over them.
            material.SetFloat("_QueueOffset", -50f);
            BaseShaderGUI.SetMaterialKeywords(material);
            EditorUtility.SetDirty(material);
        }

        private static void SetSurface(Material material, bool transparent)
        {
            material.SetFloat("_Surface", transparent ? (float)BaseShaderGUI.SurfaceType.Transparent : (float)BaseShaderGUI.SurfaceType.Opaque);
            material.SetFloat("_Blend", (float)BaseShaderGUI.BlendMode.Alpha);
            material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Back);
        }

        private static void EnsurePanelSettings(string path)
        {
            var settings = EnsureAsset(path, ScriptableObject.CreateInstance<PanelSettings>);
            settings.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(SetupPaths.Theme)
                                       ?? throw new InvalidOperationException($"Theme not found: {SetupPaths.Theme}");
            // 1 UI px = 1 CSS px (WebGL reports dpi = 96 × devicePixelRatio), so a phone in portrait is ~360–430 px wide
            // and the 768 px compact breakpoint and 44 px touch targets mean what they say.
            settings.scaleMode = PanelScaleMode.ConstantPhysicalSize;
            settings.referenceDpi = 96f;
            settings.fallbackDpi = 96f;
            EditorUtility.SetDirty(settings);
        }

        private static T EnsureAsset<T>(string path, Func<T> create) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            EnsureFolder(Path.GetDirectoryName(path));
            asset = create();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        public static void EnsureFolder(string folder)
        {
            folder = folder.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(folder))
                return;

            var parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
