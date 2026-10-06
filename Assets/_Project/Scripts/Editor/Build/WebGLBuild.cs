using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace StructureViewer.Editor.Build
{
    // Player settings live in code so the WebGL build is reproducible from a clean checkout and from CLI:
    // Unity.exe -batchmode -quit -projectPath . -buildTarget WebGL -executeMethod StructureViewer.Editor.Build.WebGLBuild.BuildFromCommandLine [-svScene Assets/...unity]
    public static class WebGLBuild
    {
        public const string OutputPath = "Builds/WebGL";
        public const string TemplateName = "PROJECT:StructureViewer";
        private const string SceneArgument = "-svScene";

        [MenuItem("Tools/Structure Viewer/Apply WebGL Player Settings")]
        public static void ApplyPlayerSettings()
        {
            var target = NamedBuildTarget.WebGL;

            PlayerSettings.productName = "Structure Viewer";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[] { GraphicsDeviceType.OpenGLES3 });

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            // Lets any static host serve the .br files without Content-Encoding headers.
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.template = TemplateName;

            PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetIl2CppCodeGeneration(target, Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.SetIl2CppCompilerConfiguration(target, Il2CppCompilerConfiguration.Master);
            PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.Low);
            PlayerSettings.stripEngineCode = true;

            UnityEditor.WebGL.UserBuildSettings.codeOptimization = UnityEditor.WebGL.WasmCodeOptimization.DiskSizeLTO;
            // ASTC: native on iOS and most Android GPUs; desktop browsers decompress the few textures at load.
            EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.ASTC;

            AssetDatabase.SaveAssets();
            Debug.Log("WebGL player settings applied.");
        }

        [MenuItem("Tools/Structure Viewer/Build WebGL")]
        public static void BuildFromMenu()
        {
            var report = Build(EnabledScenes(), OutputPath);
            if (report.summary.result == BuildResult.Succeeded)
                EditorUtility.RevealInFinder(Path.Combine(OutputPath, "index.html"));
        }

        public static void BuildFromCommandLine()
        {
            var args = Environment.GetCommandLineArgs();
            int sceneArg = Array.IndexOf(args, SceneArgument);
            var scenes = sceneArg >= 0 && sceneArg + 1 < args.Length ? new[] { args[sceneArg + 1] } : EnabledScenes();

            var report = Build(scenes, OutputPath);
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        public static BuildReport Build(string[] scenes, string outputPath)
        {
            if (scenes.Length == 0)
                throw new InvalidOperationException("No scenes to build: enable at least one scene in Build Settings.");

            ApplyPlayerSettings();
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
            Debug.Log($"WebGL build ({(profile != null ? profile.name : "no build profile")}) {summary.result}:{summary.totalSize / (1024f * 1024f):F1} MB in {summary.totalTime.TotalSeconds:F0} s, " +
                      $"{summary.totalErrors} errors, {summary.totalWarnings} warnings → {outputPath}");
            return report;
        }

        private static string[] EnabledScenes() =>
            EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
    }
}
