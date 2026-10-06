# A1 — Foundation

**Goal**: correctly wired empty project — assemblies, EventBus, CommandHistory, render settings that follow the PC + mobile rules, and a working CLI test run.
**Estimate**: 1 h · **Needs**: —

## Tasks

### Cleanup & layout
- [x] Delete URP template leftovers: `Assets/Readme.asset`, `Assets/TutorialInfo/`.
- [x] Create the `Assets/_Project/` layout from CLAUDE.md. Rename/move `SampleScene` → `Assets/_Project/Scenes/Main.unity` via the Editor/AssetDatabase (keeps GUIDs).

### Assemblies (reference by name)
| Asmdef | References |
|---|---|
| `StructureViewer.Domain` | — (engine refs allowed for math types) |
| `StructureViewer.Application` | Domain |
| `StructureViewer.Infrastructure` | Domain, Application |
| `StructureViewer.Presentation` | Domain, Application, `Unity.InputSystem` |
| `StructureViewer.Bootstrap` | all runtime |
| `StructureViewer.Editor` (Editor only) | all runtime |
| `StructureViewer.Tests.Fixtures` (`UNITY_INCLUDE_TESTS`) | all runtime, `UnityEngine.TestRunner` |
| `StructureViewer.Tests.EditMode` (Editor only) | all runtime, Editor, Fixtures |
| `StructureViewer.Tests.PlayMode` | all runtime, Fixtures, `Unity.InputSystem` |

### Core
- [x] `Application/Events/EventBus.cs` — `Publish<T>`, `Subscribe<T>(Action<T>) → IDisposable`; iterate a snapshot so unsubscribing during publish is safe.
- [x] `Application/Commands/ICommand.cs`, `CommandHistory.cs` — `Execute`, `Undo`, `Redo`, `CanUndo`, `CanRedo`, `Clear`, `Changed` event, cap 100.

### Render / project settings (⚠ call out in commit)
- [x] `PC_RPAsset` + `Mobile_RPAsset`: main + additional light shadows off; HDR off on mobile; MSAA off on mobile (4x on PC). Depth/opaque textures and GPU Resident Drawer off (unused, not supported on WebGL).
- [x] `PC_Renderer`: remove the active **ScreenSpaceAmbientOcclusion** feature.
- [ ] Build target → Web (WebGL) — Editor action (File > Build Profiles > Web > Switch Platform). `activeInputHandler` is already Input System only.

## Tests (EditMode)
- EventBus: typed delivery only; dispose stops delivery; double dispose safe; unsubscribe inside handler doesn't skip others; no subscribers = no-op.
- CommandHistory: undo/redo order; execute clears redo; undo on empty no-op; `Changed` fires on each change; cap drops oldest.

## Done when
- Everything compiles, CLI EditMode run is green, shadows/SSAO off in both pipelines.
