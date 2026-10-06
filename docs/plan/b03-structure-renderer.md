# B03 — Geometry & structure renderer

**Goal**: `StructureModel` → combined per-assembly meshes (wood grain follows each member) plus per-element pick colliders, implementing `IStructureRenderer`.
**Estimate**: 2.5 h · **Needs**: A2
**Owns**: `Scripts/Domain/Geometry/`, `Scripts/Presentation/Structure/`, `Tests/EditMode/Geometry/`, `Tests/PlayMode/Structure/`

## Tasks

### Pure geometry (Domain)
- [ ] `MemberGeometry.ComputePose(start, end, roll)` → position, rotation, length. `refUp` = world up, or world forward when |dot(dir, up)| > 0.999.
- [ ] `MeshData` (vertices, normals, uv, triangles arrays).
- [ ] `BoxMeshBuilder.Build(width, depth, length)` — 24 verts, local Z = axis, **UV in metres** (V along length).
- [ ] `PanelMeshBuilder.Build(corners, thickness)` — extruded quad, outward normals.
- [ ] `SlabMeshBuilder.Build(outline, top, thickness)` — convex fan + sides.
- [ ] `MeshCombiner.Append(target, source, pose)` — appends a `MeshData` transformed by a pose (positions + normals), keeps UVs; switches to 32-bit indices past 65 535 vertices.

### Renderer (Presentation)
- [ ] `ChunkPlanner` (pure): given each element's group, visibility and material id → the set of `(group, material)` chunks and their element lists. Unit-tested; the renderer only turns chunks into meshes.
- [ ] `StructureRenderer : MonoBehaviour, IStructureRenderer` — `Build(model, defaultMaterial)`, `Clear()`; one renderer GameObject per `(group, material)` chunk under a root; per-element collider-only GameObjects (members `BoxCollider`, panels/slab `MeshCollider`) with an `ElementHandle` holding the index; element `MeshData` cached by `(w, d, len)` rounded to 1 mm.
- [ ] `SetVisible` / `SetMaterial` skip no-op calls and mark the element's group dirty (collider enabled state follows visibility); dirty groups rebuild once in `LateUpdate`, reusing `Mesh` objects; unused chunk renderers are disabled, not destroyed.
- [ ] `TryPick` raycasts against own colliders only (layer mask); `GetWorldBounds` from element bounds.
- [ ] `Clear()` destroys spawned objects **and** runtime meshes.

## Tests
**EditMode**: pose — horizontal, sloped, **vertical stud** (no NaN, width axis horizontal), roll 90°; box — 24 verts/36 indices, bounds = w × d × len, V range [0, len], outward normals; panel — normals outward after the handedness swap; slab — closed, outward normals; `MeshCombiner` — vertex/index counts add up, transformed positions/normals, index offsets; `ChunkPlanner` — one chunk per (group, material), hidden elements excluded, material change moves an element between chunks, empty groups produce no chunk.
**PlayMode** (with `TestStructures.MiniHouse()`): renderer count = chunk count (≪ element count); collider count = element count; `TryPick` at a member's projected centre returns its index; `SetVisible(false)` removes only that element's triangles and disables its collider; `SetMaterial` on one element splits its group into two chunks; one rebuild per group per frame; `Clear` leaves no objects/meshes.

## Hand-off to C
- C1: `AppBootstrap` calls `Build(model, RenderingConfig.wood)` on `StructureLoaded`; passes the renderer as `IStructureRenderer` to features.
