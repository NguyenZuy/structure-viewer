# C1 — Core viewer composition

**Goal**: first end-to-end app — the sample house loads in Realistic look, camera works with mouse and touch, UI shell is up, running on a phone.
**Estimate**: 1 h · **Needs**: B01–B08

## Tasks
- [ ] `Bootstrap/AppBootstrap.cs`: `QualitySelector` → create `EventBus`, `CommandHistory`, `StructureSession`, parser, `LoadStructureUseCase` → load `sample-house.json` → `StructureRenderer.Build(model, wood)` → `CameraController.FitAll`. Holds and disposes every presenter/subscription it creates.
- [ ] Load errors → `IShell.ShowToast`.
- [ ] Extend `Setup Scene` (B04) to add `AppBootstrap`, `PointerInput`, `CameraController`, `StructureRenderer`, `ShellView` and wire serialized references.
- [ ] Toolbar: Fit all.
- [ ] Build `Main.unity` with the B08 build menu; test on desktop and phone.

## Tests
**EditMode**: generator ↔ parser contract — `SampleHouseGenerator.Generate()` serialised → `JsonStructureParser` → zero errors, element counts match.
**PlayMode**: `Main.unity` loads → spawned element count = model count; no errors logged; camera frames the model.

## PC + mobile checks
- [ ] Orbit/pan/zoom/fit with mouse, trackpad and a real phone (portrait + landscape).
- [ ] UI pointer events don't rotate the camera.

## Done when
- End of day 1 milestone: house visible and explorable on desktop and phone.
