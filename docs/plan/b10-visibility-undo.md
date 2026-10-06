# B10 — Visibility & undo

**Goal**: layer/level toggles, isolate, hide, show all — every change undoable — from a single immutable `VisibilityState`.
**Estimate**: 1.5 h · **Needs**: A2
**Owns**: `Scripts/Domain/Visibility/` (except `IVisibility`), `Scripts/Application/Visibility/`, `Scripts/Presentation/Layers/`, `UI/Layers/`, `Tests/EditMode/Visibility/`

## Key decisions
- `VisibilityState : IVisibility` is **immutable** (`bool[]` per category, level, isolated, hidden). Commands keep `before`/`after` snapshots → exact undo by construction.
- `VisibilityService` publishes `VisibilityChanged` only when the state actually differs; no-op commands are not recorded.

## Tasks
- [ ] `VisibilityState`: `IsVisible` (DESIGN.md formula), `WithCategory`, `WithLevel`, `WithIsolated(indices)`, `WithHidden(indices)`, `ShowAll()`, `IsIsolating`, structural equality.
- [ ] `VisibilityService`: current state, `Set(state)`, resets on `StructureLoaded`.
- [ ] Commands: `SetCategoryVisibleCommand`, `SetLevelVisibleCommand`, `IsolateCommand` (indices — selection, level or group are resolved by the caller), `HideCommand`, `ShowAllCommand`.
- [ ] `VisibilityActions` (presenter-level API used by toolbar/shortcuts in C2): `IsolateSelection()`, `IsolateLevel(i)`, `IsolateGroup(group)`, `HideSelection()`, `ShowAll()`, `Undo()`, `Redo()`; tracks the current selection via `SelectionChanged`.
- [ ] `VisibilityApplier`: on `VisibilityChanged` → `IStructureRenderer.SetVisible` for changed elements only.
- [ ] Layer panel: `ILayerPanelView`, `LayerPanelView` (UXML: category toggles with counts, level toggles + "Isolate" per level), `LayerPanelPresenter`.

## Tests (EditMode)
- `VisibilityState`: each rule; isolate + category off; hide inside isolate; level off hides its panels/slab; `ShowAll`; equality.
- Every command: execute → undo equals original; redo re-applies; no-op not recorded; new command after undo clears redo.
- `VisibilityApplier` with `FakeStructureRenderer`: only changed indices touched.
- `LayerPanelPresenter` with fake view: toggle → command executed; view reflects state after undo.

## Hand-off to C
- C2: mount layer panel (left slot / sheet); toolbar Isolate, Hide, Show all, Undo, Redo (+ enabled state from `CommandHistory.Changed`), isolate badge; shortcuts `I`, `H`, `Shift+H`, `Ctrl+Z`, `Ctrl+Y`/`Ctrl+Shift+Z`.
