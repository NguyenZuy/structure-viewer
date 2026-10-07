using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StructureViewer.Application.Events;
using StructureViewer.Application.Loading;
using StructureViewer.Domain.Structure;
using StructureViewer.Presentation.Takeoff;
using StructureViewer.Tests.Fixtures;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace StructureViewer.Tests.EditMode.Takeoff
{
    public sealed class TakeoffPresenterTests
    {
        private const string LayoutPath = "Assets/_Project/UI/Takeoff/Takeoff.uxml";

        private StructureModel _model;
        private EventBus _bus;
        private StructureSession _session;
        private FakeTakeoffView _view;
        private TakeoffPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _model = TestStructures.MiniHouse();
            _bus = new EventBus();
            _session = new StructureSession();
            _view = new FakeTakeoffView();
            _presenter = new TakeoffPresenter(_view, _session, _bus);
        }

        [TearDown]
        public void TearDown() => _presenter.Dispose();

        private void Load()
        {
            _session.Set(_model);
            _bus.Publish(new StructureLoaded(_model));
        }

        private static TakeoffLine Line(TakeoffContent content, string type) => content.Lines.Single(l => l.Type == type);

        [Test]
        public void BeforeLoad_RendersEmpty()
        {
            Assert.IsEmpty(_view.Last.Lines);
        }

        [Test]
        public void Load_RendersRowsThenTotal()
        {
            Load();

            var lines = _view.Last.Lines;
            Assert.IsTrue(lines[lines.Count - 1].IsTotal);
            Assert.AreEqual(_model.Elements.Count.ToString(), lines[lines.Count - 1].Count);
            Assert.IsTrue(lines.Take(lines.Count - 1).All(l => !l.IsTotal));
            Assert.AreEqual(string.Empty, Line(_view.Last, "RoofSheathing").Length);
            Assert.AreEqual(string.Empty, Line(_view.Last, "Stud").Area);
        }

        [Test]
        public void VisibleOnlyToggle_Recalculates()
        {
            Load();
            _bus.Publish(new VisibilityChanged(new FakeVisibility(_model.IndexOf(TestStructures.VerticalStudId))));
            int calculations = _presenter.CalculationCount;

            _view.ToggleVisibleOnly(true);

            Assert.AreEqual(calculations + 1, _presenter.CalculationCount);
            Assert.IsTrue(_view.Last.VisibleOnly);
            Assert.AreEqual("3", Line(_view.Last, "Stud").Count);
        }

        [Test]
        public void VisibilityChanged_IgnoredWhenVisibleOnlyIsOff()
        {
            Load();
            int calculations = _presenter.CalculationCount;

            _bus.Publish(new VisibilityChanged(new FakeVisibility(0)));

            Assert.AreEqual(calculations, _presenter.CalculationCount);
        }

        [Test]
        public void VisibilityChanged_RecalculatesWhenVisibleOnlyIsOn()
        {
            Load();
            _view.ToggleVisibleOnly(true);
            int calculations = _presenter.CalculationCount;

            _bus.Publish(new VisibilityChanged(new FakeVisibility(0)));

            Assert.AreEqual(calculations + 1, _presenter.CalculationCount);
        }

        [Test]
        public void SwatchProvider_AddsSwatchesToDataRowsOnly()
        {
            Load();

            _presenter.SetSwatchProvider(type => type == "Stud" ? Color.red : (Color?)null);

            Assert.IsTrue(_view.Last.ShowSwatches);
            Assert.AreEqual(Color.red, Line(_view.Last, "Stud").Swatch);
            Assert.IsNull(Line(_view.Last, "Joist").Swatch);
            Assert.IsNull(_view.Last.Lines.Last().Swatch);
        }

        [Test]
        public void Dispose_UnsubscribesViewAndBus()
        {
            _presenter.Dispose();
            int renders = _view.Renders.Count;

            Load();

            Assert.IsFalse(_view.HasSubscribers);
            Assert.AreEqual(renders, _view.Renders.Count);
        }

        [Test]
        public void TakeoffView_RendersLinesAndHidesSwatchColumnWithoutProvider()
        {
            var view = new TakeoffView(AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LayoutPath));
            var lines = new[]
            {
                new TakeoffLine(false, null, "Stud", "35 × 90", "MGP10", "4", "9.32", "0.029", string.Empty),
                new TakeoffLine(true, null, "Total", string.Empty, string.Empty, "4", "9.32", "0.029", string.Empty)
            };

            view.Render(new TakeoffContent(lines, false, false));
            Assert.AreEqual(2, view.LineCount);
            Assert.IsFalse(view.Root.Q<MultiColumnListView>().columns["swatch"].visible);

            view.Render(TakeoffContent.Empty);
            Assert.AreEqual(DisplayStyle.Flex, view.Root.Q("takeoff-empty").style.display.value);
        }

        private sealed class FakeTakeoffView : ITakeoffView
        {
            public event Action<bool> VisibleOnlyToggled;

            public List<TakeoffContent> Renders { get; } = new List<TakeoffContent>();
            public TakeoffContent Last => Renders[Renders.Count - 1];
            public bool HasSubscribers => VisibleOnlyToggled != null;

            public void Render(TakeoffContent content) => Renders.Add(content);

            public void ToggleVisibleOnly(bool on) => VisibleOnlyToggled?.Invoke(on);
        }
    }
}
