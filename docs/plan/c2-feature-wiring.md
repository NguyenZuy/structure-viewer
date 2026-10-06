# C2 — Feature wiring & end-to-end tests

**Goal**: plug B09–B14 into the running app: panels mounted, toolbar and shortcuts, cross-feature reactions, end-to-end tests.
**Estimate**: 1.5 h · **Needs**: C1, B09–B14

## Tasks

### Composition (`AppBootstrap`)
- [ ] Create and own: `SelectionService`, `ViewportPresenter`, `InfoPanelPresenter`, `VisibilityService` + `VisibilityActions` + `VisibilityApplier` + `LayerPanelPresenter`, `DisplaySettings` + `MaterialApplier` + `LegendPresenter`, `MeasurePresenter`, `TakeoffPresenter`, `LabelsView`.
- [ ] Mount panels: info → right slot / bottom sheet on selection; layers + legend → left slot / sheets; takeoff → bottom slot / full sheet.

### Toolbar (priority order for compact overflow)
Fit all · Focus · Undo · Redo · Display ▾ (+ field) · Layers · Isolate · Hide · Show all · Measure · Takeoff · Labels · Multi-select (compact only). States: undo/redo enabled, isolate badge, measure/multi-select/labels active.

### Keyboard (`Presentation/Input/ShortcutInput.cs` — calls the same actions as the buttons)
`F` focus · `Home` fit · `Esc` clear selection / exit measure · `I` isolate · `H` hide · `Shift+H` show all · `Ctrl+Z` / `Ctrl+Y` / `Ctrl+Shift+Z` · `M` measure · `1`–`4` display modes.

### Cross-feature reactions
- [ ] `VisibilityChanged` → `SelectionService.RemoveWhere(hidden)`.
- [ ] Info panel `IsolateAssemblyRequested` → `VisibilityActions.IsolateGroup`.
- [ ] B11 `TypeColorProvider` → takeoff swatches (Color by Type only).
- [ ] Labels default: on desktop, off compact.

## Tests (PlayMode, `Main.unity` with the sample)
- Tap a known member → selected + highlight material; double-tap → whole group highlighted.
- Toggle `Wall` category → wall objects inactive; undo → active; selection of a hidden member is dropped.
- Cycle all 4 display modes with a hidden category + selection → hidden stays hidden, selection stays highlighted, camera transform unchanged.
- Measure mode → taps don't change selection.

## PC + mobile checks
- [ ] Every toolbar action reachable on a 360 px portrait phone (overflow); every shortcut has a button.
- [ ] **X-ray FPS on the phone** (gate from plan README); transparency correct in the WebGL build.

## Done when
- All MVP features work together on desktop and phone; full EditMode + PlayMode suites green.
