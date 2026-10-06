# B05 — Input gestures

**Goal**: one input adapter that turns mouse and touch into `IPointerEvents` (tap, double-tap, orbit, pan, zoom, hover).
**Estimate**: 1 h · **Needs**: A2
**Owns**: `Scripts/Presentation/Input/`, `Tests/EditMode/Input/`

## Tasks
- [ ] `GestureClassifier` (pure C#): feed pointer down/move/up samples + touch count + time → emits tap (with double flag), orbit delta, pan delta, zoom (amount, focus point).
  - Drag threshold DPI-scaled (`Screen.dpi / 96`, fallback 1); a press that never exceeds it = tap; no tap after a drag.
  - Double tap: second tap within 300 ms and 20 px (DPI-scaled).
  - Mouse: LMB drag → orbit, RMB/MMB drag → pan, wheel → zoom at cursor.
  - Touch: 1 finger → orbit, 2 fingers → pan (centroid delta) + pinch (distance ratio) at centroid; lifting one finger mid-gesture doesn't emit a tap.
- [ ] `PointerInput : MonoBehaviour, IPointerEvents` — Input System `Mouse` + `EnhancedTouch`; ignores pointers that start over UI Toolkit (`panel.Pick` on the shared `PanelSettings`); exposes `additive` (Ctrl) and `PointerDevice`; hover events only for mouse.

## Tests (EditMode, on `GestureClassifier`)
- Small move → tap; beyond threshold → orbit, no tap.
- Two taps inside window → second flagged double; outside → two singles.
- RMB drag → pan; wheel → zoom with cursor focus.
- Two fingers: centroid move → pan; spread → zoom > 1; finger lift mid-pinch → no tap.
- Threshold scales with DPI.

## Hand-off to C
- C1: put `PointerInput` on the `App` object; pass it as `IPointerEvents` to camera (B06), viewport (B09), measure (B12).
