# B11 — Display modes

**Goal**: Realistic, Color by (Category/Type/Level), X-ray and Clay — all four required — plus selection/hover highlight and the legend.
**Estimate**: 2 h · **Needs**: A2
**Owns**: `Scripts/Domain/Display/` (except the A2 enums), `Scripts/Application/Display/`, `Scripts/Presentation/Display/`, `UI/Legend/`, `Data/DisplayPalette.asset`, `Tests/EditMode/Display/`

## Key decisions
- Strategy pattern: `IElementPalette.Resolve(index, HighlightState) → Material`; one class per mode.
- Key mapping is pure domain (`ColorKeyResolver`); palettes only map key + state → material.
- Materials are **clones of template materials** (`RenderingConfig`), cached by `(mode, key, state)`; surface type is never switched at runtime.
- Switching mode never touches visibility, selection or camera; not undoable.

## Tasks
- [ ] Domain: `HighlightState` (`None, Hover, Member, Assembly`), `HighlightResolver` (selection > hover > none), `ColorKeyResolver.KeyOf(element, field)`, `LegendBuilder.Build(model, field, IVisibility)` → rows (key, label, count, isHidden) in stable order.
- [ ] Application: `DisplaySettings` (mode, field; publishes `DisplayModeChanged` on real change).
- [ ] `DisplayPaletteAsset` (ScriptableObject): category/type/level colours (type families: walls blues, floor greens, roof reds, sheathing violet; avoid selection orange/blue), X-ray colour + alphas (0.12 / 0.08), clay `#E8E6E1` / `#CFCBC4` / sheathing white α 0.25, highlight orange + blue.
- [ ] `MaterialLibrary` (clone + cache + destroy on dispose).
- [ ] `RealisticPalette`, `ColorByPalette`, `XRayPalette` (highlighted → **opaque**), `ClayPalette` — per DESIGN.md table.
- [ ] `MaterialApplier`: listens to `StructureLoaded`, `SelectionChanged`, `HoverChanged`, `DisplayModeChanged` → `IStructureRenderer.SetMaterial` only where the resolved material changed.
- [ ] `DisplayActions`: `SetMode(mode)`, `SetField(field)` (for toolbar/keys in C2).
- [ ] Legend: `ILegendView`, `LegendView` (swatch, label, count; hidden greyed), `LegendPresenter` (visible only in Color by; refresh on `DisplayModeChanged` + `VisibilityChanged`).
- [ ] `TypeColorProvider` (`Func<string, Color?>`) for the takeoff swatch column.

## Tests (EditMode)
- `HighlightResolver` precedence table.
- `ColorKeyResolver` for member, panel, slab under each field.
- `LegendBuilder`: present keys only, counts, stable order, hidden flag.
- Palettes (test materials): X-ray highlighted → opaque material; same key → same instance; field change → different material.
- `MaterialApplier` with `FakeStructureRenderer`: mode switch updates materials, never calls `SetVisible`; selection change touches only affected indices.

## Hand-off to C
- C2: toolbar Display dropdown + field selector (overflow on compact), keys `1`–`4`; mount legend (left slot / sheet); pass `TypeColorProvider` to takeoff; X-ray phone FPS gate; verify transparency in the WebGL build.
