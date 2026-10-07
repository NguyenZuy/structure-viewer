using System.Collections;
using System.Linq;
using NUnit.Framework;
using StructureViewer.Domain.Structure;
using StructureViewer.Presentation.Structure;
using StructureViewer.Tests.Fixtures;
using UnityEngine;
using UnityEngine.TestTools;

namespace StructureViewer.Tests.PlayMode.Structure
{
    public sealed class StructureRendererTests
    {
        // MiniHouse groups: 2 walls, joist bay, bearer, 2 trusses, 2 roof panels, slab.
        private const int MiniHouseGroups = 9;

        private StructureModel _model;
        private GameObject _host;
        private StructureRenderer _renderer;
        private Camera _camera;
        private Material _wood;
        private Material _highlight;

        [SetUp]
        public void SetUp()
        {
            _model = TestStructures.MiniHouse();
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            _wood = new Material(shader) { name = "Wood" };
            _highlight = new Material(shader) { name = "Highlight" };

            _camera = new GameObject("Test Camera").AddComponent<Camera>();
            _host = new GameObject("Renderer");
            _renderer = _host.AddComponent<StructureRenderer>();
            _renderer.Camera = _camera;
            _renderer.Build(_model, _ => _wood);
            Physics.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_host);
            Object.Destroy(_camera.gameObject);
            Object.Destroy(_wood);
            Object.Destroy(_highlight);
        }

        [Test]
        public void Build_OneChunkPerGroupAndOneColliderPerElement()
        {
            Assert.AreEqual(_model.Elements.Count, _renderer.Count);
            Assert.AreEqual(MiniHouseGroups, _renderer.ChunkRendererCount);
            Assert.AreEqual(_model.Elements.Count, _host.GetComponentsInChildren<ElementHandle>().Length);
            Assert.AreEqual(_model.Bounds, _renderer.ModelBounds);
        }

        [Test]
        public void TryPick_MemberCentre_ReturnsItsIndex()
        {
            int bearer = _model.ElementsInGroup(TestStructures.Bearer)[0];
            HideAllExcept(bearer);
            var centre = _model.Elements[bearer].Bounds.center;
            LookAt(centre, new Vector3(0f, 0f, -4f));

            bool picked = _renderer.TryPick(_camera.WorldToScreenPoint(centre), out var hit);

            Assert.IsTrue(picked);
            Assert.AreEqual(bearer, hit.Index);
            Assert.Less(Vector3.Distance(centre, hit.Point), 0.2f);
        }

        [Test]
        public void TryPick_EmptySpace_ReturnsFalse()
        {
            LookAt(new Vector3(50f, 50f, 50f), new Vector3(0f, 0f, -4f));

            Assert.IsFalse(_renderer.TryPick(new Vector2(_camera.pixelWidth * 0.5f, _camera.pixelHeight * 0.5f), out _));
        }

        [Test]
        public void SetVisible_False_RemovesOnlyThatElementsGeometryAndCollider()
        {
            int stud = _model.IndexOf(TestStructures.VerticalStudId);
            int before = TotalVertices();

            _renderer.SetVisible(stud, false);
            _renderer.RebuildDirtyGroups();

            Assert.AreEqual(before - 24, TotalVertices());
            var handle = _host.GetComponentsInChildren<ElementHandle>().Single(h => h.Index == stud);
            Assert.IsFalse(handle.GetComponent<Collider>().enabled);
        }

        [Test]
        public void SetMaterial_OneElement_SplitsItsGroupIntoTwoChunks()
        {
            int stud = _model.IndexOf(TestStructures.VerticalStudId);

            _renderer.SetMaterial(stud, _highlight);
            _renderer.RebuildDirtyGroups();

            Assert.AreEqual(MiniHouseGroups + 1, _renderer.ChunkRendererCount);
            var highlighted = _host.GetComponentsInChildren<MeshRenderer>().Where(r => r.enabled && r.sharedMaterial == _highlight).ToList();
            Assert.AreEqual(1, highlighted.Count);
            Assert.AreEqual(24, highlighted[0].GetComponent<MeshFilter>().sharedMesh.vertexCount);
        }

        [Test]
        public void SetMaterial_BackToOriginal_MergesChunksAgain()
        {
            int stud = _model.IndexOf(TestStructures.VerticalStudId);
            _renderer.SetMaterial(stud, _highlight);
            _renderer.RebuildDirtyGroups();

            _renderer.SetMaterial(stud, _wood);
            _renderer.RebuildDirtyGroups();

            Assert.AreEqual(MiniHouseGroups, _renderer.ChunkRendererCount);
        }

        [UnityTest]
        public IEnumerator ChangesInOneGroup_RebuildThatGroupOncePerFrame()
        {
            int rebuildsBefore = _renderer.RebuildCount;
            foreach (int index in _model.ElementsInGroup(TestStructures.NorthWall))
                _renderer.SetMaterial(index, _highlight);

            yield return null;

            Assert.AreEqual(rebuildsBefore + 1, _renderer.RebuildCount);
        }

        [Test]
        public void SetVisible_NoChange_DoesNotRebuild()
        {
            int rebuildsBefore = _renderer.RebuildCount;

            _renderer.SetVisible(0, true);
            _renderer.SetMaterial(0, _wood);
            _renderer.RebuildDirtyGroups();

            Assert.AreEqual(rebuildsBefore, _renderer.RebuildCount);
        }

        [UnityTest]
        public IEnumerator Clear_DestroysSpawnedObjectsAndMeshes()
        {
            var meshes = _host.GetComponentsInChildren<MeshFilter>().Select(f => f.sharedMesh).ToList();

            _renderer.Clear();
            yield return null;

            Assert.AreEqual(0, _host.transform.childCount);
            Assert.IsTrue(meshes.All(m => m == null), "runtime meshes leaked");
            Assert.AreEqual(0, _renderer.Count);
        }

        private void HideAllExcept(int keep)
        {
            for (int i = 0; i < _model.Elements.Count; i++)
                _renderer.SetVisible(i, i == keep);
            _renderer.RebuildDirtyGroups();
            Physics.SyncTransforms();
        }

        private void LookAt(Vector3 target, Vector3 offset)
        {
            _camera.transform.position = target + offset;
            _camera.transform.LookAt(target);
        }

        private int TotalVertices() =>
            _host.GetComponentsInChildren<MeshRenderer>().Where(r => r.enabled).Sum(r => r.GetComponent<MeshFilter>().sharedMesh.vertexCount);
    }
}
