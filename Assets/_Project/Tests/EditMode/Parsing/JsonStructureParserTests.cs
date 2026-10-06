using NUnit.Framework;
using StructureViewer.Infrastructure.Parsing;
using UnityEngine;

namespace StructureViewer.Tests.EditMode.Parsing
{
    // JsonUtility needs the engine; validation rules are covered by StructureDtoConverterTests.
    public sealed class JsonStructureParserTests
    {
        private readonly JsonStructureParser _parser = new JsonStructureParser();

        [Test]
        public void Parse_DesignSample_SucceedsWithConvertedPositions()
        {
            var result = _parser.Parse(SampleDto.Json);

            Assert.IsTrue(result.Success, string.Join("\n", result.Errors));
            Assert.AreEqual(3, result.Model.Elements.Count);
            var member = result.Model.Elements[result.Model.IndexOf("T03-TC1")].Member;
            Assert.AreEqual(new Vector3(4.2f, 7f, 3.6f).ToString("F4"), member.End.ToString("F4"));
        }

        [Test]
        public void Parse_MissingRoll_DefaultsToZero()
        {
            var result = _parser.Parse(SampleDto.Json.Replace(@"""roll"": 0,", ""));

            Assert.IsTrue(result.Success, string.Join("\n", result.Errors));
            Assert.AreEqual(0f, result.Model.Elements[0].Member.Roll);
        }

        [Test]
        public void Parse_MalformedJson_FailsWithoutThrowing()
        {
            var result = _parser.Parse("{ \"name\": \"Broken\", \"levels\": [ ");

            Assert.IsFalse(result.Success);
            Assert.IsNotEmpty(result.Errors);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Parse_EmptyInput_Fails(string json)
        {
            var result = _parser.Parse(json);

            Assert.IsFalse(result.Success);
            StringAssert.Contains("empty", result.Errors[0]);
        }
    }
}
