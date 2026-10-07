# B14 — Assembly labels

**Goal**: floating group labels (`W-L0-N`, `T03`) that follow the model, hide with their assembly and fade with distance.
**Estimate**: 0.75 h · **Needs**: A2 · **First to cut** if behind
**Owns**: `Scripts/Domain/Labels/`, `Scripts/Presentation/Labels/`, `UI/Labels/`, `Tests/EditMode/Labels/`

## Tasks
- [x] `AssemblyAnchors.Build(model)` → one anchor per group: top-centre of the group's bounds (computed once per load).
- [x] `LabelVisibilityRule` (pure): visible if any element of the group is visible, in front of the camera and inside the viewport; opacity from distance.
- [x] `LabelsView`: pooled UI Toolkit labels positioned each frame (no allocations); selected assembly emphasised (listens to `SelectionChanged`); `SetEnabled(bool)`.

## Tests (EditMode)
- One anchor per group at the top-centre of its bounds.
- Rule: hidden group → hidden; behind camera → hidden; far → lower opacity.

## Notes
- Added `LabelsPresenter` (plain C#) so the view stays passive: it builds anchors on load, resolves "any element visible" per group on `VisibilityChanged` (pushed to the view only on change) and emphasises the selected **assembly** (member selections don't). `LabelsView` only projects and styles each frame.
- Fade distances = model bounding radius × 3 (fully opaque) … × 8 (`LabelVisibilityRule.MinOpacity` 0.35); a fit-all view stays fully opaque. The emphasised label never fades.
- Styles are inline in `LabelsView` (no `UI/Labels` assets needed); overlay mounting works like `MeasureView.Overlay`.

## Hand-off to C
- C2: `LabelsView` component, `Camera` set by `AppBootstrap`, `Overlay` inserted under the panels (with the measure overlay); `LabelsPresenter.SetEnabled(!shell.IsCompact)` at start.
- C2: toolbar Labels toggle — default on desktop, off compact.
