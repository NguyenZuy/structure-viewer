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
- [x] Domain: `HighlightState` (`None, Hover, Member, Assembly`), `HighlightResolver` (selection > hover > none), `ColorKeyResolver.KeyOf(element, field)`, `LegendBuilder.Build(model, field, IVisibility)` → rows (key, label, count, isHidden) in stable order.
- [x] Application: `DisplaySettings` (mode, field; publishes `DisplayModeChanged` on real change).
- [x] `DisplayPaletteAsset` (ScriptableObject): category/type/level colours (type families: walls blues, floor greens, roof reds, sheathing violet; avoid selection orange/blue), X-ray colour + alphas (0.12 / 0.08), clay `#E8E6E1` / `#CFCBC4` / sheathing white α 0.25, highlight orange + blue.
- [x] `MaterialLibrary` (clone + cache + destroy on dispose).
- [x] `RealisticPalette`, `ColorByPalette`, `XRayPalette` (highlighted → **opaque**), `ClayPalette` — per DESIGN.md table.
- [x] `MaterialApplier`: listens to `StructureLoaded`, `SelectionChanged`, `HoverChanged`, `DisplayModeChanged` → `IStructureRenderer.SetMaterial` only where the resolved material changed.
- [x] ~~`DisplayActions`~~: `DisplaySettings.SetMode/SetField` already is that API (a wrapper would be a one-line pass-through).
- [x] Legend: `ILegendView`, `LegendView` (swatch, label, count; hidden greyed), `LegendPresenter` (visible only in Color by; refresh on `DisplayModeChanged` + `VisibilityChanged`).
- [x] `TypeColorProvider` (`Func<string, Color?>`) for the takeoff swatch column.

## Tests (EditMode)
- `HighlightResolver` precedence table.
- `ColorKeyResolver` for member, panel, slab under each field.
- `LegendBuilder`: present keys only, counts, stable order, hidden flag.
- Palettes (test materials): X-ray highlighted → opaque material; same key → same instance; field change → different material.
- `MaterialApplier` with `FakeStructureRenderer`: mode switch updates materials, never calls `SetVisible`; selection change touches only affected indices.

## Notes
- `ColorKey` (field, ordinal, name): category/level keys carry their ordinal (palette index), type keys their name. `LegendRow` also carries the category of the key's first element, because types missing from the palette table fall back to their category colour.
- Legend order: categories and levels by ordinal, types grouped by category then by name. `IsHidden` = every element of that key is invisible.
- `DisplayPaletteAsset`: field initialisers are the shipped defaults (hex parsed in plain C#: Unity APIs are not allowed in a ScriptableObject constructor), so `CreateInstance` is a complete palette. Wall types use steel blues/teals and roof types reds/pinks, distinct from the saturated selection blue `#3D8BFF` and orange `#FF8A1F`.
- `MaterialLibrary` clones a template per (template, colour) and sets `_BaseColor`; `Tint` keeps the template's alpha and texture (Realistic highlight = wood × orange/blue). All palettes live in `ElementPalettes.cs`.
- Panels stay translucent when highlighted in Color by / Clay (a highlighted roof must not hide the frame); X-ray highlights are opaque for every kind.
- `MaterialApplier` keeps the last applied material per element. Handlers of one event run in subscription order, so after `StructureLoaded` the renderer may not be rebuilt yet: the applier waits until `renderer.Count` matches the model, then does one full pass. Pass `applier.BaseMaterial` to `StructureRenderer.Build` so a fresh build already matches the mode.
- `Data/DisplayPalette.asset` is not created yet (needs the Editor): C2 Setup Scene creates it with `ScriptableObject.CreateInstance<DisplayPaletteAsset>()` and wires it.

## Hand-off to C
- C2: `AppBootstrap` replaces its `RealisticMaterial` with `MaterialApplier.BaseMaterial`; create the applier before the first load. Serialize `DisplayPaletteAsset` + `Legend.uxml` on `AppBootstrap` (Setup Scene).
- C2: toolbar Display dropdown + field selector (overflow on compact), keys `1`–`4`; mount legend (left slot / sheet); pass `TypeColorProvider` to takeoff; X-ray phone FPS gate; verify transparency in the WebGL build.
