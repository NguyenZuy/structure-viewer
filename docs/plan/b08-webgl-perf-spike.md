# B08 — WebGL build & perf spike

**Goal**: a repeatable WebGL build pipeline and an early answer to "can a phone draw ~800 members at ≥ 30 FPS?" — before the renderer is finished.
**Estimate**: 1 h · **Needs**: A2 · **Do this early** (it de-risks B03)
**Owns**: `Scripts/Infrastructure/Quality/`, `Scripts/Editor/Build/`, `Sandbox/PerfSpike*`, WebGL Player settings (⚠ ProjectSettings — call out in commit)

## Tasks
- [ ] `QualitySelector` (Infrastructure): picks the Mobile or PC quality level (→ URP asset) from `Application.isMobilePlatform` at startup.
- [ ] Player settings: WebGL 2 only, Linear, Brotli + **Decompression Fallback**, Data Caching, IL2CPP "Disk size", Managed Stripping **Low**, minimal template with mobile viewport meta, canvas fills the window.
- [ ] `Tools > Structure Viewer > Build WebGL` (`BuildPipeline`) → `Builds/WebGL`; also `-executeMethod` for CLI.
- [ ] Spike scene `Sandbox/PerfSpike.unity` + script: spawns 800 boxes (shared lit material, 1K texture) in a house-sized volume, slow auto-orbit, on-screen FPS; a toggle switching all to a transparent material (X-ray worst case).
- [ ] Serve on LAN (`python -m http.server` in `Builds/WebGL`), open on a phone and desktop; record results below.

## Results (fill in)
| Device / browser | Opaque FPS | Transparent FPS | Load time | Build size |
|---|---|---|---|---|
| | | | | |

## Decision
- Opaque ≥ 30 FPS on the phone → B03 keeps one GameObject per element.
- Opaque < 30 FPS → B03 implements the assembly mesh-combine fallback.
- Transparent < 30 FPS → note for C2 (hide slab/sheathing in X-ray).

## Tests
- EditMode: `QualitySelector` maps mobile → Mobile level, desktop → PC level (logic extracted to a pure function).

## Hand-off to C
- C1: call `QualitySelector` first thing in `AppBootstrap`; build `Main.unity` instead of the spike scene. Delete the spike scene before C4.
