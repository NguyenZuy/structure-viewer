# C3 — Polish

**Goal**: no rough edges in the first 30 seconds on desktop or phone, consistent look across all four display modes.
**Estimate**: 1 h · **Needs**: C2

## Tasks
- [x] Onboarding hint adapted to pointer type ("Drag to rotate · Right-drag / two fingers to pan · Scroll / pinch to zoom · Tap to select"); dismissible; remembered via `PlayerPrefs` wrapped in try/catch.
- [x] Loading overlay until the model is built. (Covered by the WebGL template's progress bar, removed when the engine starts; the sample is a `TextAsset` built synchronously in the first `Start`, so an in-app overlay would never be visible.)
- [x] Initial camera: pleasant 3/4 front-left view, then fit-all.
- [ ] Tune light direction, ambient gradient, wood tiling, grid fade, sheathing alpha against the background — check in all 4 modes.
- [x] Desktop tooltips; clear active-tool states.
- [x] Empty/edge states: nothing selected; everything hidden ("Everything is hidden — Show all"); load error.
- [x] Dev FPS counter only in `DEVELOPMENT_BUILD`.

## Notes
- `NoticeView` (Presentation/Notices): a card under the toolbar over the 3D view, inserted with `ShellView.AddViewportOverlay`; only the card takes pointer input. Used by `OnboardingPresenter` (mouse or touch wording, guessed from `isMobilePlatform` + touchscreen, corrected by the first tap; dismissal stored via `IOnboardingStore` → `PlayerPrefsOnboardingStore`, try/catch for blocked storage) and `EverythingHiddenPresenter` ("Everything is hidden." + **Show all**).
- Empty states already covered: nothing selected → info panel hint (B09); load error → error toast (C1); no model → panels' "No structure loaded".
- Initial camera: `CameraController` starts at yaw 45° / pitch 30° (front-left 3/4) and animates into `FitAll` on load (C1).
- `FpsCounter` is added only under `DEVELOPMENT_BUILD` (bottom-left, text updated twice a second).

- **Envelope (user request, 2026-10-07)**: the bare frame didn't read as a house. `EnvelopeBuilder` (generator) adds external wall sheathing per storey, cut around openings and grouped with its wall, plus first-floor decking per joist bay (`FloorSheathing`, 19 mm). Realistic shows sheathing as an OSB tone at α 0.85 (`DisplayPaletteAsset.RealisticSheathing`). `StructureRenderer.TryPick` prefers a member within 0.3 m behind a sheathing hit, so clicking a wall over a stud still selects the stud. Regenerate the sample with *Tools > Structure Viewer > Generate Sample House*.

- **Sample house v2 (user request)**: American colonial/farmhouse (`SampleHouseSpec`: 12 × 8.4 m, 35° roof, symmetric front, `PorchBuilder` porch, gable sheathing). ~700 members, 85 panels.
- **Realistic colours (user feedback: "all orange")**: panel tone per type (`DisplayPaletteAsset.RealisticPanelFor`): walls white house wrap, roofs charcoal, floors OSB, doors red, glazing pale blue.
- **Doors and windows (user feedback: "no doors yet")**: `OpeningFillBuilder` (generator) fills every framed opening with a `Door` leaf or `Window` glazing panel, centred in the wall depth, in the wall's group. New `ElementCategory.Opening` → "Doors & windows" layer, gold family in Color by.
- **Label declutter**: `LabelDeclutter` (Domain) hides labels overlapping a higher-priority one (selected first, then nearest), allocation-free, run every frame by `LabelsView` with each label's last measured size.

## Stretch (only if ahead)
Exploded view, human scale figure, FPS/draw-call overlay (DESIGN.md › Stretch).

## Checks
- [ ] Full pass of every feature at 360 × 780 portrait, phone landscape and 1920 × 1080.
- [ ] Console clean during the pass.
