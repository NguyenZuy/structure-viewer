using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StructureViewer.Application.Events;
using StructureViewer.Application.Loading;
using StructureViewer.Application.Selection;
using StructureViewer.Domain.Selection;
using StructureViewer.Domain.Structure;
using StructureViewer.Presentation.Info;
using StructureViewer.Tests.Fixtures;

namespace StructureViewer.Tests.EditMode.Selection
{
    public sealed class InfoPanelPresenterTests
    {
        private StructureModel _model;
        private EventBus _bus;
        private SelectionService _selection;
        private FakeInfoPanelView _view;
        private InfoPanelPresenter _presenter;

        private int Stud => _model.IndexOf(TestStructures.VerticalStudId);

        [SetUp]
        public void SetUp()
        {
            _model = TestStructures.MiniHouse();
            _bus = new EventBus();
            var session = new StructureSession();
            session.Set(_model);
            _selection = new SelectionService(session, _bus);
            _view = new FakeInfoPanelView();
            _presenter = new InfoPanelPresenter(_view, session, _selection, _bus);
        }

        [TearDown]
        public void TearDown()
        {
            _presenter.Dispose();
            _selection.Dispose();
        }

        [Test]
        public void Construct_RendersEmptyHint()
        {
            Assert.IsTrue(_view.Last.IsEmpty);
            Assert.AreEqual(InfoPanelPresenter.EmptyMessage, _view.Last.Title);
        }

        [Test]
        public void MemberSelected_RendersMemberDetails()
        {
            _selection.Select(Stud);

            var content = _view.Last;
            Assert.AreEqual(SelectionKind.Member, content.Kind);
            Assert.AreEqual(TestStructures.VerticalStudId, content.Title);
            Assert.AreEqual("Stud", Row(content, "Type"));
            Assert.AreEqual("Wall", Row(content, "Category"));
            Assert.AreEqual("Ground Floor", Row(content, "Level"));
            Assert.AreEqual(TestStructures.NorthWall, Row(content, "Assembly"));
            Assert.AreEqual("35 × 90", Row(content, "Section"));
            Assert.AreEqual("MGP10", Row(content, "Material"));
            Assert.AreEqual("2,330 mm", Row(content, "Length"));
            Assert.IsTrue(content.CanSelectAssembly);
            Assert.IsTrue(content.CanIsolateAssembly);
            Assert.IsEmpty(content.Breakdown);
        }

        [Test]
        public void PanelSelected_RendersThicknessAndArea()
        {
            int panel = _model.IndexOf(TestStructures.RoofPanelId);

            _selection.Select(panel);

            Assert.AreEqual("12 mm", Row(_view.Last, "Thickness"));
            Assert.IsNotNull(Row(_view.Last, "Area"));
            Assert.IsNull(Row(_view.Last, "Length"));
        }

        [Test]
        public void AssemblySelected_RendersCountTotalsAndBreakdown()
        {
            _selection.SelectAssembly(Stud);

            var content = _view.Last;
            int count = _model.ElementsInGroup(TestStructures.NorthWall).Count;
            Assert.AreEqual(SelectionKind.Assembly, content.Kind);
            Assert.AreEqual($"Assembly {TestStructures.NorthWall}", content.Title);
            Assert.AreEqual(count.ToString(), Row(content, "Elements"));
            Assert.IsNotNull(Row(content, "Total length"));
            Assert.AreEqual("Stud", content.Breakdown[0].Label);
            StringAssert.StartsWith("2 · ", content.Breakdown[0].Value);
            Assert.IsFalse(content.CanSelectAssembly);
            Assert.IsTrue(content.CanIsolateAssembly);
        }

        [Test]
        public void MultiSelected_CannotIsolateAnAssembly()
        {
            _selection.Toggle(Stud);
            _selection.Toggle(_model.IndexOf(TestStructures.JoistId));

            Assert.AreEqual(SelectionKind.Multi, _view.Last.Kind);
            Assert.AreEqual("2 selected", _view.Last.Title);
            Assert.IsFalse(_view.Last.CanIsolateAssembly);
        }

        [Test]
        public void Cleared_RendersEmptyHintAgain()
        {
            _selection.Select(Stud);

            _selection.Clear();

            Assert.IsTrue(_view.Last.IsEmpty);
        }

        [Test]
        public void SelectAssemblyClicked_WidensMemberToAssembly()
        {
            _selection.Select(Stud);

            _view.RaiseSelectAssembly();

            Assert.AreEqual(SelectionKind.Assembly, _selection.Current.Kind);
        }

        [Test]
        public void IsolateAssemblyClicked_RaisesRequestWithGroup()
        {
            string requested = null;
            _presenter.IsolateAssemblyRequested += group => requested = group;
            _selection.Select(Stud);

            _view.RaiseIsolateAssembly();

            Assert.AreEqual(TestStructures.NorthWall, requested);
        }

        [Test]
        public void ClearClicked_ClearsSelection()
        {
            _selection.Select(Stud);

            _view.RaiseClear();

            Assert.IsTrue(_selection.Current.IsEmpty);
        }

        [Test]
        public void Dispose_UnsubscribesViewAndBus()
        {
            int renders = _view.Renders.Count;

            _presenter.Dispose();
            _selection.Select(Stud);

            Assert.IsFalse(_view.HasSubscribers);
            Assert.AreEqual(renders, _view.Renders.Count);
        }

        private static string Row(InfoPanelContent content, string label) =>
            content.Rows.Where(r => r.Label == label).Select(r => r.Value).FirstOrDefault();

        private sealed class FakeInfoPanelView : IInfoPanelView
        {
            public event Action SelectAssemblyClicked;
            public event Action IsolateAssemblyClicked;
            public event Action ClearClicked;

            public List<InfoPanelContent> Renders { get; } = new List<InfoPanelContent>();
            public InfoPanelContent Last => Renders[Renders.Count - 1];

            public bool HasSubscribers => SelectAssemblyClicked != null || IsolateAssemblyClicked != null || ClearClicked != null;

            public void Render(InfoPanelContent content) => Renders.Add(content);

            public void RaiseSelectAssembly() => SelectAssemblyClicked?.Invoke();
            public void RaiseIsolateAssembly() => IsolateAssemblyClicked?.Invoke();
            public void RaiseClear() => ClearClicked?.Invoke();
        }
    }
}
