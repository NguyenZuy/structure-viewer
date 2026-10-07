using System.IO;
using UnityEditor;
using UnityEngine;

namespace StructureViewer.Editor.Generator
{
    public static class SampleHouseMenu
    {
        public const string OutputPath = "Assets/_Project/Data/sample-house.json";

        [MenuItem("Tools/Structure Viewer/Generate Sample House")]
        public static void Generate()
        {
            var dto = SampleHouseGenerator.Generate(new SampleHouseSpec());
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            File.WriteAllText(OutputPath, JsonUtility.ToJson(dto, prettyPrint: true));
            AssetDatabase.ImportAsset(OutputPath);
            Debug.Log($"Sample house written to {OutputPath}: {dto.members.Length} members, {dto.panels.Length} panels, {dto.slabs.Length} slabs.");
        }
    }
}
