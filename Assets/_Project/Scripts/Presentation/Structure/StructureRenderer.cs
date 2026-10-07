using System;
using System.Collections.Generic;
using StructureViewer.Domain.Geometry;
using StructureViewer.Domain.Structure;
using StructureViewer.Presentation.Contracts;
using UnityEngine;
using UnityEngine.Rendering;

namespace StructureViewer.Presentation.Structure
{
    // Draws each assembly (group) as one combined mesh per material in use, and keeps one collider-only
    // object per element for picking. Visibility/material changes mark the group dirty; dirty groups are
    // rebuilt once in LateUpdate, so changing a whole assembly costs a single rebuild.
    public sealed class StructureRenderer : MonoBehaviour, IStructureRenderer
    {
        private const int MaxPickHits = 32;
        private const float MaxPickDistance = 1000f;

        [SerializeField] private Camera _camera;

        private readonly Dictionary<long, MeshData> _boxCache = new Dictionary<long, MeshData>();
        private readonly List<Material> _materials = new List<Material>();
        private readonly Dictionary<Material, int> _materialIds = new Dictionary<Material, int>();
        private readonly List<GroupState> _groups = new List<GroupState>();
        private readonly List<Mesh> _ownedMeshes = new List<Mesh>();
        private readonly MeshDataBuilder _scratch = new MeshDataBuilder();
        private readonly RaycastHit[] _hits = new RaycastHit[MaxPickHits];

        private StructureModel _model;
        private Transform _root;
        private MeshData[] _meshes;
        private MemberPose[] _poses;
        private Collider[] _colliders;
        private int[] _groupOf;
        private bool[] _visible;
        private int[] _materialOf;
        private bool _anyDirty;

        private sealed class GroupState
        {
            public string Key;
            public List<int> Elements = new List<int>();
            public bool Dirty;
            public readonly Dictionary<int, (MeshFilter Filter, MeshRenderer Renderer)> Chunks = new Dictionary<int, (MeshFilter, MeshRenderer)>();
        }

        public int Count => _model?.Elements.Count ?? 0;
        public Bounds ModelBounds => _model?.Bounds ?? default;
        public int ChunkRendererCount => CountChunks(onlyEnabled: true);
        public int RebuildCount { get; private set; }

        public Camera Camera
        {
            get => _camera;
            set => _camera = value;
        }

        public void Build(StructureModel model, Func<Element, Material> initialMaterial)
        {
            Clear();
            _model = model ?? throw new ArgumentNullException(nameof(model));
            int count = model.Elements.Count;
            _meshes = new MeshData[count];
            _poses = new MemberPose[count];
            _colliders = new Collider[count];
            _groupOf = new int[count];
            _visible = new bool[count];
            _materialOf = new int[count];

            _root = new GameObject("Structure").transform;
            _root.SetParent(transform, false);
            var pickers = new GameObject("Pickers").transform;
            pickers.SetParent(_root, false);

            var groupIndex = new Dictionary<string, int>();
            foreach (var element in model.Elements)
            {
                int i = element.Index;
                BuildGeometry(element);
                _colliders[i] = CreatePicker(element, pickers);
                _visible[i] = true;
                _materialOf[i] = MaterialId(initialMaterial(element));

                // Ungrouped elements get a chunk of their own.
                string key = string.IsNullOrEmpty(element.Info.Group) ? "#" + i : element.Info.Group;
                if (!groupIndex.TryGetValue(key, out int g))
                {
                    g = _groups.Count;
                    groupIndex.Add(key, g);
                    _groups.Add(new GroupState { Key = key });
                }
                _groups[g].Elements.Add(i);
                _groupOf[i] = g;
            }

            foreach (var group in _groups)
                group.Dirty = true;
            _anyDirty = true;
            RebuildDirtyGroups();
        }

        public void Clear()
        {
            if (_root != null)
            {
                // Destroy is deferred to the end of the frame; hide the old structure (and its colliders) right away.
                _root.gameObject.SetActive(false);
                DestroyOwned(_root.gameObject);
            }
            foreach (var mesh in _ownedMeshes)
                DestroyOwned(mesh);
            _ownedMeshes.Clear();
            _groups.Clear();
            _materials.Clear();
            _materialIds.Clear();
            _boxCache.Clear();
            _model = null;
            _root = null;
            _anyDirty = false;
            RebuildCount = 0;
        }

        public void SetVisible(int index, bool visible)
        {
            if (_visible[index] == visible)
                return;
            _visible[index] = visible;
            _colliders[index].enabled = visible;
            MarkDirty(index);
        }

        public void SetMaterial(int index, Material material)
        {
            int id = MaterialId(material);
            if (_materialOf[index] == id)
                return;
            _materialOf[index] = id;
            MarkDirty(index);
        }

        public Bounds GetWorldBounds(int index) => _model.Elements[index].Bounds;

        // Read back the requested state (end-to-end tests).
        public bool IsVisible(int index) => _visible[index];
        public Material MaterialOf(int index) => _materials[_materialOf[index]];

        public bool TryPick(Vector2 screenPosition, out PickHit hit)
        {
            hit = default;
            if (_model == null || _camera == null)
                return false;

            var ray = _camera.ScreenPointToRay(screenPosition);
            int count = Physics.RaycastNonAlloc(ray, _hits, MaxPickDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                // Only our own pick colliders count; anything else in the scene is ignored.
                if (_hits[i].distance >= best || !_hits[i].collider.TryGetComponent(out ElementHandle handle) || handle.Owner != this)
                    continue;
                best = _hits[i].distance;
                hit = new PickHit(handle.Index, _hits[i].point);
                found = true;
            }
            return found;
        }

        // Applies pending visibility/material changes now instead of waiting for LateUpdate.
        public void RebuildDirtyGroups()
        {
            if (!_anyDirty)
                return;
            _anyDirty = false;
            foreach (var group in _groups)
            {
                if (group.Dirty)
                    Rebuild(group);
            }
        }

        private void LateUpdate() => RebuildDirtyGroups();

        private void OnDestroy() => Clear();

        private void BuildGeometry(Element element)
        {
            int i = element.Index;
            switch (element.Kind)
            {
                case ElementKind.Member:
                    var member = element.Member;
                    _poses[i] = MemberGeometry.ComputePose(member.Start, member.End, member.Roll);
                    _meshes[i] = BoxMesh(member.Section, _poses[i].Length);
                    break;
                case ElementKind.Panel:
                    _poses[i] = MemberPose.Identity;
                    _meshes[i] = PanelMeshBuilder.Build(element.Panel.Corners, element.Panel.Thickness);
                    break;
                default:
                    _poses[i] = MemberPose.Identity;
                    _meshes[i] = SlabMeshBuilder.Build(element.Slab.Outline, element.Slab.Top, element.Slab.Thickness);
                    break;
            }
        }

        // Members with the same section and length (to the millimetre) share mesh data.
        private MeshData BoxMesh(Section section, float length)
        {
            long key = ((long)Mathf.RoundToInt(section.Width * 1000f) << 42)
                       | ((long)Mathf.RoundToInt(section.Depth * 1000f) << 21)
                       | (long)Mathf.RoundToInt(length * 1000f);
            if (!_boxCache.TryGetValue(key, out var mesh))
            {
                mesh = BoxMeshBuilder.Build(section.Width, section.Depth, length);
                _boxCache.Add(key, mesh);
            }
            return mesh;
        }

        private Collider CreatePicker(Element element, Transform parent)
        {
            var go = new GameObject(element.Info.Id);
            go.transform.SetParent(parent, false);
            go.AddComponent<ElementHandle>().Init(this, element.Index);

            if (element.Kind == ElementKind.Member)
            {
                var pose = _poses[element.Index];
                go.transform.SetPositionAndRotation(pose.Position, pose.Rotation);
                var box = go.AddComponent<BoxCollider>();
                box.size = new Vector3(element.Member.Section.Width, element.Member.Section.Depth, pose.Length);
                return box;
            }

            var mesh = CreateMesh(element.Info.Id);
            Fill(mesh, _meshes[element.Index]);
            var meshCollider = go.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
            return meshCollider;
        }

        private void Rebuild(GroupState group)
        {
            group.Dirty = false;
            RebuildCount++;
            var chunks = ChunkPlanner.Plan(group.Elements, _visible, _materialOf);

            foreach (var chunk in group.Chunks.Values)
                chunk.Renderer.enabled = false;

            foreach (var chunk in chunks)
            {
                if (!group.Chunks.TryGetValue(chunk.MaterialId, out var target))
                {
                    target = CreateChunk(group.Key, chunk.MaterialId);
                    group.Chunks.Add(chunk.MaterialId, target);
                }

                _scratch.Clear();
                foreach (int element in chunk.Elements)
                    MeshCombiner.Append(_scratch, _meshes[element], _poses[element]);
                Fill(target.Filter.sharedMesh, _scratch);
                target.Renderer.enabled = true;
            }
        }

        private (MeshFilter, MeshRenderer) CreateChunk(string groupKey, int materialId)
        {
            var go = new GameObject($"{groupKey} [{_materials[materialId].name}]");
            go.transform.SetParent(_root, false);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = CreateMesh(go.name);
            filter.sharedMesh.MarkDynamic();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _materials[materialId];
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return (filter, renderer);
        }

        private int MaterialId(Material material)
        {
            if (material == null)
                throw new ArgumentNullException(nameof(material));
            if (!_materialIds.TryGetValue(material, out int id))
            {
                id = _materials.Count;
                _materials.Add(material);
                _materialIds.Add(material, id);
            }
            return id;
        }

        private void MarkDirty(int index)
        {
            _groups[_groupOf[index]].Dirty = true;
            _anyDirty = true;
        }

        private Mesh CreateMesh(string name)
        {
            var mesh = new Mesh { name = name };
            _ownedMeshes.Add(mesh);
            return mesh;
        }

        private static void Fill(Mesh mesh, MeshData data)
        {
            mesh.Clear();
            mesh.indexFormat = data.Vertices.Length > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(data.Vertices);
            mesh.SetNormals(data.Normals);
            mesh.SetUVs(0, data.Uvs);
            mesh.SetTriangles(data.Triangles, 0);
            mesh.RecalculateBounds();
        }

        private static void Fill(Mesh mesh, MeshDataBuilder data)
        {
            mesh.Clear();
            mesh.indexFormat = data.Vertices.Count > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(data.Vertices);
            mesh.SetNormals(data.Normals);
            mesh.SetUVs(0, data.Uvs);
            mesh.SetTriangles(data.Triangles, 0);
            mesh.RecalculateBounds();
        }

        private int CountChunks(bool onlyEnabled)
        {
            int count = 0;
            foreach (var group in _groups)
            {
                foreach (var chunk in group.Chunks.Values)
                {
                    if (!onlyEnabled || chunk.Renderer.enabled)
                        count++;
                }
            }
            return count;
        }

        // Tests and editor tooling run without play mode, where Destroy is not allowed.
        private static void DestroyOwned(UnityEngine.Object target)
        {
            if (UnityEngine.Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }
    }
}
