using NUnit.Framework;
using StructureViewer.Application.Events;
using StructureViewer.Application.Loading;
using StructureViewer.Tests.Fixtures;

namespace StructureViewer.Tests.EditMode.Parsing
{
    public sealed class LoadStructureUseCaseTests
    {
        private sealed class ScriptedParser : IStructureParser
        {
            public ParseResult Result;
            public string LastJson;

            public ParseResult Parse(string json)
            {
                LastJson = json;
                return Result;
            }
        }

        private ScriptedParser _parser;
        private StructureSession _session;
        private EventBus _bus;
        private EventRecorder<StructureLoaded> _loaded;
        private LoadStructureUseCase _useCase;

        [SetUp]
        public void SetUp()
        {
            _parser = new ScriptedParser();
            _session = new StructureSession();
            _bus = new EventBus();
            _loaded = new EventRecorder<StructureLoaded>(_bus);
            _useCase = new LoadStructureUseCase(_parser, _session, _bus);
        }

        [TearDown]
        public void TearDown() => _loaded.Dispose();

        [Test]
        public void Execute_Success_SetsSessionAndPublishesOnce()
        {
            var model = TestStructures.MiniHouse();
            _parser.Result = ParseResult.Ok(model);

            var result = _useCase.Execute("{}");

            Assert.IsTrue(result.Success);
            Assert.AreSame(model, _session.Current);
            Assert.AreEqual(1, _loaded.Count);
            Assert.AreSame(model, _loaded.Last.Model);
            Assert.AreEqual("{}", _parser.LastJson);
        }

        [Test]
        public void Execute_Failure_PublishesNothingAndKeepsPreviousModel()
        {
            var previous = TestStructures.MiniHouse();
            _session.Set(previous);
            _parser.Result = ParseResult.Fail(new[] { "bad" });

            var result = _useCase.Execute("{}");

            Assert.IsFalse(result.Success);
            Assert.AreSame(previous, _session.Current);
            Assert.AreEqual(0, _loaded.Count);
        }
    }
}
