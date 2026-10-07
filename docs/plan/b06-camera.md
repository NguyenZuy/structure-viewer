# B06 — Camera controller

**Goal**: smooth orbit/pan/zoom camera with fit-all and focus, implementing `ICameraControl` and driven by `IPointerEvents`.
**Estimate**: 1 h · **Needs**: A2
**Owns**: `Scripts/Presentation/CameraControl/`, `Tests/EditMode/CameraControl/`, `Tests/PlayMode/CameraControl/` (not `Camera/`: a `…Camera` namespace would shadow `UnityEngine.Camera`)

## Tasks
- [x] `CameraFraming` (pure): distance to fit `Bounds` for vertical FOV **and aspect** (portrait phones are the hard case), with margin.
- [x] `OrbitState` (pure): yaw, pitch (clamped 5°–89°), distance (min/max from model size), pivot; `Orbit(delta)`, `Pan(delta, cameraBasis)`, `Zoom(amount, focusRay)` (zoom toward pointer moves the pivot).
- [x] `CameraController : MonoBehaviour, ICameraControl` — subscribes to `IPointerEvents`; damped interpolation toward the target `OrbitState`; `FitAll(bounds)`, `Focus(bounds)` animate (~0.4 s); default 3/4 view from front-left.
- [x] Sensitivity per pointer type (touch slower orbit, pinch zoom curve).

## Tests
**EditMode**: framing keeps all 8 bound corners inside the frustum for 16:9 and 9:19.5; pitch clamp; zoom distance clamp; zoom toward a point off-centre moves the pivot toward it.
**PlayMode** (with `FakePointerEvents`): orbit event rotates the camera around the pivot; `FitAll` ends with the bounds fully visible.

## Notes
- `CameraController` sits on the camera object (`[RequireComponent(Camera)]`); `Bind(IPointerEvents)` subscribes, `OnDestroy` unsubscribes.
- Gestures move a target `OrbitState`; `LateUpdate` eases toward it (exponential, sharpness 10, unscaled time), which also animates FitAll/Focus (~0.3–0.4 s). Yaw/pitch lerp linearly, distance in log space.
- Fit is exact per corner for the current yaw/pitch (not a bounding sphere), margin 1.1. FitAll keeps the current angles and sets distance limits to 0.3 m … max(4 × fit, 5 m); the initial pose is yaw 45°, pitch 30° (front-left 3/4).
- Orbit is grab-style: drag right/up swings the camera left/down. Mouse 300°, touch 240° per screen-height of drag. Pan keeps the content under the pointer (world-per-pixel at the pivot depth).
- Zoom slides the camera along the pointer ray, so the point under the pointer stays fixed and the pivot shifts sideways on its depth plane.
- Rotation is built by hand (no `Quaternion.Euler`/`Inverse`, which are native) so the math tests run outside Unity.

## Hand-off to C
- C1: add `CameraController` to `Main Camera` (Setup Scene tool) and call `Bind(pointerInput)`.
- C1: inject `IPointerEvents`; call `FitAll(renderer.ModelBounds)` after build; Fit-all toolbar button.
- C2: Focus button + `F` key → `Focus(selection bounds)`; `Home` → `FitAll`.
