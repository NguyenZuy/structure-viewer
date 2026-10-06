using System.IO;
using StructureViewer.Editor.Build;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace StructureViewer.Sandbox.Editor
{
    // Throwaway B08 tooling: builds the spike scene and its assets from code, then a WebGL build of it.
    public static class PerfSpikeSetup
    {
        private const string Folder = "Assets/_Project/Sandbox";
        private const string ScenePath = Folder + "/PerfSpike.unity";
        private const string OutputPath = "Builds/PerfSpike";

        [MenuItem("Tools/Structure Viewer/Sandbox/Create Perf Spike Scene")]
        public static void CreateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var albedo = CreateTexture("PerfSpike_WoodAlbedo", isNormal: false);
            var normal = CreateTexture("PerfSpike_WoodNormal", isNormal: true);
            var lit = Shader.Find("Universal Render Pipeline/Lit");

            var wood = CreateMaterial("PerfSpike_Wood", lit, Color.white, transparent: false);
            wood.SetTexture("_BaseMap", albedo);
            wood.SetTexture("_BumpMap", normal);
            wood.EnableKeyword("_NORMALMAP");
            wood.SetFloat("_Smoothness", 0.25f);
            var concrete = CreateMaterial("PerfSpike_Concrete", lit, new Color(0.62f, 0.6f, 0.57f), transparent: false);
            var xray = CreateMaterial("PerfSpike_XRay", lit, new Color(0.75f, 0.8f, 0.88f, 0.12f), transparent: true);
            var sheathing = CreateMaterial("PerfSpike_Sheathing", lit, new Color(0.55f, 0.4f, 0.85f, 0.35f), transparent: true);
            AssetDatabase.SaveAssets();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camera = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.86f, 0.87f, 0.89f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;

            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.None;
            light.intensity = 1.1f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.85f, 0.88f, 0.95f);
            RenderSettings.ambientEquatorColor = new Color(0.7f, 0.7f, 0.7f);
            RenderSettings.ambientGroundColor = new Color(0.45f, 0.42f, 0.4f);
            RenderSettings.skybox = null;

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var cubeMesh = cube.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(cube);

            var spike = new GameObject("PerfSpike").AddComponent<PerfSpike>();
            var so = new SerializedObject(spike);
            so.FindProperty("_wood").objectReferenceValue = wood;
            so.FindProperty("_xray").objectReferenceValue = xray;
            so.FindProperty("_concrete").objectReferenceValue = concrete;
            so.FindProperty("_sheathing").objectReferenceValue = sheathing;
            so.FindProperty("_cube").objectReferenceValue = cubeMesh;
            so.FindProperty("_camera").objectReferenceValue = camera;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"Perf spike scene created at {ScenePath}.");
        }

        [MenuItem("Tools/Structure Viewer/Sandbox/Build Perf Spike")]
        public static void Build()
        {
            if (!File.Exists(ScenePath))
                CreateScene();
            var report = WebGLBuild.Build(new[] { ScenePath }, OutputPath);
            if (report.summary.result == BuildResult.Succeeded)
                Debug.Log($"Serve it on the LAN:  python -m http.server 8000 --directory {OutputPath}");
        }

        private static Material CreateMaterial(string name, Shader shader, Color color, bool transparent)
        {
            string path = $"{Folder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetColor("_BaseColor", color);
            material.enableInstancing = false;

            if (transparent)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)RenderQueue.Transparent;
                material.SetShaderPassEnabled("DepthOnly", false);
                material.SetShaderPassEnabled("ShadowCaster", false);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        // Procedural stand-ins for the ambientCG textures (B04): same size and import settings, so the cost is representative.
        private static Texture2D CreateTexture(string name, bool isNormal)
        {
            const int size = 1024;
            string path = $"{Folder}/{name}.png";
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float grain = Mathf.PerlinNoise(x * 0.004f, y * 0.06f) * 0.7f + Mathf.PerlinNoise(x * 0.03f, y * 0.4f) * 0.3f;
                    if (isNormal)
                    {
                        float slope = Mathf.PerlinNoise(x * 0.03f + 0.5f, y * 0.4f) - Mathf.PerlinNoise(x * 0.03f - 0.5f, y * 0.4f);
                        pixels[y * size + x] = new Color32((byte)(128 + slope * 200f), 128, 255, 255);
                    }
                    else
                    {
                        var light = new Color(0.86f, 0.7f, 0.5f);
                        var dark = new Color(0.62f, 0.45f, 0.28f);
                        pixels[y * size + x] = Color.Lerp(dark, light, grain);
                    }
                }
            }
            texture.SetPixels32(pixels);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.maxTextureSize = size;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
