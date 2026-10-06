# B12 — Measure

**Goal**: two-point measuring with endpoint/midpoint snapping, distance and |dx|/|dy|/|dz| in model axes.
**Estimate**: 1.5 h · **Needs**: A2
**Owns**: `Scripts/Domain/Measure/`, `Scripts/Presentation/Measure/`, `Shaders/OverlayUnlit.shader`, `Materials/Overlay.mat` (created via editor API), `Tests/EditMode/Measure/`

## Key decisions
- Pure `MeasureSession` (state machine) + pure `MeasureSnapper`; the view only projects candidates and draws.
- **dx/dy/dz in model axes (Z-up, mm)** via `ModelAxes` — otherwise "dz" shows plan depth.
- While active, publishes `InteractionModeChanged(Measure)` so selection ignores taps.

## Tasks
- [ ] `MeasureSnapper.Snap(hitWorld, hitScreen, candidates, radiusPx)` → point + kind (`Endpoint/Midpoint/Free`); nearest within radius wins.
- [ ] `MeasureSession`: `Idle → AwaitingA → AwaitingB → Done`; `Pick`, `Cancel`, `Result` (distance mm, |dx|, |dy|, |dz|).
- [ ] `OverlayUnlit.shader`: URP unlit, `ZTest Always`, `ZWrite Off`, colour property; compiles for WebGL.
- [ ] `MeasureView`: projects the hit member's 2 endpoints + midpoint, snap marker, line, label (UI Toolkit, positioned per frame without allocations).
- [ ] `MeasurePresenter`: `SetActive(bool)`, `Clear()`; taps from `IPointerEvents` → `TryPick` → snap → session; snap radius 12 px mouse / 28 px touch × `Screen.dpi / 96`.

## Tests (EditMode)
- Snapper: inside radius snaps; nearest of two; outside → free; endpoint vs midpoint kind.
- Session: transitions, cancel from every state, third pick starts a new measurement.
- Result in model axes: vertical stud → |dz| = length, |dx| = |dy| = 0.
- Presenter with fakes: active → publishes Measure mode; deactivate → Select mode; touch uses the larger radius.

## Hand-off to C
- C2: toolbar Measure toggle + `M`, `Esc`/Clear; check label/marker readability on a 360 px phone.

## Cuttable
Midpoint snapping (DESIGN.md cut order).
