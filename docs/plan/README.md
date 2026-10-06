# Implementation Plan

Spec: [../DESIGN.md](../DESIGN.md). Conventions: [../../CLAUDE.md](../../CLAUDE.md).

## Structure

```
A. Core (sequential)        B. Features (independent — any order, parallelisable)      C. Composition (sequential)
                            ┌ B01 JSON parser          B09 Selection & info ┐
A1 Foundation ─► A2 Contracts ┤ B02 Sample generator     B10 Visibility & undo ├─► C1 Core viewer ─► C2 Feature wiring ─► C3 Polish ─► C4 Release
                            │ B03 Structure renderer   B11 Display modes    │
                            │ B04 Materials & setup    B12 Measure          │
                            │ B05 Input gestures       B13 Takeoff          │
                            │ B06 Camera               B14 Labels           │
                            │ B07 UI shell                                  │
                            └ B08 WebGL build & perf spike                  ┘
```

- **A** phases build the shared base: assemblies, EventBus, CommandHistory, and the **contracts** (domain types, events, interfaces, test fixtures, fakes).
- **B** phases depend **only on A1 + A2**. Each one owns its folders, ships its own tests against fixtures/fakes, and never edits files owned by another B phase. They can be done in any order or in parallel (separate sessions/worktrees).
- **C** phases wire B outputs together. C1 needs B01–B08; C2 needs B09–B14.

## Independence rules for B phases
1. Only reference types from A2 (and your own phase). Need something from another feature? It must be in A2 — if it isn't, add it to A2 first (small, reviewed change), never reach into another B folder.
2. Only create/edit files in the folders listed under **Owns** in your phase file.
3. Don't touch toolbar wiring, keyboard shortcuts, `AppBootstrap` or panel mounting — expose public methods/`VisualElement`s and list them under **Hand-off to C** instead.
4. Tests use `TestStructures` + fakes from `StructureViewer.Tests.Fixtures`; no dependency on the parser, generator, or real scene.
5. Done = own tests green + hand-off list written.

## Phases

| ID | Phase | Est. | Needs | Status |
|---|---|---|---|---|
| A1 | [Foundation](a1-foundation.md) | 1 h | — | ☑ (build target switch pending) |
| A2 | [Contracts, fixtures & fakes](a2-contracts.md) | 1.5 h | A1 | ☑ |
| B01 | [JSON parser & load use case](b01-json-parser.md) | 1 h | A2 | ☑ |
| B02 | [Sample house generator](b02-sample-generator.md) | 1.5 h | A2 | ☐ |
| B03 | [Geometry & structure renderer](b03-structure-renderer.md) | 1.5 h | A2 | ☐ |
| B04 | [Materials, textures & scene setup](b04-materials-setup.md) | 1 h | A2 | ☐ |
| B05 | [Input gestures](b05-input-gestures.md) | 1 h | A2 | ☐ |
| B06 | [Camera controller](b06-camera.md) | 1 h | A2 | ☐ |
| B07 | [UI shell](b07-ui-shell.md) | 1.5 h | A2 | ☐ |
| B08 | [WebGL build & perf spike](b08-webgl-perf-spike.md) | 1 h | A2 | ☐ |
| B09 | [Selection & info panel](b09-selection-info.md) | 1.5 h | A2 | ☐ |
| B10 | [Visibility & undo](b10-visibility-undo.md) | 1.5 h | A2 | ☐ |
| B11 | [Display modes](b11-display-modes.md) | 2 h | A2 | ☐ |
| B12 | [Measure](b12-measure.md) | 1.5 h | A2 | ☐ |
| B13 | [Material takeoff](b13-takeoff.md) | 1 h | A2 | ☐ |
| B14 | [Assembly labels](b14-labels.md) | 0.75 h | A2 | ☐ |
| C1 | [Core viewer composition](c1-core-viewer.md) | 1 h | B01–B08 | ☐ |
| C2 | [Feature wiring & end-to-end tests](c2-feature-wiring.md) | 1.5 h | C1, B09–B14 | ☐ |
| C3 | [Polish](c3-polish.md) | 1 h | C2 | ☐ |
| C4 | [Release: build, devices, deploy, README](c4-release.md) | 2 h | C3 | ☐ |

Total ≈ 26 h of work. The contracts + composition phases add ~4–5 h over a tightly coupled plan; that cost is only repaid if B phases actually run in parallel. Working solo, use this order so something is visible by the end of day 1:

**Day 1**: A1 → A2 → B08 (perf spike first: de-risk mobile) → B01 → B02 → B03 → B04 → B05 → B06 → B07 → **C1** (house on screen, phone build)
**Day 2**: B09 → B10 → B11 → B12 → B13 → B14 → **C2** → C3 → C4

If behind schedule, apply the DESIGN.md cut order: B14 labels → "visible only" in B13 → midpoint snapping in B12.

## Decision gates
- **B08**: phone FPS with ~800 boxes < 30 → B03 must implement the assembly mesh-combine fallback (DESIGN.md › Rendering).
- **C2**: X-ray on the phone < 30 FPS → hide slab/sheathing in X-ray rather than cutting the mode.

## Definition of done (every phase)
- [ ] Own tests green (EditMode; PlayMode where the phase lists them); no new compile warnings.
- [ ] Mouse **and** touch paths covered where the phase has input (fakes in B, real devices in C).
- [ ] Phase checklist ticked, status table updated, **Hand-off to C** section filled in (B phases).
- [ ] Committed with an imperative English message.
