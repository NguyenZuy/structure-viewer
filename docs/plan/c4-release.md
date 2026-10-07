# C4 — Release: build, devices, deploy, README

**Goal**: verified release build, live in the portfolio, README that presents the project honestly.
**Estimate**: 2 h · **Needs**: C3

## Release build
- [ ] Full EditMode + PlayMode run from CLI — green before building.
- [ ] Re-check Player settings (B08); non-development build via `Tools > Structure Viewer > Build WebGL`. Record build size.

### Build size analysis (2026-10-07)
First release build: **10.8 MB** download (Brotli), 26 min build (Master + DiskSizeLTO).

| Part | Brotli | Raw | Notes |
|---|---|---|---|
| `wasm` | 5.8 MB | 21 MB | UI Toolkit, mscorlib, RP Core, Input System, URP dominate; our code ~150 KB IL |
| IL2CPP metadata (in `.data`) | 1.8 MB | 6.2 MB | Scales with code |
| Assets `data.unity3d` | 3.0 MB | 3.4 MB | ~2 MB is the two normal maps (ASTC 4x4, 1024); ASTC barely compresses |
| Engine resources, JS | 0.35 MB | | |

Changes for the next build: splash screen off, managed stripping Low → High (release only; DTOs kept by `Infrastructure/Parsing/link.xml`), normal maps and concrete albedo at 512 px (`AssetSetup.DetailTextureSize`), post-processing data removed from both renderers. Unused packages were left: the linker already drops them.

Second build: **8.3 MB** (−23%), 12 min. `wasm` 5.34 MB, metadata 1.28 MB, assets 1.53 MB, engine resources 0.15 MB. Code is now ~80% of the download and High is the strongest stripping level; what remains is mostly UI Toolkit, URP and the Input System.

## Device matrix
| Feature | Chrome (Win) | Firefox (Win) | Edge (Win) | Android Chrome | iOS Safari |
|---|---|---|---|---|---|
| Load + fit all | ☐ | ☐ | ☐ | ☐ | ☐ |
| Camera (mouse / touch) | ☐ | ☐ | ☐ | ☐ | ☐ |
| Select / assembly / multi | ☐ | ☐ | ☐ | ☐ | ☐ |
| Layers / isolate / hide / undo | ☐ | ☐ | ☐ | ☐ | ☐ |
| 4 display modes + legend | ☐ | ☐ | ☐ | ☐ | ☐ |
| Measure | ☐ | ☐ | ☐ | ☐ | ☐ |
| Takeoff | ☐ | ☐ | ☐ | ☐ | ☐ |
| Labels | ☐ | ☐ | ☐ | ☐ | ☐ |
| Portrait + landscape | — | — | — | ☐ | ☐ |

Untested targets are marked as such in the README, not claimed.

## Performance (real numbers)
- [ ] FPS in Realistic and X-ray on desktop and phone (device + browser named).
- [ ] Draw calls/batches from Editor Stats / Frame Debugger (label the source — not available in release builds).
- [ ] Cold and cached load time, build size.

## Deploy
- [ ] Ask which host the portfolio uses; copy `Builds/WebGL` (Decompression Fallback → no custom headers needed).
- [ ] Embed: `<iframe src="…/index.html" style="width:100%;aspect-ratio:16/10;border:0" allow="fullscreen"></iframe>`; check on a phone (page scroll vs canvas gestures, fullscreen).

## README
- [ ] Hero GIF (desktop) + phone clip, live demo link.
- [ ] Features, controls table (mouse + touch).
- [ ] Architecture (Mermaid: layers, EventBus, commands, palettes) + key decisions (index-based elements, immutable visibility snapshots, palette strategies, generated meshes for wood grain, contracts-first phases).
- [ ] Tests: what's covered and why, how to run.
- [ ] Performance table.
- [ ] Credits: ambientCG (CC0) with links. Disclaimer: hypothetical sample data, not engineered; not affiliated with any vendor.

## Wrap-up
- [ ] Tag `v1.0`, push.
