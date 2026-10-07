# B04 — Materials, textures & scene setup

**Goal**: all visual assets for the Realistic look plus an idempotent `Setup Scene` tool — no hand-written `.mat`/scene YAML.
**Estimate**: 1 h · **Needs**: A2
**Owns**: `Art/Textures/`, `Materials/`, `Scripts/Editor/Setup/`, `Data/RenderingConfig.asset`, `UI/PanelSettings.asset`

## Tasks
- [x] Download CC0 textures from ambientCG at 1K: one wood (albedo + normal), one concrete. Import: max 1024, compressed, normal map flagged. Note names + URLs (README credits in C4).
- [x] `Tools > Structure Viewer > Setup Scene` (idempotent — updates, never duplicates):
  - Materials (URP Lit): `Wood` (albedo + normal, tiling in metres), `Concrete`, `Sheathing` (Transparent, violet, α 0.35), `FlatOpaque` + `FlatTransparent` templates (used by B11 for cloning — **never switch surface type at runtime**, variants may be stripped), `Grid`.
  - Procedural grid texture asset (faint lines, fades by tiling).
  - `RenderingConfig.asset` filled with the above.
  - `PanelSettings.asset` (constant physical size, 96 dpi — see Notes), theme style sheet.
  - Scene `Main.unity`: camera (solid light-grey background), directional light (**shadows off**), gradient ambient, ground grid plane, empty `App` object (C1 adds `AppBootstrap`).
- [x] Never call `Shader.Find` at runtime — runtime code gets materials only through `RenderingConfig`.

## Tests
- EditMode: running the setup tool twice yields the same asset/object count (idempotency); `RenderingConfig` has no null fields.

## Notes
- Textures (CC0, ambientCG, 1K JPG; only `Color` + `NormalGL` kept): [Wood096](https://ambientcg.com/view?id=Wood096) → `Wood096_*.jpg`, [Concrete034](https://ambientcg.com/view?id=Concrete034) → `Concrete034_*.jpg`. Credit in the README (C4).
- Tool code: `Editor/Setup/` — `AssetSetup.Run(SetupPaths)` (assets) + `SceneLayout.Configure(scene, config)` (scene); tests run both twice against a temp folder and a new scene.
- Tiling in metres: wood 0.75 m, concrete 2 m per repeat. Grid: 1 m cells on an 80 m quad (no collider), transparent unlit, queue 2950 so it draws before sheathing; mipmaps fade it with distance.
- All materials: shadows not received, environment reflections off (solid-colour background, no probes).
- **PanelSettings deviates from the original plan**: scale-with-screen-size at 1920 × 1080 would make a portrait phone ~960 UI px wide (44 px targets ≈ 16 CSS px, never compact). Constant physical size at 96 dpi gives 1 UI px = 1 CSS px on WebGL (dpi = 96 × devicePixelRatio), so the 768 px breakpoint and 44 px targets hold.
- Scene: the template `Global Volume` is removed and camera post-processing/shadows/AA are off (no post-processing on any platform).
- `_overlay` stays empty: B12 creates the overlay shader/material.

## Hand-off to C
- C1: extend the setup tool to add/wire `AppBootstrap` on the `App` object.
- B11 consumes `FlatOpaque`/`FlatTransparent` templates through `RenderingConfig`.
