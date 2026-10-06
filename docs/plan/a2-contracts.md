# A2 — Contracts, fixtures & fakes

**Goal**: every type that crosses a feature boundary, defined once, so all B phases can be built and tested without each other.
**Estimate**: 1.5 h · **Needs**: A1

Interfaces here are seams for tests or platform boundaries (allowed by CLAUDE.md "Avoid over-engineering"). Keep them minimal — add members only when a B phase needs them, via a small A2 change.

## Domain types (`Scripts/Domain/`)
- [ ] `Structure/ElementCategory` (`Wall, Floor, Roof, Sheathing, Slab`), `ElementKind` (`Member, Panel, Slab`).
- [ ] `Structure/Level` (id, name, elevation m, index), `Section` (width m, depth m, `Area`).
- [ ] `Structure/ElementInfo` (id, category, type, group, levelIndex).
- [ ] `Structure/Member` (start, end — Unity space, metres; roll°; section; material; `Length`), `Panel` (4 corners, thickness, `Area`), `Slab` (outline, top, thickness).
- [ ] `Structure/Element` (index, kind, info, member/panel/slab).
- [ ] `Structure/StructureModel`: name, levels, `Elements`, `Bounds`, `ElementsInGroup(group)`, `ElementsInLevel(i)`, `Groups`. Elements are addressed by **int index** everywhere.
- [ ] `Structure/ModelAxes`: Unity (Y-up, m) ↔ model (Z-up, mm).
- [ ] `Visibility/IVisibility` (`bool IsVisible(int)`) + `AllVisible`.
- [ ] `Selection/SelectionKind` (`None, Member, Assembly, Multi`), `SelectionSnapshot` (indices, kind, `Contains`).
- [ ] `Display/DisplayMode` (`Realistic, ColorBy, XRay, Clay`), `ColorByField` (`Category, Type, Level`).
- [ ] `Interaction/InteractionMode` (`Select, Measure`).

## Events (`Scripts/Application/Events/`) — `readonly struct`
`StructureLoaded(StructureModel)`, `SelectionChanged(SelectionSnapshot)`, `HoverChanged(int index)` (−1 = none), `VisibilityChanged(IVisibility)`, `DisplayModeChanged(DisplayMode, ColorByField)`, `InteractionModeChanged(InteractionMode)`.

## Application contracts
- [ ] `Loading/IStructureParser` → `ParseResult { Model, Errors, Success }`.
- [ ] `Loading/StructureSession` — holds the current model (set by the load use case, read by features).

## Infrastructure contracts
- [ ] `Parsing/StructureDto.cs` — `[Serializable]` DTOs exactly matching the DESIGN.md schema (flat arrays). Shared by B01 (parser) and B02 (generator).

## Presentation contracts (`Scripts/Presentation/Contracts/`)
- [ ] `IStructureRenderer`: `Count`, `SetVisible(i, bool)`, `SetMaterial(i, Material)`, `GetWorldBounds(i)`, `TryPick(Vector2 screen, out PickHit)` (`PickHit`: index, world point), `ModelBounds`.
- [ ] `IPointerEvents`: `Tapped(TapEvent)` (screen pos, isDouble, additive, pointerType), `Hovered(Vector2)` (mouse only), `Orbit(Vector2)`, `Pan(Vector2)`, `Zoom(float, Vector2)`, `PointerType Current`. `PointerType` = `Mouse, Touch`.
- [ ] `ICameraControl`: `Camera`, `FitAll(Bounds)`, `Focus(Bounds)`.
- [ ] `IShell`: `LeftSlot`, `RightSlot`, `BottomSlot`, `IsCompact`, `CompactChanged`, `ShowSheet(VisualElement, title)`, `HideSheet()`, `AddToolbarItem(ToolbarItem)`, `SetToolbarItemState(id, active, enabled)`, `ShowToast(string)`.
- [ ] `RenderingConfig` ScriptableObject **class** (asset is created in B04): wood, concrete, sheathing (transparent), flat-opaque template, flat-transparent template, overlay material, grid material.
- [ ] `UI/Theme.uss` — design tokens only (colours, spacing, 44 px touch target, font sizes); every panel imports it.

## Test fixtures (`Tests/Fixtures/`)
- [ ] `TestStructures.MiniHouse()` — code-built model (~25 elements): 2 levels; 2 walls (one with an opening), incl. a **vertical stud**; 1 truss with a **sloped chord**; 2 joists; 1 roof panel; 1 slab; at least 2 groups per category. Plus `TestStructures.Empty()`.
- [ ] Fakes: `FakeStructureRenderer` (records visibility/material per index, scripted picks/bounds), `FakePointerEvents` (raise methods), `FakeCameraControl` (records calls), `FakeShell` (records toolbar items/sheets), `FakeVisibility` (hidden set), `EventRecorder<T>` (captures bus events).

## Tests
- `StructureModel` lookups (group/level membership, bounds) and `ModelAxes` round-trip — the only logic in this phase.

## Done when
- Everything compiles, fixtures/fakes are usable from both test assemblies, lookup tests green.
- Any later change to this phase is called out in its commit (it affects every B phase).
