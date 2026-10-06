# Structure Viewer

Unity 6 (`6000.6.3f1`) + URP project. **Target platform: WebGL, must work well on both PC and mobile browsers.** Unity only, no backend. UI Toolkit for UI.

Feature spec, data schema and scope: [docs/DESIGN.md](docs/DESIGN.md). Read it before implementing or changing a feature; anything listed under "Out" needs the user's OK first.

## Ground rules

- **Everything in English**: code, identifiers, comments, commit messages, asset/folder names, log messages, docs.
- Follow **SOLID**, but layer only as deep as the feature needs (see "Avoid over-engineering").
- Comment only non-obvious logic (why, not what). One short line is the norm; no XML doc on self-explanatory members, no banner/section comments.
- Every change to important or bug-prone logic ships with tests (see "Testing").
- **Every feature must work well on PC and mobile** (see "PC + mobile"). A feature that only works with mouse/keyboard is not done.

## Architecture

MVP + use cases + commands + event bus. Dependencies point inward only:

```
View (MonoBehaviour) ──► Presenter ──► UseCase / Command ──► Domain
                              ▲                │
                              └── EventBus ◄───┘
Infrastructure (loading, persistence, WebGL interop) implements interfaces owned by Domain/Application.
```

| Layer | Responsibility | Rules |
|---|---|---|
| **Domain** | Models, value objects, pure rules | Plain C#. No `UnityEngine` except math/value types (`Vector3`, `Bounds`, `Color`). |
| **Application** | Use cases, commands, service interfaces, events | Plain C#, no MonoBehaviours. One use case = one user intention (`LoadStructureUseCase`, `IsolateElementUseCase`). |
| **Presentation** | Presenters + Views | Presenter is plain C#; View is a passive MonoBehaviour. |
| **Infrastructure** | `UnityWebRequest` loaders, storage, JS interop, Unity-specific adapters | Implements Application interfaces. Never referenced by Domain/Application. |
| **Bootstrap** | Composition root | Single place that constructs and wires everything (manual DI, no DI framework). |

### MVP
- **View**: exposes `I<Name>View` (events for input + methods to render state). Holds no logic, no references to use cases or other views.
- **Presenter**: subscribes to view events, calls use cases / dispatches commands, pushes state back to the view. Implements `IDisposable` and unsubscribes everything in `Dispose`.
- Presenters never touch `GameObject`/`Transform` directly — that goes through the view interface so presenters stay EditMode-testable.

### Use cases
- Orchestrate domain + infrastructure for one intention. Expose a single public method (`Execute` / `ExecuteAsync`).
- Return results or publish events; do not reach into presentation.

### Commands
- Use `ICommand { void Execute(); void Undo(); }` only for **user actions that must be undoable/replayable** (select, hide, isolate, transform, section cut…).
- `CommandHistory` owns undo/redo stacks. Executing a new command clears the redo stack.
- Read-only operations or one-shot loads are use cases, not commands.

### Event bus
- Typed, synchronous: `Publish<T>(T evt)`, `Subscribe<T>(Action<T>)` returning `IDisposable`.
- Events are immutable `readonly struct`s named in past tense (`StructureLoaded`, `SelectionChanged`).
- Use it for **cross-feature notifications only**. Direct calls stay direct: view↔presenter uses the view interface, presenter→use case is a method call.
- Every subscription must be disposed by its owner.

### Avoid over-engineering
- No interface with a single implementation unless it is a seam for tests or a platform boundary (views, infrastructure).
- No use case that is a one-line pass-through — let the presenter call the service.
- No command for non-undoable actions. No event when a direct call works.
- No generic base classes / frameworks "for later". Add abstraction on the second real use, not the first.
- Prefer composition and small classes over inheritance hierarchies.

## Project layout

```
Assets/_Project/
  Scripts/
    Domain/            StructureViewer.Domain.asmdef
    Application/       StructureViewer.Application.asmdef   (UseCases/, Commands/, Events/, Interfaces)
    Presentation/      StructureViewer.Presentation.asmdef  (<Feature>/<Feature>Presenter.cs, <Feature>View.cs, I<Feature>View.cs)
    Infrastructure/    StructureViewer.Infrastructure.asmdef
    Bootstrap/         StructureViewer.Bootstrap.asmdef
    Editor/            StructureViewer.Editor.asmdef        (Editor only: sample generator, scene setup, build tools)
  Tests/
    Fixtures/          StructureViewer.Tests.Fixtures.asmdef (TestStructures + fakes, shared by both test assemblies)
    EditMode/          StructureViewer.Tests.EditMode.asmdef (Editor only)
    PlayMode/          StructureViewer.Tests.PlayMode.asmdef
  Data/ Scenes/ Prefabs/ Materials/ Shaders/ Art/ UI/
```

Implementation plan, one file per phase: [docs/plan/](docs/plan/README.md). Tick checklists and update the status table as phases complete. In a B (feature) phase, only touch the folders that phase **Owns**; cross-feature types go in A2 contracts.

- Group by **feature inside each layer** (`Presentation/Selection/`, `Application/Selection/`), not by type.
- Asmdef references must respect layer direction. Reference other asmdefs **by name**, not `GUID:` (GUIDs don't exist until Unity imports the asset).
- Test asmdefs: `defineConstraints: ["UNITY_INCLUDE_TESTS"]`, `overrideReferences: true` + `precompiledReferences: ["nunit.framework.dll"]`, reference `UnityEngine.TestRunner` (and `UnityEditor.TestRunner` for EditMode, with `includePlatforms: ["Editor"]`).
- Namespaces mirror folders: `StructureViewer.<Layer>.<Feature>`.

## C# conventions

- `PascalCase` types/methods/properties, `_camelCase` private fields, `camelCase` locals/params, `I` prefix for interfaces.
- `[SerializeField] private` instead of public fields. No `GameObject.Find`, `FindObjectOfType`, or singletons — wire through Bootstrap.
- `sealed` by default for classes not designed for inheritance.
- Inside `StructureViewer.*`, the `StructureViewer.Application` namespace shadows `UnityEngine.Application`: write `UnityEngine.Application.isMobilePlatform`. Don't name feature folders after Unity types you use (`Camera`, `PointerType`…) — the mirrored namespace/type would shadow them.
- Avoid allocations in `Update` and hot paths (no LINQ, closures, string concat, boxing there).
- Async: use Unity's `Awaitable` (main thread). Always pass/observe a `CancellationToken` (e.g. `destroyCancellationToken`).

## PC + mobile

- **Input**: every interaction supports mouse and touch (Input System pointer/touch APIs). Gesture mapping lives in one input adapter, not scattered across views.
- **No keyboard-only or hover-only features**: every shortcut has an on-screen button; hover is a bonus, never the only way to see information.
- **Responsive UI**: layouts work from ~360 px wide portrait up to desktop. Touch targets ≥ 44 px. Panels collapse on narrow screens. Respect safe areas.
- **Performance**: no realtime shadows, no SSAO or other heavy post-processing. Target 60 FPS on desktop, ≥ 30 FPS on a mid-range phone. Keep draw calls and texture sizes low (≤ 1024 px textures, compressed).
- **Quality**: use `Mobile_RPAsset` when `Application.isMobilePlatform`, `PC_RPAsset` otherwise. Don't add a setting that is only tuned for one of them.
- Size thresholds (snap radius, drag threshold) scale with screen DPI / pointer type.

## WebGL constraints

- **No threads**: no `Thread`, `Task.Run`, `Parallel`, blocking `.Result`/`.Wait()`. Everything runs on the main thread.
- **No sync file/network IO**: load via `UnityWebRequest` / Addressables. `System.IO.File` doesn't work on WebGL; `Application.persistentDataPath` is IndexedDB-backed.
- **IL2CPP stripping**: avoid reflection-based lookups and `System.Reflection.Emit`; add types to `link.xml` if reflection is unavoidable.
- **Memory**: heap is limited — stream/dispose large meshes and textures, call `Destroy` on runtime-created assets, avoid huge temporary arrays.
- **Rendering**: WebGL 2 only. Keep shaders URP-compatible, no compute shaders, no geometry shaders. Watch draw calls (use GPU instancing/SRP batcher).
- JS interop lives in `Assets/_Project/Plugins/WebGL/*.jslib` behind an Infrastructure interface, with a no-op/editor implementation so the editor and tests run without a browser.
- Use `#if UNITY_WEBGL && !UNITY_EDITOR` only inside Infrastructure.

## Testing

Unity Test Framework (`com.unity.test-framework`). Test **behaviour that matters or breaks easily**; skip noise.

**EditMode** (fast, default choice) — plain C# logic:
- Use cases: happy path + failure/edge cases (invalid input, empty structure, cancellation).
- Commands: `Execute` → `Undo` restores exact prior state; redo; history clears redo after new command.
- Event bus: delivery, unsubscribe via `Dispose`, no leak after dispose, publish with no subscribers.
- Presenters: with a fake `I<Name>View` — view event triggers correct use case / command, state rendered back correctly, `Dispose` unsubscribes.
- Domain rules and math (bounds, hierarchy traversal, filtering).

**PlayMode** — only what needs the engine:
- View ↔ presenter wiring on a real prefab, Bootstrap composition producing a working scene.
- Runtime asset loading/unloading (no leaked objects), camera/selection behaviour that depends on physics/raycasts.

**Do not test**: getters/setters, trivial constructors, pure Unity API calls, one-line pass-throughs, serialized field plumbing.

Conventions:
- Name: `MethodOrScenario_Condition_ExpectedResult`. One behaviour per test, Arrange/Act/Assert.
- Hand-written fakes over mocking frameworks. No real network in tests.
- Fix a bug → add a test that reproduces it first.

### Running tests

Batchmode can't open the project while the Editor has it open. **If `Temp/UnityLockfile` exists, don't run these** — ask the user to run them from Window > General > Test Runner and report results.

```bash
"/c/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults ./Logs/editmode-results.xml -logFile ./Logs/editmode.log
```

```bash
"/c/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults ./Logs/playmode-results.xml -logFile ./Logs/playmode.log
```

Non-zero exit = compile error or failed tests. Read failures from the results XML; compile errors are in the `.log` (search `error CS`).

## Workflow

- After changing C#: run EditMode tests (PlayMode too if Views/Bootstrap/Infrastructure changed). Don't report done with failing tests or compile errors.
- Never edit `Library/`, `Temp/`, `Logs/`, `UserSettings/`, or generated `*.csproj`/`*.slnx`.
- Don't hand-write `.meta` files or GUIDs; Unity generates them on import. Commit each asset together with its `.meta`.
- Don't modify `ProjectSettings/` or `Packages/manifest.json` without calling it out explicitly in the summary.
- Commit messages: imperative, English, short subject line.
