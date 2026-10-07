using System.Linq;
using NUnit.Framework;
using StructureViewer.Application.Events;
using StructureViewer.Domain.Structure;
using StructureViewer.Presentation.Layers;
using StructureViewer.Tests.Fixtures;

namespace StructureViewer.Tests.EditMode.Visibility
{
    public sealed class VisibilityApplierTests
    {
        private StructureModel _model;
        private EventBus _bus;
        private FakeStructureRenderer _renderer;
        private VisibilityApplier _applier;

        [SetUp]
        public void SetUp()
        {
            _model = TestStructures.MiniHouse();
            _bus = new EventBus();
            _renderer = new FakeStructureRenderer(_model);
            _applier = new VisibilityApplier(_renderer, _bus);
        }

        [TearDown]
        public void TearDown() => _applier.Dispose();

        [Test]
        public void FirstChange_AppliesEveryElement()
        {
            _bus.Publish(new VisibilityChanged(new FakeVisibility(3)));

            Assert.AreEqual(_model.Elements.Count, _renderer.VisibleCalls.Count);
            Assert.IsFalse(_renderer.IsVisible(3));
        }

        [Test]
        public void LaterChange_TouchesOnlyChangedElements()
        {
            _bus.Publish(new VisibilityChanged(new FakeVisibility(3)));
            _renderer.ClearCalls();

            _bus.Publish(new VisibilityChanged(new FakeVisibility(5)));

            CollectionAssert.AreEquivalent(new[] { (3, true), (5, false) }, _renderer.VisibleCalls.Select(c => (c.Index, c.Visible)));
        }

        [Test]
        public void StructureLoaded_ForcesAFullPassOnTheNextChange()
        {
            _bus.Publish(new VisibilityChanged(new FakeVisibility(3)));
            _bus.Publish(new StructureLoaded(_model));
            _renderer.ClearCalls();

            _bus.Publish(new VisibilityChanged(new FakeVisibility(3)));

            Assert.AreEqual(_model.Elements.Count, _renderer.VisibleCalls.Count);
        }

        [Test]
        public void Dispose_StopsApplying()
        {
            _applier.Dispose();

            _bus.Publish(new VisibilityChanged(new FakeVisibility(3)));

            Assert.IsEmpty(_renderer.VisibleCalls);
        }
    }
}
