# Structure Viewer

Unity 6 (`6000.6.3f1`) + URP project. **Target platform: WebGL.**

## Ground rules

- **Everything in English**: code, identifiers, comments, commit messages, asset/folder names, log messages, docs.
- Follow **SOLID**, but layer only as deep as the feature needs (see "Avoid over-engineering").
- Comment only non-obvious logic (why, not what). One short line is the norm; no XML doc on self-explanatory members, no banner/section comments.
- Every change to important or bug-prone logic ships with tests (see "Testing").

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
  Tests/
    EditMode/          StructureViewer.Tests.EditMode.asmdef (Editor only)
    PlayMode/          StructureViewer.Tests.PlayMode.asmdef
  Scenes/ Prefabs/ Materials/ Shaders/ Art/
```

- Group by **feature inside each layer** (`Presentation/Selection/`, `Application/Selection/`), not by type.
- Asmdef references must respect layer direction; Domain/Application asmdefs set `noEngineReferences` where possible.
- Namespaces mirror folders: `StructureViewer.<Layer>.<Feature>`.

## C# conventions

- `PascalCase` types/methods/properties, `_camelCase` private fields, `camelCase` locals/params, `I` prefix for interfaces.
- `[SerializeField] private` instead of public fields. No `GameObject.Find`, `FindObjectOfType`, or singletons — wire through Bootstrap.
- `sealed` by default for classes not designed for inheritance.
- Avoid allocations in `Update` and hot paths (no LINQ, closures, string concat, boxing there).
- Async: use Unity's `Awaitable` (main thread). Always pass/observe a `CancellationToken` (e.g. `destroyCancellationToken`).

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

Run tests from CLI (Editor must be closed for this project):

```bash
"/c/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults ./Logs/editmode-results.xml -logFile ./Logs/editmode.log
```

```bash
"/c/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults ./Logs/playmode-results.xml -logFile ./Logs/playmode.log
```

## Working in this repo

- Never edit `Library/`, `Temp/`, `Logs/`, `UserSettings/`, or generated `*.csproj`/`*.slnx`.
- Every new asset/folder needs its `.meta` file; let Unity generate it — don't hand-write GUIDs.
- Don't modify `ProjectSettings/` or `Packages/manifest.json` without saying so explicitly in the summary.
