# B14 — Assembly labels

**Goal**: floating group labels (`W-L0-N`, `T03`) that follow the model, hide with their assembly and fade with distance.
**Estimate**: 0.75 h · **Needs**: A2 · **First to cut** if behind
**Owns**: `Scripts/Domain/Labels/`, `Scripts/Presentation/Labels/`, `UI/Labels/`, `Tests/EditMode/Labels/`

## Tasks
- [ ] `AssemblyAnchors.Build(model)` → one anchor per group: top-centre of the group's bounds (computed once per load).
- [ ] `LabelVisibilityRule` (pure): visible if any element of the group is visible, in front of the camera and inside the viewport; opacity from distance.
- [ ] `LabelsView`: pooled UI Toolkit labels positioned each frame (no allocations); selected assembly emphasised (listens to `SelectionChanged`); `SetEnabled(bool)`.

## Tests (EditMode)
- One anchor per group at the top-centre of its bounds.
- Rule: hidden group → hidden; behind camera → hidden; far → lower opacity.

## Hand-off to C
- C2: toolbar Labels toggle — default on desktop, off compact.
