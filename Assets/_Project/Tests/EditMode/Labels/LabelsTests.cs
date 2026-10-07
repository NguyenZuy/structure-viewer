using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StructureViewer.Application.Events;
using StructureViewer.Domain.Labels;
using StructureViewer.Domain.Selection;
using StructureViewer.Domain.Structure;
using StructureViewer.Presentation.Labels;
using StructureViewer.Tests.Fixtures;
using UnityEngine;

namespace StructureViewer.Tests.EditMode.Labels
{
    public sealed class LabelsTests
    {
        private StructureModel _model;

        [SetUp]
        public void SetUp() => _model = TestStructures.MiniHouse();

        [Test]
        public void Anchors_OnePerGroup_AtTopCentreOfItsBounds()
        {
            var anchors = AssemblyAnchors.Build(_model);

            CollectionAssert.AreEqual(_model.Groups, anchors.Select(a => a.Group));
            var wall = anchors.Single(a => a.Group == TestStructures.NorthWall);
            var bounds = _model.ElementsInGroup(TestStructures.NorthWall)
                .Select(i => _model.Elements[i].Bounds)
                .Aggregate((a, b) => { a.Encapsulate(b); return a; });
            Assert.AreEqual(new Vector3(bounds.center.x, bounds.max.y, bounds.center.z), wall.Position);
        }

        [Test]
        public void Rule_HiddenGroupOrBehindCameraOrOffScreen_IsHidden()
        {
            var centre = new Vector3(0.5f, 0.5f, 5f);

            Assert.AreEqual(0f, LabelVisibilityRule.Opacity(false, centre, 10f, 20f));
            Assert.AreEqual(0f, LabelVisibilityRule.Opacity(true, new Vector3(0.5f, 0.5f, -1f), 10f, 20f));
            Assert.AreEqual(0f, LabelVisibilityRule.Opacity(true, new Vector3(1.2f, 0.5f, 5f), 10f, 20f));
            Assert.AreEqual(1f, LabelVisibilityRule.Opacity(true, centre, 10f, 20f));
        }

        [Test]
        public void Rule_FartherIsFainter_DownToMinimum()
        {
            float mid = LabelVisibilityRule.Opacity(true, new Vector3(0.5f, 0.5f, 15f), 10f, 20f);
            float far = LabelVisibilityRule.Opacity(true, new Vector3(0.5f, 0.5f, 100f), 10f, 20f);

            Assert.That(mid, Is.LessThan(1f).And.GreaterThan(LabelVisibilityRule.MinOpacity));
            Assert.AreEqual(LabelVisibilityRule.MinOpacity, far, 1e-5f);
        }

        [Test]
        public void Presenter_Load_ShowsAnchorsWithFadeFromModelSize()
        {
            var (bus, view, presenter) = Create();

            bus.Publish(new StructureLoaded(_model));

            Assert.AreEqual(_model.Groups.Count, view.Anchors.Count);
            float radius = _model.Bounds.extents.magnitude;
            Assert.AreEqual(radius * LabelsPresenter.NearFadeRadii, view.NearFade, 1e-4f);
            presenter.Dispose();
        }

        [Test]
        public void Presenter_GroupFullyHidden_HidesItsLabelOnlyOnChange()
        {
            var (bus, view, presenter) = Create();
            bus.Publish(new StructureLoaded(_model));
            var joists = _model.ElementsInGroup(TestStructures.JoistBay).ToArray();
            int joistAnchor = view.Anchors.ToList().FindIndex(a => a.Group == TestStructures.JoistBay);

            bus.Publish(new VisibilityChanged(new FakeVisibility(joists[0])));
            Assert.IsEmpty(view.GroupVisibleCalls, "one joist left visible");

            bus.Publish(new VisibilityChanged(new FakeVisibility(joists)));
            bus.Publish(new VisibilityChanged(new FakeVisibility(joists)));
            CollectionAssert.AreEqual(new[] { (joistAnchor, false) }, view.GroupVisibleCalls);
            presenter.Dispose();
        }

        [Test]
        public void Presenter_AssemblySelection_EmphasisesItsLabel_MemberSelectionDoesNot()
        {
            var (bus, view, presenter) = Create();
            bus.Publish(new StructureLoaded(_model));
            var wall = _model.ElementsInGroup(TestStructures.NorthWall);
            int wallAnchor = view.Anchors.ToList().FindIndex(a => a.Group == TestStructures.NorthWall);

            bus.Publish(new SelectionChanged(new SelectionSnapshot(wall, SelectionKind.Assembly)));
            Assert.AreEqual(wallAnchor, view.Emphasized);

            bus.Publish(new SelectionChanged(new SelectionSnapshot(new[] { wall[0] }, SelectionKind.Member)));
            Assert.AreEqual(-1, view.Emphasized);
            presenter.Dispose();
        }

        [Test]
        public void Presenter_Dispose_StopsUpdates()
        {
            var (bus, view, presenter) = Create();

            presenter.Dispose();
            bus.Publish(new StructureLoaded(_model));

            Assert.IsEmpty(view.Anchors);
        }

        private static (EventBus, FakeLabelsView, LabelsPresenter) Create()
        {
            var bus = new EventBus();
            var view = new FakeLabelsView();
            return (bus, view, new LabelsPresenter(view, bus));
        }

        private sealed class FakeLabelsView : ILabelsView
        {
            public IReadOnlyList<AssemblyAnchor> Anchors { get; private set; } = new AssemblyAnchor[0];
            public float NearFade { get; private set; }
            public List<(int, bool)> GroupVisibleCalls { get; } = new List<(int, bool)>();
            public int Emphasized { get; private set; } = -1;
            public bool Enabled { get; private set; } = true;

            public void ShowAnchors(IReadOnlyList<AssemblyAnchor> anchors, float nearFade, float farFade)
            {
                Anchors = anchors;
                NearFade = nearFade;
            }

            public void SetGroupVisible(int anchorIndex, bool visible) => GroupVisibleCalls.Add((anchorIndex, visible));
            public void SetEmphasized(int anchorIndex) => Emphasized = anchorIndex;
            public void SetEnabled(bool enabled) => Enabled = enabled;
        }
    }
}
