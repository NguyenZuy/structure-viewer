# B06 — Camera controller

**Goal**: smooth orbit/pan/zoom camera with fit-all and focus, implementing `ICameraControl` and driven by `IPointerEvents`.
**Estimate**: 1 h · **Needs**: A2
**Owns**: `Scripts/Presentation/CameraControl/`, `Tests/EditMode/CameraControl/`, `Tests/PlayMode/CameraControl/` (not `Camera/`: a `…Camera` namespace would shadow `UnityEngine.Camera`)

## Tasks
- [ ] `CameraFraming` (pure): distance to fit `Bounds` for vertical FOV **and aspect** (portrait phones are the hard case), with margin.
- [ ] `OrbitState` (pure): yaw, pitch (clamped 5°–89°), distance (min/max from model size), pivot; `Orbit(delta)`, `Pan(delta, cameraBasis)`, `Zoom(amount, focusRay)` (zoom toward pointer moves the pivot).
- [ ] `CameraController : MonoBehaviour, ICameraControl` — subscribes to `IPointerEvents`; damped interpolation toward the target `OrbitState`; `FitAll(bounds)`, `Focus(bounds)` animate (~0.4 s); default 3/4 view from front-left.
- [ ] Sensitivity per pointer type (touch slower orbit, pinch zoom curve).

## Tests
**EditMode**: framing keeps all 8 bound corners inside the frustum for 16:9 and 9:19.5; pitch clamp; zoom distance clamp; zoom toward a point off-centre moves the pivot toward it.
**PlayMode** (with `FakePointerEvents`): orbit event rotates the camera around the pivot; `FitAll` ends with the bounds fully visible.

## Hand-off to C
- C1: inject `IPointerEvents`; call `FitAll(renderer.ModelBounds)` after build; Fit-all toolbar button.
- C2: Focus button + `F` key → `Focus(selection bounds)`; `Home` → `FitAll`.
