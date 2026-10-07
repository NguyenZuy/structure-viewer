# B09 — Selection & info panel

**Goal**: member / assembly / multi selection from taps, and an info panel presenting the selection.
**Estimate**: 1.5 h · **Needs**: A2
**Owns**: `Scripts/Domain/Selection/SelectionSummary.cs`, `Scripts/Application/Selection/`, `Scripts/Presentation/Viewport/`, `Scripts/Presentation/Info/`, `UI/Info/`, `Tests/EditMode/Selection/`

Highlight colours are **not** applied here — B11 resolves materials from `SelectionChanged`/`HoverChanged`.

## Tasks
- [x] `SelectionService` (Application): `Select(i)`, `SelectAssembly(i)` (all elements of `i`'s group), `Toggle(i)`, `Clear()`, `RemoveWhere(predicate)`, `Current` snapshot; publishes `SelectionChanged` only on real changes.
- [x] `SelectionSummary` (Domain): count, total length, total area, breakdown by type for any index set.
- [x] `ViewportPresenter`: `IPointerEvents.Tapped` → `TryPick` → tap = select, double = assembly, additive (Ctrl or multi-select mode) = toggle, empty = clear; mouse hover → `HoverChanged` (max one pick per frame); ignores taps while `InteractionMode` ≠ `Select`; `SetMultiSelect(bool)` for the touch toggle; `ClearSelection()` for `Esc`.
- [x] `FocusSelection()` → `ICameraControl.Focus(union of renderer bounds)`.
- [x] Info panel: `IInfoPanelView`, `InfoPanelView` (UXML, root `VisualElement`), `InfoPanelPresenter` — single member: id, type, category, level, group, section `35 × 90`, material, length mm; assembly/multi: count, total length, breakdown by type; empty: "Tap a member to see details". Buttons raise `SelectAssemblyRequested`, `IsolateAssemblyRequested` (handled in C2).

## Tests (EditMode, `MiniHouse` + fakes)
- `SelectionService`: select replaces; assembly selects whole group; toggle add/remove; clear; event fires once per change, none when unchanged; `RemoveWhere` updates kind correctly.
- `SelectionSummary`: totals and breakdown.
- `ViewportPresenter` with `FakePointerEvents` + `FakeStructureRenderer`: tap/double/additive/empty paths; taps ignored in Measure mode; hover only from mouse.
- `InfoPanelPresenter` with fake view: member vs assembly vs empty rendering; dispose unsubscribes.

## Notes
- `SelectionService` is `IDisposable`: it clears itself on `StructureLoaded` (old indices are meaningless). Invalid indices and "no model" are ignored.
- Kinds: `Toggle` → `Member` (1) / `Multi` (2+); a partly removed assembly stays `Assembly`; a double tap always replaces the selection with the assembly (the first tap of the pair has already selected the element).
- Additive tap on empty space keeps the selection (Ctrl+click convention; also stops accidental clears in touch multi-select mode). The info panel has a **Clear** button so touch can always clear.
- Hover: `PointerInput` raises `Hovered` at most once per frame and only on movement, so the presenter picks once per event. Touch taps and leaving Select mode reset hover to none.
- Info panel content is built by the presenter as `InfoPanelContent` (formatted strings, invariant culture: `2,330 mm`, `4.66 m`, `6.49 m²`); `InfoPanelView` is a plain class wrapping `UI/Info/InfoPanel.uxml` (`Root`), not a MonoBehaviour, because the shell decides where it is mounted.
- "Select assembly" is handled inside the presenter (`SelectionService.SelectAssembly`); only isolate leaves the feature.

## Hand-off to C
- C2: `new InfoPanelView(infoPanelUxml)` (serialize the `VisualTreeAsset` on `AppBootstrap`, wire it in Setup Scene), mount `Root`; `InfoPanelPresenter.IsolateAssemblyRequested(group)` → B10 `IsolateGroup`.
- C2: `ViewportPresenter.FocusSelection()` returns false on empty selection → toast "Select something to focus".
- C2: mount info panel (right slot / bottom sheet that opens on selection); toolbar Focus + Multi-select (compact only); `Esc` → clear; `VisibilityChanged` → `RemoveWhere(hidden)`; wire `IsolateAssemblyRequested` to B10.
