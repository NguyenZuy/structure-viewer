# C1 — Core viewer composition

**Goal**: first end-to-end app — the sample house loads in Realistic look, camera works with mouse and touch, UI shell is up, running on a phone.
**Estimate**: 1 h · **Needs**: B01–B08

## Tasks
- [x] `Bootstrap/AppBootstrap.cs`: `QualitySelector` → create `EventBus`, `CommandHistory`, `StructureSession`, parser, `LoadStructureUseCase` → load `sample-house.json` → `StructureRenderer.Build(model, wood)` → `CameraController.FitAll`. Holds and disposes every presenter/subscription it creates.
- [x] Load errors → `IShell.ShowToast`.
- [x] Extend `Setup Scene` (B04) to add `AppBootstrap`, `PointerInput`, `CameraController`, `StructureRenderer`, `ShellView` and wire serialized references.
- [x] Toolbar: Fit all.
- [ ] Build `Main.unity` with the B08 build menu; test on desktop and phone.

## Tests
**EditMode**: generator ↔ parser contract — `SampleHouseGenerator.Generate()` serialised → `JsonStructureParser` → zero errors, element counts match.
**PlayMode**: `Main.unity` loads → spawned element count = model count; no errors logged; camera frames the model.

## PC + mobile checks
- [ ] Orbit/pan/zoom/fit with mouse, trackpad and a real phone (portrait + landscape).
- [ ] UI pointer events don't rotate the camera.

## Notes
- `AppBootstrap` (on `App`): `QualitySelector.Apply()` in Awake; wiring in Start (after `ShellView.OnEnable`). The sample is a serialized `TextAsset`, so no web request on WebGL. `CommandHistory` is created in C2 when the first command exists.
- Scene objects: `Main Camera` (+ `CameraController`), `Structure` (`StructureRenderer`), `Shell` (`UIDocument` + `ShellView`), `App` (`PointerInput` → shell document, `AppBootstrap` with every reference wired by the setup tool).
- Setup Scene loads every asset it wires *after* opening the scene: `OpenScene` unloads unused assets, and a reference held across it is saved as null.
- Fit all glyph `⌂` (WGL4 range, present in common default fonts).
- Contract tests: generated house parses with zero errors and keeps every element; the committed `sample-house.json` matches the generator output.

## Done when
- End of day 1 milestone: house visible and explorable on desktop and phone.
