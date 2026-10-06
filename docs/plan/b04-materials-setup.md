# B04 — Materials, textures & scene setup

**Goal**: all visual assets for the Realistic look plus an idempotent `Setup Scene` tool — no hand-written `.mat`/scene YAML.
**Estimate**: 1 h · **Needs**: A2
**Owns**: `Art/Textures/`, `Materials/`, `Scripts/Editor/Setup/`, `Data/RenderingConfig.asset`, `UI/PanelSettings.asset`

## Tasks
- [ ] Download CC0 textures from ambientCG at 1K: one wood (albedo + normal), one concrete. Import: max 1024, compressed, normal map flagged. Note names + URLs (README credits in C4).
- [ ] `Tools > Structure Viewer > Setup Scene` (idempotent — updates, never duplicates):
  - Materials (URP Lit): `Wood` (albedo + normal, tiling in metres), `Concrete`, `Sheathing` (Transparent, violet, α 0.35), `FlatOpaque` + `FlatTransparent` templates (used by B11 for cloning — **never switch surface type at runtime**, variants may be stripped), `Grid`.
  - Procedural grid texture asset (faint lines, fades by tiling).
  - `RenderingConfig.asset` filled with the above.
  - `PanelSettings.asset` (scale with screen size, reference 1920 × 1080, match 0.5), theme style sheet.
  - Scene `Main.unity`: camera (solid light-grey background), directional light (**shadows off**), gradient ambient, ground grid plane, empty `App` object (C1 adds `AppBootstrap`).
- [ ] Never call `Shader.Find` at runtime — runtime code gets materials only through `RenderingConfig`.

## Tests
- EditMode: running the setup tool twice yields the same asset/object count (idempotency); `RenderingConfig` has no null fields.

## Hand-off to C
- C1: extend the setup tool to add/wire `AppBootstrap` on the `App` object.
- B11 consumes `FlatOpaque`/`FlatTransparent` templates through `RenderingConfig`.
