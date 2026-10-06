# C4 — Release: build, devices, deploy, README

**Goal**: verified release build, live in the portfolio, README that presents the project honestly.
**Estimate**: 2 h · **Needs**: C3

## Release build
- [ ] Full EditMode + PlayMode run from CLI — green before building.
- [ ] Re-check Player settings (B08); non-development build via `Tools > Structure Viewer > Build WebGL`. Record build size.

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
