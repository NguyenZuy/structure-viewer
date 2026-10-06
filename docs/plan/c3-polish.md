# C3 — Polish

**Goal**: no rough edges in the first 30 seconds on desktop or phone, consistent look across all four display modes.
**Estimate**: 1 h · **Needs**: C2

## Tasks
- [ ] Onboarding hint adapted to pointer type ("Drag to rotate · Right-drag / two fingers to pan · Scroll / pinch to zoom · Tap to select"); dismissible; remembered via `PlayerPrefs` wrapped in try/catch.
- [ ] Loading overlay until the model is built.
- [ ] Initial camera: pleasant 3/4 front-left view, then fit-all.
- [ ] Tune light direction, ambient gradient, wood tiling, grid fade, sheathing alpha against the background — check in all 4 modes.
- [ ] Desktop tooltips; clear active-tool states.
- [ ] Empty/edge states: nothing selected; everything hidden ("Everything is hidden — Show all"); load error.
- [ ] Dev FPS counter only in `DEVELOPMENT_BUILD`.

## Stretch (only if ahead)
Exploded view, human scale figure, FPS/draw-call overlay (DESIGN.md › Stretch).

## Checks
- [ ] Full pass of every feature at 360 × 780 portrait, phone landscape and 1920 × 1080.
- [ ] Console clean during the pass.
