using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEditor.WebGL;
using UnityEngine;
using UnityEngine.Rendering;

namespace StructureViewer.Editor.Build
{
    public enum WebGLBuildMode
    {
        // Smallest download: what gets deployed and measured for the README.
        Release,

        // Iteration builds for LAN/device testing: no LTO, faster IL2CPP config, Gzip. Several times quicker to link.
        Fast
    }

    // Player settings live in code so the WebGL build is reproducible from a clean checkout and from CLI:
    // Unity.exe -batchmode -quit -projectPath . -buildTarget WebGL -executeMethod StructureViewer.Editor.Build.WebGLBuild.BuildFromCommandLine [-svScene Assets/...unity] [-svFast]
    public static class WebGLBuild
    {
        public const string OutputPath = "Builds/WebGL";
        public const string TemplateName = "PROJECT:StructureViewer";
        private const string SceneArgument = "-svScene";
        private const string FastArgument = "-svFast";

        [MenuItem("Tools/Structure Viewer/Apply WebGL Player Settings")]
        public static void ApplyReleaseSettings() => ApplyPlayerSettings(WebGLBuildMode.Release);

        // With a Build Profile active, PlayerSettings writes go to that profile's player settings override.
        public static void ApplyPlayerSettings(WebGLBuildMode mode)
        {
            var target = NamedBuildTarget.WebGL;
            bool release = mode == WebGLBuildMode.Release;

            PlayerSettings.productName = "Structure Viewer";
            // The splash costs a large logo texture and seconds before the model shows; optional for every licence since Unity 6.
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[] { GraphicsDeviceType.OpenGLES3 });

            PlayerSettings.WebGL.compressionFormat = release ? WebGLCompressionFormat.Brotli : WebGLCompressionFormat.Gzip;
            // Lets any static host serve the compressed files without Content-Encoding headers.
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.template = TemplateName;

            PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetIl2CppCodeGeneration(target, Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.SetIl2CppCompilerConfiguration(target, release ? Il2CppCompilerConfiguration.Master : Il2CppCompilerConfiguration.Release);
            // Code is ~70% of the download. Our code uses no reflection; UI Toolkit and the Input System ship their own link.xml.
            PlayerSettings.SetManagedStrippingLevel(target, release ? ManagedStrippingLevel.High : ManagedStrippingLevel.Low);
            PlayerSettings.stripEngineCode = true;

            UserBuildSettings.codeOptimization = release ? WasmCodeOptimization.DiskSizeLTO : WasmCodeOptimization.BuildTimes;
            // ASTC: native on iOS and most Android GPUs; desktop browsers decompress the few textures at load.
            EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.ASTC;

            AssetDatabase.SaveAssets();
            Debug.Log($"WebGL player settings applied ({mode}).");
        }

        [MenuItem("Tools/Structure Viewer/Build WebGL")]
        public static void BuildReleaseFromMenu() => BuildFromMenu(WebGLBuildMode.Release);

        [MenuItem("Tools/Structure Viewer/Build WebGL (Fast)")]
        public static void BuildFastFromMenu() => BuildFromMenu(WebGLBuildMode.Fast);

        public static void BuildFromCommandLine()
        {
            var args = Environment.GetCommandLineArgs();
            int sceneArg = Array.IndexOf(args, SceneArgument);
            var scenes = sceneArg >= 0 && sceneArg + 1 < args.Length ? new[] { args[sceneArg + 1] } : EnabledScenes();
            var mode = Array.IndexOf(args, FastArgument) >= 0 ? WebGLBuildMode.Fast : WebGLBuildMode.Release;

            var report = Build(scenes, OutputPath, mode);
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        public static BuildReport Build(string[] scenes, string outputPath, WebGLBuildMode mode)
        {
            if (scenes.Length == 0)
                throw new InvalidOperationException("No scenes to build: enable at least one scene in Build Settings.");

            ApplyPlayerSettings(mode);
            try
            {
                var options = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = outputPath,
                    target = BuildTarget.WebGL,
                    targetGroup = BuildTargetGroup.WebGL,
                    options = BuildOptions.None
                };

                // Unity 6.6 has no profile-less platform: the active Build Profile (Web - Mobile - Release) supplies platform settings.
                var profile = BuildProfile.GetActiveBuildProfile();
                var report = BuildPipeline.BuildPlayer(options);
                var summary = report.summary;
                Debug.Log($"WebGL {mode} build ({(profile != null ? profile.name : "no build profile")}) {summary.result}: " +
                          $"{summary.totalSize / (1024f * 1024f):F1} MB in {summary.totalTime.TotalSeconds:F0} s, " +
                          $"{summary.totalErrors} errors, {summary.totalWarnings} warnings → {outputPath}");
                return report;
            }
            finally
            {
                // Keep the committed profile on release settings whatever mode was just built.
                if (mode != WebGLBuildMode.Release)
                    ApplyPlayerSettings(WebGLBuildMode.Release);
            }
        }

        private static void BuildFromMenu(WebGLBuildMode mode)
        {
            var report = Build(EnabledScenes(), OutputPath, mode);
            if (report.summary.result == BuildResult.Succeeded)
                EditorUtility.RevealInFinder(Path.Combine(OutputPath, "index.html"));
        }

        private static string[] EnabledScenes() =>
            EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
    }
}
