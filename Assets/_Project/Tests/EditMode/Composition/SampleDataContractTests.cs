using System.IO;
using NUnit.Framework;
using StructureViewer.Editor.Generator;
using StructureViewer.Editor.Setup;
using StructureViewer.Infrastructure.Parsing;
using UnityEngine;

namespace StructureViewer.Tests.EditMode.Composition
{
    // The generator and the parser are separate features; these pin the JSON contract between them.
    public sealed class SampleDataContractTests
    {
        [Test]
        public void GeneratedHouse_ParsesWithoutErrors_AndKeepsEveryElement()
        {
            var dto = SampleHouseGenerator.Generate(new SampleHouseSpec());

            var result = new JsonStructureParser().Parse(JsonUtility.ToJson(dto));

            Assert.IsTrue(result.Success, string.Join("\n", result.Errors));
            Assert.AreEqual(dto.members.Length + dto.panels.Length + dto.slabs.Length, result.Model.Elements.Count);
        }

        [Test]
        public void CommittedSampleFile_IsUpToDateWithTheGenerator()
        {
            var generated = JsonUtility.ToJson(SampleHouseGenerator.Generate(new SampleHouseSpec()), prettyPrint: true);

            Assert.AreEqual(Normalize(generated), Normalize(File.ReadAllText(SetupPaths.SampleStructure)),
                "Run Tools > Structure Viewer > Generate Sample House.");
        }

        // Line endings depend on git settings, not on the generator.
        private static string Normalize(string text) => text.Replace("\r\n", "\n");
    }
}
