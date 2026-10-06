# B09 — Selection & info panel

**Goal**: member / assembly / multi selection from taps, and an info panel presenting the selection.
**Estimate**: 1.5 h · **Needs**: A2
**Owns**: `Scripts/Domain/Selection/SelectionSummary.cs`, `Scripts/Application/Selection/`, `Scripts/Presentation/Viewport/`, `Scripts/Presentation/Info/`, `UI/Info/`, `Tests/EditMode/Selection/`

Highlight colours are **not** applied here — B11 resolves materials from `SelectionChanged`/`HoverChanged`.

## Tasks
- [ ] `SelectionService` (Application): `Select(i)`, `SelectAssembly(i)` (all elements of `i`'s group), `Toggle(i)`, `Clear()`, `RemoveWhere(predicate)`, `Current` snapshot; publishes `SelectionChanged` only on real changes.
- [ ] `SelectionSummary` (Domain): count, total length, total area, breakdown by type for any index set.
- [ ] `ViewportPresenter`: `IPointerEvents.Tapped` → `TryPick` → tap = select, double = assembly, additive (Ctrl or multi-select mode) = toggle, empty = clear; mouse hover → `HoverChanged` (max one pick per frame); ignores taps while `InteractionMode` ≠ `Select`; `SetMultiSelect(bool)` for the touch toggle; `ClearSelection()` for `Esc`.
- [ ] `FocusSelection()` → `ICameraControl.Focus(union of renderer bounds)`.
- [ ] Info panel: `IInfoPanelView`, `InfoPanelView` (UXML, root `VisualElement`), `InfoPanelPresenter` — single member: id, type, category, level, group, section `35 × 90`, material, length mm; assembly/multi: count, total length, breakdown by type; empty: "Tap a member to see details". Buttons raise `SelectAssemblyRequested`, `IsolateAssemblyRequested` (handled in C2).

## Tests (EditMode, `MiniHouse` + fakes)
- `SelectionService`: select replaces; assembly selects whole group; toggle add/remove; clear; event fires once per change, none when unchanged; `RemoveWhere` updates kind correctly.
- `SelectionSummary`: totals and breakdown.
- `ViewportPresenter` with `FakePointerEvents` + `FakeStructureRenderer`: tap/double/additive/empty paths; taps ignored in Measure mode; hover only from mouse.
- `InfoPanelPresenter` with fake view: member vs assembly vs empty rendering; dispose unsubscribes.

## Hand-off to C
- C2: mount info panel (right slot / bottom sheet that opens on selection); toolbar Focus + Multi-select (compact only); `Esc` → clear; `VisibilityChanged` → `RemoveWhere(hidden)`; wire `IsolateAssemblyRequested` to B10.
