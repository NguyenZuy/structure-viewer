# B13 — Material takeoff

**Goal**: takeoff table grouped by type + section + material with counts, lengths, volumes and areas.
**Estimate**: 1 h · **Needs**: A2
**Owns**: `Scripts/Domain/Takeoff/`, `Scripts/Presentation/Takeoff/`, `UI/Takeoff/`, `Tests/EditMode/Takeoff/`

## Tasks
- [x] `TakeoffCalculator.Calculate(model, IVisibility, visibleOnly)` → rows: type, section (`35 × 90`), material, count, total length (m), volume (m³); panels → area (m²); slab → volume; totals row. Sorted by category, then type.
- [x] `ITakeoffView`, `TakeoffView` (`MultiColumnListView`, "Visible only" toggle, optional swatch column, horizontal scroll on narrow widths).
- [x] `TakeoffPresenter`: recalculates on `StructureLoaded`, on toggle, and on `VisibilityChanged` only when visible-only is on; `SetSwatchProvider(Func<string, Color?>)` shows swatches when given (wired in C2).

## Tests (EditMode)
- Grouping key; counts; length/volume/area maths on `MiniHouse`; visible-only excludes hidden (`FakeVisibility`); totals = sum of rows.
- Presenter with fake view: toggle recalculates; `VisibilityChanged` ignored when visible-only is off.

## Notes
- Grouping key: kind + type + section + material. Panels and slabs have no section or material in the schema, so their "section" is the thickness (`12 mm`) and material is empty.
- Members report length + volume; panels area + volume (area × thickness); slabs area + volume. Columns that don't apply are blank, not `0`.
- `TakeoffPresenter` recalculates on load, on the toggle, and on `VisibilityChanged` only while "Visible only" is on (`CalculationCount` exposes this for tests). `SetSwatchProvider(null)` hides the swatch column.
- `TakeoffView`: `MultiColumnListView` built in code (fixed column widths, horizontal scrolling), totals as the last line in bold; plain class wrapping `UI/Takeoff/Takeoff.uxml`.

## Hand-off to C
- C2: `takeoffPresenter.SetSwatchProvider(typeColors.ColorOf)` with B11's `TypeColorProvider`.
- C2: mount (bottom slot desktop / full sheet compact) + toolbar Takeoff button; pass `TypeColorProvider` from B11.

## Cuttable
"Visible only" toggle (DESIGN.md cut order).
