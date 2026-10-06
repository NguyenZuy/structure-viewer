# B03 — Geometry & structure renderer

**Goal**: `StructureModel` → GameObjects with generated meshes (wood grain follows each member), implementing `IStructureRenderer`.
**Estimate**: 1.5 h (+1–2 h if the B08 gate requires mesh combining) · **Needs**: A2
**Owns**: `Scripts/Domain/Geometry/`, `Scripts/Presentation/Structure/`, `Tests/EditMode/Geometry/`, `Tests/PlayMode/Structure/`

## Tasks

### Pure geometry (Domain)
- [ ] `MemberGeometry.ComputePose(start, end, roll)` → position, rotation, length. `refUp` = world up, or world forward when |dot(dir, up)| > 0.999.
- [ ] `MeshData` (vertices, normals, uv, triangles arrays).
- [ ] `BoxMeshBuilder.Build(width, depth, length)` — 24 verts, local Z = axis, **UV in metres** (V along length).
- [ ] `PanelMeshBuilder.Build(corners, thickness)` — extruded quad, outward normals.
- [ ] `SlabMeshBuilder.Build(outline, top, thickness)` — convex fan + sides.

### Renderer (Presentation)
- [ ] `StructureRenderer : MonoBehaviour, IStructureRenderer` — `Build(model, defaultMaterial)`, `Clear()`; one GameObject per element under a root; members get `BoxCollider`, panels/slab `MeshCollider`; mesh cache keyed by `(w, d, len)` rounded to 1 mm; `ElementHandle` component holds the index.
- [ ] `SetVisible` / `SetMaterial` skip no-op calls; `TryPick` raycasts against own colliders only (layer mask).
- [ ] `Clear()` destroys spawned objects **and** runtime meshes.
- [ ] *If B08 gate fails*: combine per `group` into one mesh with per-vertex element index; `SetVisible`/`SetMaterial` rebuild/split the affected group.

## Tests
**EditMode**: pose — horizontal, sloped, **vertical stud** (no NaN, width axis horizontal), roll 90°; box — 24 verts/36 indices, bounds = w × d × len, V range [0, len], outward normals; panel — normals outward after the handedness swap; slab — closed, outward normals.
**PlayMode** (with `TestStructures.MiniHouse()`): spawned count = element count; `TryPick` at a member's projected centre returns its index; `SetVisible(false)` deactivates only that element; `Clear` leaves no objects/meshes; mesh cache reuses identical sections.

## Hand-off to C
- C1: `AppBootstrap` calls `Build(model, RenderingConfig.wood)` on `StructureLoaded`; passes the renderer as `IStructureRenderer` to features.
