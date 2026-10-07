# B10 — Visibility & undo

**Goal**: layer/level toggles, isolate, hide, show all — every change undoable — from a single immutable `VisibilityState`.
**Estimate**: 1.5 h · **Needs**: A2
**Owns**: `Scripts/Domain/Visibility/` (except `IVisibility`), `Scripts/Application/Visibility/`, `Scripts/Presentation/Layers/`, `UI/Layers/`, `Tests/EditMode/Visibility/`

## Key decisions
- `VisibilityState : IVisibility` is **immutable** (`bool[]` per category, level, isolated, hidden). Commands keep `before`/`after` snapshots → exact undo by construction.
- `VisibilityService` publishes `VisibilityChanged` only when the state actually differs; no-op commands are not recorded.

## Tasks
- [x] `VisibilityState`: `IsVisible` (DESIGN.md formula), `WithCategory`, `WithLevel`, `WithIsolated(indices)`, `WithHidden(indices)`, `ShowAll()`, `IsIsolating`, structural equality.
- [x] `VisibilityService`: current state, `Set(state)`, resets on `StructureLoaded`.
- [x] Commands: `SetCategoryVisibleCommand`, `SetLevelVisibleCommand`, `IsolateCommand` (indices — selection, level or group are resolved by the caller), `HideCommand`, `ShowAllCommand`.
- [x] `VisibilityActions` (presenter-level API used by toolbar/shortcuts in C2): `IsolateSelection()`, `IsolateLevel(i)`, `IsolateGroup(group)`, `HideSelection()`, `ShowAll()`, `Undo()`, `Redo()`; tracks the current selection via `SelectionChanged`.
- [x] `VisibilityApplier`: on `VisibilityChanged` → `IStructureRenderer.SetVisible` for changed elements only.
- [x] Layer panel: `ILayerPanelView`, `LayerPanelView` (UXML: category toggles with counts, level toggles + "Isolate" per level), `LayerPanelPresenter`.

## Tests (EditMode)
- `VisibilityState`: each rule; isolate + category off; hide inside isolate; level off hides its panels/slab; `ShowAll`; equality.
- Every command: execute → undo equals original; redo re-applies; no-op not recorded; new command after undo clears redo.
- `VisibilityApplier` with `FakeStructureRenderer`: only changed indices touched.
- `LayerPanelPresenter` with fake view: toggle → command executed; view reflects state after undo.

## Notes
- One `VisibilityCommand(service, before, after)` instead of five command classes: every action in the DESIGN.md table is a swap between two immutable states, so the per-action logic lives in `VisibilityActions` (it builds `after` with `VisibilityState.With*`) and the command only swaps. Undo restores the exact `before` instance.
- `VisibilityState.With*` returns `this` for no-ops; `VisibilityActions` skips recording when `after.Equals(before)`. Every action returns `bool` (changed or not) for toasts in C2.
- `IsolateLevel` also switches that level on (the user asked to see it); other isolates follow the plain formula, and hidden elements stay hidden inside an isolate.
- `VisibilityService.Current` is null before the first load; on `StructureLoaded` it publishes `AllVisible(model)`.
- `VisibilityApplier` (in `Presentation/Layers/`) keeps what it last applied; the first state and any state after `StructureLoaded` get a full pass, later ones touch only changed indices.
- Layer panel: categories present in the model ("Walls", "Floors", "Roof", "Sheathing", "Slab") with counts, levels with counts + **Isolate**, and a "Show all" notice while isolating/hiding. `LayerPanelView` is a plain class wrapping `UI/Layers/LayerPanel.uxml`; rows are rebuilt only when the row set changes, toggles are updated with `SetValueWithoutNotify`.

## Hand-off to C
- C2: on `StructureLoaded` clear `CommandHistory` (old commands hold states of the previous model).
- C2: `VisibilityChanged` → B09 `SelectionService.RemoveWhere(i => !evt.Visibility.IsVisible(i))`.
- C2: mount layer panel (left slot / sheet); toolbar Isolate, Hide, Show all, Undo, Redo (+ enabled state from `CommandHistory.Changed`), isolate badge; shortcuts `I`, `H`, `Shift+H`, `Ctrl+Z`, `Ctrl+Y`/`Ctrl+Shift+Z`.
