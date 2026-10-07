# C2 — Feature wiring & end-to-end tests

**Goal**: plug B09–B14 into the running app: panels mounted, toolbar and shortcuts, cross-feature reactions, end-to-end tests.
**Estimate**: 1.5 h · **Needs**: C1, B09–B14

## Tasks

### Composition (`AppBootstrap`)
- [x] Create and own: `SelectionService`, `ViewportPresenter`, `InfoPanelPresenter`, `VisibilityService` + `VisibilityActions` + `VisibilityApplier` + `LayerPanelPresenter`, `DisplaySettings` + `MaterialApplier` + `LegendPresenter`, `MeasurePresenter`, `TakeoffPresenter`, `LabelsView`.
- [x] Mount panels: info → right slot / bottom sheet on selection; layers + legend → left slot / sheets; takeoff → bottom slot / full sheet.

### Toolbar (priority order for compact overflow)
Fit all · Focus · Undo · Redo · Display ▾ (+ field) · Layers · Isolate · Hide · Show all · Measure · Takeoff · Labels · Multi-select (compact only). States: undo/redo enabled, isolate badge, measure/multi-select/labels active.

### Keyboard (`Presentation/Input/ShortcutInput.cs` — calls the same actions as the buttons)
`F` focus · `Home` fit · `Esc` clear selection / exit measure · `I` isolate · `H` hide · `Shift+H` show all · `Ctrl+Z` / `Ctrl+Y` / `Ctrl+Shift+Z` · `M` measure · `1`–`4` display modes.

### Cross-feature reactions
- [x] `VisibilityChanged` → `SelectionService.RemoveWhere(hidden)`.
- [x] Info panel `IsolateAssemblyRequested` → `VisibilityActions.IsolateGroup`.
- [x] B11 `TypeColorProvider` → takeoff swatches (Color by Type only).
- [x] Labels default: on desktop, off compact.

## Tests (PlayMode, `Main.unity` with the sample)
- Tap a known member → selected + highlight material; double-tap → whole group highlighted.
- Toggle `Wall` category → wall objects inactive; undo → active; selection of a hidden member is dropped.
- Cycle all 4 display modes with a hidden category + selection → hidden stays hidden, selection stays highlighted, camera transform unchanged.
- Measure mode → taps don't change selection.

## PC + mobile checks
- [ ] Every toolbar action reachable on a 360 px portrait phone (overflow); every shortcut has a button.
- [ ] **X-ray FPS on the phone** (gate from plan README); transparency correct in the WebGL build.

## Notes
- `AppBootstrap` builds the graph in Start and disposes it in reverse order. Its `StructureLoaded` handler is subscribed first, so the renderer is rebuilt (with `MaterialApplier.BaseMaterial`) before any feature reacts. Services are exposed as read-only properties for the PlayMode tests only.
- `PanelLayout` (Bootstrap): desktop = layers + legend in the left slot, info right, takeoff in the bottom slot when toggled; compact = slots empty, every panel is the single bottom sheet and a selection opens "Details" (clearing it closes only that sheet). The Display picker is a sheet on both.
- `AppActions` (Bootstrap): every toolbar action; `ShortcutInput` → `ShortcutMap` ids → `AppActions.Execute`, so keys and buttons run the same code. Esc steps back: measurement → measure mode → selection. Undo/Redo/Show all/Focus enabled states; Isolate, Measure, Multi, Labels, Layers, Takeoff, Display show as active. A test pins "every shortcut has a toolbar button".
- Toolbar priority on narrow phones: Fit all, Layers, Display, Measure, Undo, Redo, Focus, Multi, then the rest in the overflow menu. Glyphs are WGL4 (`⌂ ◙ ◄ ► ☼ ≡ ■ □ ○ ↔ ▬ ♦ +`); the label under each glyph carries the meaning.
- Measure and label overlays are inserted under the shell (`ShellView.AddViewportOverlay`). `PointerInput.Classifier` is public so end-to-end tests tap through the real gesture path; `StructureRenderer.IsVisible/MaterialOf` read back state for them.
- Setup Scene now also creates `Data/DisplayPalette.asset` (coded defaults, kept on re-runs) and adds `ShortcutInput`, `MeasureView`, `LabelsView` to `App`.
- EditMode test asmdef now references `Unity.InputSystem` (shortcut tests).

## Done when
- All MVP features work together on desktop and phone; full EditMode + PlayMode suites green.
