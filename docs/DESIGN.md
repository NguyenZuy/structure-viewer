# Structure Viewer — Design

A WebGL viewer for 3D timber-framed buildings (wall frames, floor framing, roof trusses, slab, sheathing). Loads a JSON description of a building and lets the user explore it: orbit, toggle layers, isolate, inspect members and assemblies, measure, and read a material takeoff.

Portfolio demo. Unity only, no backend. Built for WebGL, embedded in a portfolio website, **must work well on PC and mobile browsers**.

Visual reference: timber framing plugins / framing previews — real wood-grain studs on a concrete slab, translucent roof/wall sheathing, whole wall highlighted when selected, assembly labels.

## Scope

**In (MVP, priority order)**
1. Load structure from JSON → build members, slab, panels
2. Camera: orbit / pan / zoom / focus / fit all — mouse and touch
3. Selection (member + whole assembly) + info panel
4. Visibility: category layers, level filter, isolate, hide, show all — with undo/redo
5. Display modes: Realistic, Color by (Category / Type / Level), X-ray, Clay — **all four are required**
6. Measure tool with endpoint/midpoint snapping
7. Material takeoff table
8. Assembly labels (`W-N1`, `T03`), toggleable

**Out**: backend/database, comments, loading user JSON files, hip roofs, section plane, exploded view, wireframe mode, realtime shadows/SSAO, IFC or other real formats, editing members, outline shader, CSV export.

**Stretch (only if time allows)**: exploded view, human scale figure, FPS/draw-call overlay.

**Cut order if behind schedule**: assembly labels → takeoff "visible only" toggle → midpoint snapping. Display modes are not cut.

## Data

Sample data is a hypothetical two-storey timber house produced by an editor generator (`Tools > Structure Viewer > Generate Sample House`). It is not engineered or code-compliant — say so in the README.

Sample house (American colonial / farmhouse style, revised 2026-10-07):
- 12 × 8.4 m footprint, two storeys (2.7 m each), **gable roof** (identical Fink trusses @ 600 mm, 35° pitch, 500 mm eaves, 400 mm gable overhang) with roof sheathing on both slopes.
- Concrete slab, external walls on both levels, one internal wall per level (upstairs hall door), first-floor joists on a bearer.
- **Symmetric front**: centred front door, two windows each side downstairs, five aligned windows upstairs; windows on the back and both sides of each storey, a back door. Each opening has a lintel, trimmer studs either side and a sill (windows), with cripple studs above/below.
- **Covered front porch**: concrete pad, four posts and a beam, lean-to roof (15°) of rafters on a ledger fixed to the front wall, with its own sheathing.
- **Envelope**: external wall sheathing on both storeys cut around every opening, closed gable ends, first-floor decking. Wall sheathing is grouped with its wall frame (selecting/isolating a wall includes it). Hide the `Sheathing` layer to see the bare frame.
- **Doors and windows**: every opening is filled by a door leaf (40 mm) or a glazing pane (6 mm), centred in the wall depth and grouped with its wall. Windows have a colonial 6-over-6 muntin grid (3 or 4 columns, deeper meeting rail); every external opening has a 90 mm white casing on the sheathing. Category `Opening` ("Doors & windows" layer). ~1000 elements in total.
- Studs @ 600 mm, noggings mid-height, top + bottom plates. ~700 members, ~85 panels, 2 slabs.

Only this built-in sample is loaded — no user file loading. Stored as a `TextAsset` (`Assets/_Project/Data/sample-house.json`), parsed with `JsonUtility`. No `StreamingAssets`/web requests needed.

```json
{
  "name": "Sample House",
  "units": "mm",
  "upAxis": "Z",
  "levels": [
    { "id": "L0", "name": "Ground Floor", "elevation": 0 },
    { "id": "L1", "name": "First Floor", "elevation": 2700 },
    { "id": "RF", "name": "Roof", "elevation": 5400 }
  ],
  "members": [
    {
      "id": "T03-TC1",
      "category": "Roof",
      "type": "TrussTopChord",
      "group": "T03",
      "level": "RF",
      "start": [0, 3600, 5400],
      "end": [4200, 3600, 7000],
      "roll": 0,
      "section": { "width": 35, "depth": 90 },
      "material": "MGP10"
    }
  ],
  "slabs": [
    { "id": "SLAB-1", "level": "L0", "outline": [0,0, 10000,0, 10000,8000, 0,8000], "top": 0, "thickness": 300 }
  ],
  "panels": [
    {
      "id": "RS-N", "category": "Sheathing", "type": "RoofSheathing", "group": "ROOF-N", "level": "RF",
      "corners": [0,0,5400, 10000,0,5400, 10000,4000,7000, 0,4000,7000], "thickness": 12
    }
  ]
}
```

| Field | Notes |
|---|---|
| `category` | `Wall` / `Floor` / `Roof` / `Sheathing` / `Slab` / `Opening` — drives layer toggles. |
| `type` | Display subtype: `Stud`, `TrimmerStud`, `CrippleStud`, `TopPlate`, `BottomPlate`, `Nogging`, `Lintel`, `Sill`, `Joist`, `Bearer`, `TrussTopChord`, `TrussBottomChord`, `TrussWeb`, `RoofSheathing`, `WallSheathing`, `FloorSheathing`, `Door`, `Window`, `Casing`, `Muntin`. |
| `group` | Assembly id (`W-N1`, `T03`) — used for assembly selection, isolate and labels. |
| `start`/`end` | Member centreline endpoints, mm, Z-up. |
| `roll` | Degrees around the member axis (optional, default 0). Two points alone don't fix the orientation of a rectangular section. |
| `section` | `width` × `depth` in mm. Width lies on local X, depth on local Y. |
| `slabs[].outline` | Plan polygon as a **flat** array `x0,y0, x1,y1, …` in mm, extruded down from `top` by `thickness`. Convex only (rectangle for the sample). |
| `panels[].corners` | 4 coplanar corners as a **flat** array `x0,y0,z0, …` (planar quad), extruded by `thickness` along the normal. |

Point lists are flat because `JsonUtility` can't deserialize nested arrays.

### Coordinate conversion (Infrastructure)
- File is right-handed Z-up in mm; Unity is left-handed Y-up in metres: `unity = (x, z, y) / 1000`. The axis swap also fixes handedness (watch the panel winding order).
- Member transform: centre = midpoint, length = |end − start|, rotation = `LookRotation(dir, refUp)` then `roll` about `dir`. `refUp` = world up, or world forward when the member is (near) vertical — **studs are vertical, this case must be tested**.
- Reject zero-length members, non-planar/degenerate panels and unknown level references with a clear parse error.

## Rendering

- **Members use generated box meshes, not scaled cubes**: UVs are in metres along the member axis, so wood grain follows each member and never stretches. Mesh generation is pure C# (`BoxMeshBuilder`) and unit-tested. Meshes are cached per `(width, depth, length)`.
- One shared material per look (wood, slab, sheathing, highlight-member, highlight-assembly, hover) → SRP Batcher compatible. Highlight = swap shared material. **No `MaterialPropertyBlock`** (breaks SRP Batcher).
- Wood: URP Lit + CC0 wood albedo/normal from ambientCG (1K, compressed). Concrete slab from ambientCG too. Credit source + licence in README; textures live in `Assets/_Project/Art/Textures/`. Slab: concrete/brick-red tint. Sheathing: URP Lit Transparent, translucent violet, rendered after opaques.
- **Combined meshes per assembly**: each `group` is drawn as one mesh per material in use (`(group, material)` chunk), built from the per-element meshes transformed to world space. ~800 elements → a few dozen draw calls. `SetVisible`/`SetMaterial` only mark the group dirty; dirty groups are rebuilt once per frame (reusing their `Mesh` objects), so selecting a whole assembly costs one rebuild. Elements without a group form their own chunk.
- Picking stays per element: one collider-only GameObject per element (`BoxCollider` for members, `MeshCollider` for panels/slab, convex off, static), no renderer.
- **Lighting: no realtime shadows, no SSAO.** Bright gradient ambient + one directional light (shadows off). Depth comes from the wood texture, normal map, slab edge and a faint ground grid.
- Measure line uses a tiny unlit `ZTest Always` shader so it is always visible.
- **Performance**: the B08 spike held ~60 FPS on a Xiaomi 14T with 1600 separate renderers, but mesh combining is used anyway (decision 2026-10-07) for headroom on weaker phones and fewer draw calls.

## Features

### 1. Camera
| Action | Mouse | Touch |
|---|---|---|
| Orbit around pivot | LMB drag (beyond threshold) | 1-finger drag |
| Pan | RMB / MMB drag | 2-finger drag |
| Zoom toward pointer | Scroll | Pinch |
| Focus selection | `F` / toolbar button | toolbar button |
| Fit all | `Home` / toolbar button | toolbar button |

A press that doesn't exceed the drag threshold is a click/tap (selection). Threshold scales with DPI. Damped motion, clamped pitch, min/max distance.

### 2. Selection + info panel
| Action | Mouse | Touch |
|---|---|---|
| Select member | Click | Tap |
| Select whole assembly (`group`) | Double-click | Double-tap |
| Add/remove from selection | `Ctrl`+click | "Multi-select" toggle in toolbar, then tap |
| Clear | Click empty / `Esc` | Tap empty |

- Selected member: orange tint. Selected assembly: whole group tinted blue (as in the reference). Hover tint on desktop only (never required for any information).
- Single member: id, type, category, level, group, section (`35 × 90`), material, length (mm).
- Assembly / multi-selection: count, total length, breakdown by type.
- Selection is **not** undoable. Hidden members are deselected.
- Mobile: info panel is a bottom sheet; desktop: right side panel.

### 3. Visibility
Single source of truth `VisibilityState`:

```
visible(e) = categoryOn[e.category] && levelOn[e.level]
          && (isolated is empty || e ∈ isolated)
          && e ∉ hidden
```

| Action | Input | Command |
|---|---|---|
| Toggle category | Layer panel | `SetCategoryVisibleCommand` |
| Toggle level | Layer panel | `SetLevelVisibleCommand` |
| Isolate selection / level / group | `I`, toolbar, panels | `IsolateCommand` |
| Hide selection | `H`, toolbar | `HideCommand` |
| Show all (reset isolate + hidden, all layers on) | `Shift+H`, toolbar | `ShowAllCommand` |
| Undo / redo | `Ctrl+Z` / `Ctrl+Y`, toolbar | `CommandHistory` |

Every command stores the full previous `VisibilityState` snapshot, so `Undo` restores it exactly. Toolbar shows a badge when isolate is active.

### 4. Display modes
Switch via the toolbar "Display" dropdown (mobile: overflow menu) or keys `1`–`4`. Switching is a view setting: **not undoable, never changes visibility, selection or camera**. Default: Realistic.

| Mode | Members | Slab | Sheathing | Selection highlight |
|---|---|---|---|---|
| **1. Realistic** | Wood albedo + normal (URP Lit) | Concrete texture | Per type, mostly opaque (α ≈ 0.9) so the house reads as enclosed with the frame faintly visible: walls white house wrap, roofs charcoal shingle tone, floors OSB. Doors opaque colonial red, casings and muntins white trim; glazing dark blue-grey (α ≈ 0.8) on a glossy Glass template with preserved specular | Member: wood tinted orange. Assembly: wood tinted blue |
| **2. Color by** `Category` / `Type` / `Level` | Flat colour per key (URP Lit, low smoothness, no texture) | Colour of its key | Colour of its key, translucent (α ≈ 0.35) | Flat orange / blue |
| **3. X-ray** | Translucent pale blue-grey (α ≈ 0.12, no depth write) | Same, α ≈ 0.08 | Same, α ≈ 0.08 | **Opaque** orange / blue, so the selection pops out of the ghosted model |
| **4. Clay** | Matte off-white `#E8E6E1` | Slightly darker `#CFCBC4` | Translucent white (α ≈ 0.25) | Flat orange / blue |

**Color by details**
- Sub-selector `Category | Type | Level` next to the mode dropdown (default `Type`, the most informative).
- Fixed palette per key, defined in a `DisplayPaletteAsset` ScriptableObject (tweakable without code). Same key → same colour every session. Type colours are grouped by family (wall types = blues, floor = greens, roof = reds/oranges, sheathing = violet) so ~16 types stay readable. Orange/blue hues are avoided in the palette because they are reserved for selection.
- **Legend panel** visible only in this mode: one row per key present in the model (swatch, name, count). Hidden keys are greyed out. Read-only.
- Takeoff table rows show the matching colour swatch when grouping matches the colour key.

**Precedence**: selection highlight > hover (desktop) > display mode colour.

**Rendering notes**
- Every mode is a set of **shared** materials (created once, cached by `(mode, key, highlight state)`) → stays SRP Batcher friendly, no new shaders.
- X-ray uses many transparent objects → check overdraw/FPS on mobile; acceptable because all ghosted elements share one colour, so sort-order artefacts are invisible.

### 5. Assembly labels
- One label per `group` at the assembly's top centre, screen-projected UI Toolkit labels. Hidden when the assembly is hidden or behind the camera; fade with distance; overlapping labels are decluttered every frame (nearer and selected ones win); toggle in toolbar. Off by default on mobile.

### 6. Measure
- Toggle with `M` or toolbar. Click/tap point A, then point B → line + label with distance (mm) and |dx|, |dy|, |dz|.
- Snap to the hit member's endpoints or midpoint when within the snap radius (≈12 px mouse, ≈28 px touch, DPI-scaled), with a snap marker; otherwise use the raw hit point.
- One measurement at a time; `Esc` / toolbar clears or exits.

### 7. Material takeoff
- Table grouped by `type + section + material`: count, total length (m), volume (m³). Totals row. Panels report area (m²) instead of length.
- Toggle "visible only" (default off). Recomputes on `VisibilityChanged`.
- Desktop: dockable panel. Mobile: full-screen sheet.

## UI layout
- **Desktop**: top toolbar (icons + tooltips), left layer panel, right info panel, takeoff as a collapsible bottom panel.
- **Mobile (portrait/landscape)**: compact top toolbar with overflow menu, layers/takeoff as sheets, info as a bottom sheet. Touch targets ≥ 44 px, safe areas respected.
- UI Toolkit (UXML + USS) with one USS breakpoint switch driven by screen width. Dark neutral panels (`#1E1F22`), orange accent for selection/measure, blue for assembly selection.

## Architecture mapping

| Layer | Contents |
|---|---|
| Domain | `StructureModel`, `Member`, `Panel`, `Slab`, `Level`, `Section`, `ElementCategory`, `VisibilityState`, `TakeoffCalculator`, `MeasureSnapper`, `MemberGeometry`, `BoxMeshBuilder` (mesh data only), `DisplayMode`, `ColorByField`, `ColorKeyResolver` + legend builder (element → key, keys present + counts) |
| Application | `LoadStructureUseCase`, `SelectionService`, `DisplaySettings` (current mode + colour field), visibility commands, `CommandHistory`, `EventBus`, events: `StructureLoaded`, `SelectionChanged`, `VisibilityChanged`, `DisplayModeChanged` |
| Presentation | Presenter + View per panel: `Toolbar`, `LayerPanel`, `InfoPanel`, `LegendPanel`, `TakeoffPanel`, `Measure`, `Labels`. `StructureView` (spawns elements, applies visibility/highlight/materials, raycast → element id). Display modes are strategies: `IElementPalette` with `RealisticPalette`, `ColorByPalette`, `XRayPalette`, `ClayPalette` resolving `(element, highlight state) → shared Material`; adding a mode = adding a class. `PointerInput` adapter turns mouse/touch into click / double-click / drag / pinch. `CameraController` is a plain MonoBehaviour fed by `PointerInput`. |
| Infrastructure | `JsonStructureParser` (DTOs + coordinate conversion), `QualitySelector` (PC vs mobile URP asset) |
| Bootstrap | `AppBootstrap` wires everything; editor menu `Tools > Structure Viewer > Setup Scene` creates the scene, `PanelSettings` and references so setup is reproducible |

## Tests

**EditMode**
- Parser: mm→m, axis swap, missing/unknown level, zero-length member, degenerate panel, malformed JSON.
- `MemberGeometry`: horizontal, sloped, vertical (stud), roll 90°.
- `BoxMeshBuilder`: vertex/triangle counts, bounds match section × length, UV V-range = length in metres, outward normals.
- `VisibilityState`: each rule and combinations (isolate + category off, hide inside isolate, show all).
- Commands: execute → undo restores the exact snapshot; redo; new command clears redo.
- `SelectionService`: member vs assembly selection, toggle, hidden members get deselected.
- `TakeoffCalculator`: grouping, totals, panel area, visible-only.
- `ColorKeyResolver` + legend: correct key per field for members, panels and slab; legend lists only present keys with correct counts in stable order; hidden keys flagged.
- Palette precedence: selection > hover > mode colour; X-ray selected element resolves to an opaque material.
- `MeasureSnapper`: snaps within radius, picks nearest candidate, falls back to raw hit.
- Pointer gesture classification: tap vs drag threshold, double-tap window, pinch scale.
- `EventBus`: delivery, dispose unsubscribes.
- Presenters with fake views: `LayerPanel` toggle dispatches command; `InfoPanel` member vs assembly.

**PlayMode**
- Bootstrap loads sample → element counts match, all visible.
- Toggle a category → matching GameObjects inactive; undo → active again.
- Raycast at a known member → selects the right id; double-click → selects its group.
- Switching through all 4 display modes keeps hidden elements hidden, keeps the selection highlighted, and leaves the camera untouched.

## WebGL / deploy
- Player: WebGL 2, Linear colour, Brotli + **Decompression Fallback on** (works on any static host without custom headers), Data Caching on, minimal template with mobile viewport meta.
- Both URP assets: shadows off, no SSAO. Mobile asset: lower render scale, MSAA off.
- Test the real build on desktop Chrome/Firefox/Safari and on an Android + iOS phone before calling it done.
- Embed in the portfolio via `<iframe>`; canvas fills the frame.
- README: GIF, controls (mouse + touch), architecture diagram, test summary, measured FPS/draw calls (real numbers, with device/browser), note that data is hypothetical and the project is not affiliated with any vendor.

## Plan

Phase-by-phase implementation plan: [docs/plan/](plan/README.md).
