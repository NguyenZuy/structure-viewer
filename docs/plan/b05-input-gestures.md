# B05 — Input gestures

**Goal**: one input adapter that turns mouse and touch into `IPointerEvents` (tap, double-tap, orbit, pan, zoom, hover).
**Estimate**: 1 h · **Needs**: A2
**Owns**: `Scripts/Presentation/Input/`, `Tests/EditMode/Input/`

## Tasks
- [x] `GestureClassifier` (pure C#): feed pointer down/move/up samples + touch count + time → emits tap (with double flag), orbit delta, pan delta, zoom (amount, focus point).
  - Drag threshold DPI-scaled (`Screen.dpi / 96`, fallback 1); a press that never exceeds it = tap; no tap after a drag.
  - Double tap: second tap within 300 ms and 20 px (DPI-scaled).
  - Mouse: LMB drag → orbit, RMB/MMB drag → pan, wheel → zoom at cursor.
  - Touch: 1 finger → orbit, 2 fingers → pan (centroid delta) + pinch (distance ratio) at centroid; lifting one finger mid-gesture doesn't emit a tap.
- [x] `PointerInput : MonoBehaviour, IPointerEvents` — Input System `Mouse` + `EnhancedTouch`; ignores pointers that start over UI Toolkit (`panel.Pick` on the shared `PanelSettings`); exposes `additive` (Ctrl) and `PointerDevice`; hover events only for mouse.

## Tests (EditMode, on `GestureClassifier`)
- Small move → tap; beyond threshold → orbit, no tap.
- Two taps inside window → second flagged double; outside → two singles.
- RMB drag → pan; wheel → zoom with cursor focus.
- Two fingers: centroid move → pan; spread → zoom > 1; finger lift mid-pinch → no tap.
- Threshold scales with DPI.

## Notes
- Thresholds (`GestureSettings.ForDpi`): drag 8 px, double-tap radius 24 px, both × max(1, dpi / 96); double-tap window 0.3 s; wheel normalised to notches of 120, clamped to ±3 per event, 1.15× zoom per notch. A third quick tap is a single (each double consumes its pair).
- Lifting one finger mid-pinch suppresses the remaining finger until all are up (no surprise orbit or tap).
- Browsers emit compatibility mouse events for touches: `PointerInput` ignores the mouse for 0.5 s after any touch.

## Hand-off to C
- C1: set `PointerInput.Ui` to the shell's `UIDocument`. **B07/C2: full-screen layout containers must use `PickingMode.Ignore`** — `PointerInput` treats any pickable element under the pointer as UI and ignores presses that start there (wheel too).
- C1: put `PointerInput` on the `App` object; pass it as `IPointerEvents` to camera (B06), viewport (B09), measure (B12).
