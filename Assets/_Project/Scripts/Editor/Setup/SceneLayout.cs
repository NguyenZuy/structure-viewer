using StructureViewer.Bootstrap;
using StructureViewer.Presentation.CameraControl;
using StructureViewer.Presentation.Input;
using StructureViewer.Presentation.Labels;
using StructureViewer.Presentation.Measure;
using StructureViewer.Presentation.Shell;
using StructureViewer.Presentation.Structure;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace StructureViewer.Editor.Setup
{
    // Brings a scene to the expected layout, reusing root objects by name. The scene must be the active one
    // (RenderSettings belong to the active scene).
    public static class SceneLayout
    {
        public const string CameraName = "Main Camera";
        public const string LightName = "Directional Light";
        public const string GridName = "Ground Grid";
        public const string StructureName = "Structure";
        public const string ShellName = "Shell";
        public const string AppName = "App";

        // Post-processing is off on every platform (CLAUDE.md › PC + mobile), so the template volume only costs a lookup.
        private const string LegacyVolumeName = "Global Volume";

        private static readonly Color Background = new Color(0.86f, 0.87f, 0.89f);
        private static readonly Color AmbientSky = new Color(0.86f, 0.89f, 0.93f);
        private static readonly Color AmbientEquator = new Color(0.72f, 0.73f, 0.74f);
        private static readonly Color AmbientGround = new Color(0.52f, 0.50f, 0.47f);
        private static readonly Color Sun = new Color(1f, 0.96f, 0.9f);

        public static void Configure(Scene scene, SceneAssets assets)
        {
            var roots = scene.GetRootGameObjects();
            DestroyRoot(roots, LegacyVolumeName);

            var camera = ConfigureCamera(FindOrCreateRoot(roots, CameraName));
            ConfigureLight(FindOrCreateRoot(roots, LightName));
            ConfigureGrid(roots, assets.Rendering.Grid);

            var structure = FindOrCreateRoot(roots, StructureName);
            ResetTransform(structure.transform);
            var renderer = GetOrAdd<StructureRenderer>(structure);

            var shellObject = FindOrCreateRoot(roots, ShellName);
            var document = GetOrAdd<UIDocument>(shellObject);
            document.panelSettings = assets.PanelSettings;
            document.visualTreeAsset = assets.ShellLayout;
            var shell = GetOrAdd<ShellView>(shellObject);

            var app = FindOrCreateRoot(roots, AppName);
            ResetTransform(app.transform);
            var pointer = GetOrAdd<PointerInput>(app);
            pointer.Ui = document;
            var views = new FeatureViews(GetOrAdd<ShortcutInput>(app), GetOrAdd<MeasureView>(app), GetOrAdd<LabelsView>(app));
            WireBootstrap(GetOrAdd<AppBootstrap>(app), assets, renderer, camera, pointer, shell, views);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = AmbientSky;
            RenderSettings.ambientEquatorColor = AmbientEquator;
            RenderSettings.ambientGroundColor = AmbientGround;
            RenderSettings.skybox = null;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = null;
            RenderSettings.fog = false;
        }

        private static CameraController ConfigureCamera(GameObject go)
        {
            go.tag = "MainCamera";
            go.transform.SetPositionAndRotation(new Vector3(14f, 10f, -14f), Quaternion.Euler(28f, -45f, 0f));

            var camera = GetOrAdd<Camera>(go);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Background;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 500f;

            var data = GetOrAdd<UniversalAdditionalCameraData>(go);
            data.renderPostProcessing = false;
            data.renderShadows = false;
            data.antialiasing = AntialiasingMode.None;

            return GetOrAdd<CameraController>(go);
        }

        private readonly struct FeatureViews
        {
            public FeatureViews(ShortcutInput shortcuts, MeasureView measure, LabelsView labels)
            {
                Shortcuts = shortcuts;
                Measure = measure;
                Labels = labels;
            }

            public ShortcutInput Shortcuts { get; }
            public MeasureView Measure { get; }
            public LabelsView Labels { get; }
        }

        private static void WireBootstrap(AppBootstrap bootstrap, SceneAssets assets, StructureRenderer renderer,
            CameraController camera, PointerInput pointer, ShellView shell, FeatureViews views)
        {
            var so = new SerializedObject(bootstrap);
            so.FindProperty("_structureJson").objectReferenceValue = assets.Structure;
            so.FindProperty("_rendering").objectReferenceValue = assets.Rendering;
            so.FindProperty("_palette").objectReferenceValue = assets.Palette;
            so.FindProperty("_renderer").objectReferenceValue = renderer;
            so.FindProperty("_camera").objectReferenceValue = camera;
            so.FindProperty("_pointer").objectReferenceValue = pointer;
            so.FindProperty("_shortcuts").objectReferenceValue = views.Shortcuts;
            so.FindProperty("_shell").objectReferenceValue = shell;
            so.FindProperty("_measureView").objectReferenceValue = views.Measure;
            so.FindProperty("_labelsView").objectReferenceValue = views.Labels;
            so.FindProperty("_infoLayout").objectReferenceValue = assets.InfoLayout;
            so.FindProperty("_layersLayout").objectReferenceValue = assets.LayersLayout;
            so.FindProperty("_legendLayout").objectReferenceValue = assets.LegendLayout;
            so.FindProperty("_takeoffLayout").objectReferenceValue = assets.TakeoffLayout;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureLight(GameObject go)
        {
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(50f, -30f, 0f));

            var light = GetOrAdd<Light>(go);
            light.type = LightType.Directional;
            light.color = Sun;
            light.intensity = 1.1f;
            light.shadows = LightShadows.None;
        }

        private static void ConfigureGrid(GameObject[] roots, Material material)
        {
            var go = FindRoot(roots, GridName);
            if (go == null)
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = GridName;
                // Picking raycasts must only ever hit structure colliders.
                Object.DestroyImmediate(go.GetComponent<Collider>());
            }

            // Slightly below 0 so it never z-fights with slab or plate bottoms.
            go.transform.SetPositionAndRotation(new Vector3(0f, -0.005f, 0f), Quaternion.Euler(90f, 0f, 0f));
            go.transform.localScale = new Vector3(AssetSetup.GridSizeMetres, AssetSetup.GridSizeMetres, 1f);

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private static GameObject FindOrCreateRoot(GameObject[] roots, string name)
        {
            var go = FindRoot(roots, name);
            return go != null ? go : new GameObject(name);
        }

        private static GameObject FindRoot(GameObject[] roots, string name)
        {
            foreach (var root in roots)
                if (root != null && root.name == name)
                    return root;
            return null;
        }

        private static void DestroyRoot(GameObject[] roots, string name)
        {
            var go = FindRoot(roots, name);
            if (go != null)
                Object.DestroyImmediate(go);
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }

        private static void ResetTransform(Transform t)
        {
            t.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            t.localScale = Vector3.one;
        }
    }
}
