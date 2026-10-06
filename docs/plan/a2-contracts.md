# A2 — Contracts, fixtures & fakes

**Goal**: every type that crosses a feature boundary, defined once, so all B phases can be built and tested without each other.
**Estimate**: 1.5 h · **Needs**: A1

Interfaces here are seams for tests or platform boundaries (allowed by CLAUDE.md "Avoid over-engineering"). Keep them minimal — add members only when a B phase needs them, via a small A2 change.

## Domain types (`Scripts/Domain/`)
- [x] `Structure/ElementCategory` (`Wall, Floor, Roof, Sheathing, Slab`), `ElementKind` (`Member, Panel, Slab`).
- [x] `Structure/Level` (id, name, elevation m, index), `Section` (width m, depth m, `Area`).
- [x] `Structure/ElementInfo` (id, category, type, group, levelIndex).
- [x] `Structure/Member` (start, end — Unity space, metres; roll°; section; material; `Length`), `Panel` (4 corners, thickness, `Area`), `Slab` (outline, top, thickness).
- [x] `Structure/Element` (index, kind, info, member/panel/slab).
- [x] `Structure/StructureModel`: name, levels, `Elements`, `Bounds`, `ElementsInGroup(group)`, `ElementsInLevel(i)`, `Groups`. Elements are addressed by **int index** everywhere.
- [x] `Structure/ModelAxes`: Unity (Y-up, m) ↔ model (Z-up, mm).
- [x] `Visibility/IVisibility` (`bool IsVisible(int)`) + `AllVisible`.
- [x] `Selection/SelectionKind` (`None, Member, Assembly, Multi`), `SelectionSnapshot` (indices, kind, `Contains`).
- [x] `Display/DisplayMode` (`Realistic, ColorBy, XRay, Clay`), `ColorByField` (`Category, Type, Level`).
- [x] `Interaction/InteractionMode` (`Select, Measure`).

## Events (`Scripts/Application/Events/`) — `readonly struct`
`StructureLoaded(StructureModel)`, `SelectionChanged(SelectionSnapshot)`, `HoverChanged(int index)` (−1 = none), `VisibilityChanged(IVisibility)`, `DisplayModeChanged(DisplayMode, ColorByField)`, `InteractionModeChanged(InteractionMode)`.

## Application contracts
- [x] `Loading/IStructureParser` → `ParseResult { Model, Errors, Success }`.
- [x] `Loading/StructureSession` — holds the current model (set by the load use case, read by features).

## Infrastructure contracts
- [x] `Parsing/StructureDto.cs` — `[Serializable]` DTOs exactly matching the DESIGN.md schema (flat arrays). Shared by B01 (parser) and B02 (generator).

## Presentation contracts (`Scripts/Presentation/Contracts/`)
- [x] `IStructureRenderer`: `Count`, `SetVisible(i, bool)`, `SetMaterial(i, Material)`, `GetWorldBounds(i)`, `TryPick(Vector2 screen, out PickHit)` (`PickHit`: index, world point), `ModelBounds`.
- [x] `IPointerEvents`: `Tapped(TapEvent)` (screen pos, isDouble, additive, device), `Hovered(Vector2)` (mouse only), `Orbited(Vector2)`, `Panned(Vector2)`, `Zoomed(float, Vector2)`, `PointerDevice Current`. `PointerDevice` = `Mouse, Touch` (not `PointerType`: clashes with `UnityEngine.PointerType`).
- [x] `ICameraControl`: `Camera`, `FitAll(Bounds)`, `Focus(Bounds)`.
- [x] `IShell`: `LeftSlot`, `RightSlot`, `BottomSlot`, `IsCompact`, `CompactChanged`, `ShowSheet(VisualElement, title)`, `HideSheet()`, `AddToolbarItem(ToolbarItem)`, `SetToolbarItemState(id, active, enabled)`, `ShowToast(string)`.
- [x] `RenderingConfig` ScriptableObject **class** (asset is created in B04): wood, concrete, sheathing (transparent), flat-opaque template, flat-transparent template, overlay material, grid material.
- [x] `UI/Theme.uss` — design tokens only (colours, spacing, 44 px touch target, font sizes); every panel imports it.

## Test fixtures (`Tests/Fixtures/`)
- [x] `TestStructures.MiniHouse()` — code-built model (~25 elements): 2 levels; 2 walls (one with an opening), incl. a **vertical stud**; 1 truss with a **sloped chord**; 2 joists; 1 roof panel; 1 slab; at least 2 groups per category. Plus `TestStructures.Empty()`.
- [x] Fakes: `FakeStructureRenderer` (records visibility/material per index, scripted picks/bounds), `FakePointerEvents` (raise methods), `FakeCameraControl` (records calls), `FakeShell` (records toolbar items/sheets), `FakeVisibility` (hidden set), `EventRecorder<T>` (captures bus events).

## Implemented extras (used by several B phases)
- `StructureModel.IndexOf(id)`; `Element.Bounds` (world AABB: exact for slabs, roll-independent conservative for members, padded both sides for panels).
- `Member.Length/Midpoint/Volume`, `Panel.Area/Normal`, `Slab.Area/Volume`, `Section.Label` (`"35 × 90"`).
- `SelectionSnapshot.Empty`, `SameAs(other)` (for "publish only on real change"); duplicate indices are dropped.
- `ParseResult.Ok/Fail`; `IShell.ShowToast(message, isError = false)`; `ToolbarItem(id, label, glyph, tooltip, onClick, isToggle, priority)`.
- `TestStructures` exposes ids/groups as constants (`VerticalStudId`, `SlopedChordId`, `NorthWall`…) — look elements up with `IndexOf`, never hard-code indices.
- `MiniHouse` is 27 elements, levels `L0`/`L1` (roof elements sit on `L1`).

## Tests
- `StructureModel` lookups (group/level membership, bounds) and `ModelAxes` round-trip — the only logic in this phase.

## Done when
- Everything compiles, fixtures/fakes are usable from both test assemblies, lookup tests green.
- Any later change to this phase is called out in its commit (it affects every B phase).
