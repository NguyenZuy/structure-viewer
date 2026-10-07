# B12 — Measure

**Goal**: two-point measuring with endpoint/midpoint snapping, distance and |dx|/|dy|/|dz| in model axes.
**Estimate**: 1.5 h · **Needs**: A2
**Owns**: `Scripts/Domain/Measure/`, `Scripts/Presentation/Measure/`, `Shaders/OverlayUnlit.shader`, `Materials/Overlay.mat` (created via editor API), `Tests/EditMode/Measure/`

## Key decisions
- Pure `MeasureSession` (state machine) + pure `MeasureSnapper`; the view only projects candidates and draws.
- **dx/dy/dz in model axes (Z-up, mm)** via `ModelAxes` — otherwise "dz" shows plan depth.
- While active, publishes `InteractionModeChanged(Measure)` so selection ignores taps.

## Tasks
- [x] `MeasureSnapper.Snap(hitWorld, hitScreen, candidates, radiusPx)` → point + kind (`Endpoint/Midpoint/Free`); nearest within radius wins.
- [x] `MeasureSession`: `Idle → AwaitingA → AwaitingB → Done`; `Pick`, `Cancel`, `Result` (distance mm, |dx|, |dy|, |dz|).
- [x] ~~`OverlayUnlit.shader`~~: not needed, the line is drawn by the UI overlay (see Notes).
- [x] `MeasureView`: projects the hit member's 2 endpoints + midpoint, snap marker, line, label (UI Toolkit, positioned per frame without allocations).
- [x] `MeasurePresenter`: `SetActive(bool)`, `Clear()`; taps from `IPointerEvents` → `TryPick` → snap → session; snap radius 12 px mouse / 28 px touch × `Screen.dpi / 96`.

## Tests (EditMode)
- Snapper: inside radius snaps; nearest of two; outside → free; endpoint vs midpoint kind.
- Session: transitions, cancel from every state, third pick starts a new measurement.
- Result in model axes: vertical stud → |dz| = length, |dx| = |dy| = 0.
- Presenter with fakes: active → publishes Measure mode; deactivate → Select mode; touch uses the larger radius.

## Notes
- **Changed from the plan**: line, markers and label are drawn in screen space by `MeasureView` (UI Toolkit `Painter2D` on a full-screen overlay element, picking ignored) instead of a 3D line with an `OverlayUnlit` (`ZTest Always`) shader. Same goal (always on top of the model) with constant pixel width at any zoom, no extra shader for WebGL, and no `Overlay.mat` to generate. `RenderingConfig.Overlay` stays unused; drop it in C3 if nothing else needs it.
- `MeasureView` reprojects A/B every `LateUpdate` while a point exists (no allocations: struct styles, label text only set on change). Markers: endpoint = dot, midpoint = diamond, free = ring.
- Snap candidates: the hit member's start, end and midpoint (centreline); panels and slabs give free points. Radius 12 px mouse / 28 px touch × max(1, dpi / 96).
- Taps on empty space are ignored while measuring. A third tap starts a new measurement at that point. `StructureLoaded` restarts a running measurement.
- Label: `2,330 mm` / `dx 0 · dy 0 · dz 2,330` (invariant culture, model axes).

## Hand-off to C
- C2: `MeasureView` component (e.g. on `App`), `Camera` set by `AppBootstrap`; insert `MeasureView.Overlay` as the first child of the shell document root so panels draw above it.
- C2: toolbar Measure toggle + `M`, `Esc`/Clear; check label/marker readability on a 360 px phone.

## Cuttable
Midpoint snapping (DESIGN.md cut order).
