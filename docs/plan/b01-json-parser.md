# B01 — JSON parser & load use case

**Goal**: JSON text → validated `StructureModel` in Unity space, with every error reported.
**Estimate**: 1 h · **Needs**: A2
**Owns**: `Scripts/Infrastructure/Parsing/` (except `StructureDto.cs`), `Scripts/Application/Loading/LoadStructureUseCase.cs`, `Tests/EditMode/Parsing/`

## Tasks
- [x] `JsonStructureParser : IStructureParser` — `JsonUtility.FromJson<StructureDto>`, validate, convert mm/Z-up → m/Y-up via `ModelAxes`; collect **all** errors (with element id), never throw on bad input.
- [x] Validation (in pure `StructureDtoConverter`, so it is testable without the engine): unknown `units`/`upAxis`; duplicate ids; unknown level/category; section ≤ 0; zero-length member; panel corner count ≠ 12 floats, non-planar (> 1 mm) or degenerate; slab outline < 3 points or non-convex; missing `roll` → 0; missing id/type → error; missing `group` → no group; slab group = its id.
- [x] `LoadStructureUseCase.Execute(string json)` — parse; on success set `StructureSession` + publish `StructureLoaded`; return `ParseResult`.

## Tests (EditMode)
- DESIGN.md sample snippet parses; positions converted `(x, y, z) mm → (x, z, y) m`.
- Each validation rule → error mentioning the element id; several bad elements → several errors.
- Malformed JSON → error, no exception.
- Use case: success publishes `StructureLoaded` once; failure publishes nothing and leaves the session untouched.

## Hand-off to C
- C1: call `LoadStructureUseCase.Execute(textAsset.text)` from `AppBootstrap`; show `Errors` via `IShell.ShowToast`.
- C1: generator ↔ parser contract test (generated house parses with zero errors).
