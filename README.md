# Structure Viewer

A demo 3D viewer for timber-frame houses. It runs in the browser on desktop and phone.

[![Live demo: play in browser](https://img.shields.io/badge/Live%20demo-Play%20in%20browser-2ea44f?style=for-the-badge&logo=unity&logoColor=white)](https://www.zuyzuygames.com/structure-viewer/)

- Unity 6
- C#
- URP
- WebGL

[![Orbit, select a wall, isolate it, then X-ray, Color by and Clay](docs/media/demo.gif)](https://www.zuyzuygames.com/structure-viewer/)

Orbit the house, select a stud or a whole wall, hide or isolate parts, switch display modes, measure and check the material list. Works with mouse and touch.

## Built with AI

I built this with Claude Code. AI writes code fast and makes mistakes fast, so I set things up to catch them early:

- **Rules first:** a [spec](docs/DESIGN.md) and a [rules file](CLAUDE.md) keep every session consistent.
- **Small features:** 14 [slices](docs/plan/README.md) that share only a few contracts, so the AI works on one small part at a time.
- **Compiler checks:** each layer is its own assembly, so wrong dependencies don't compile.
- **Tests:** ~390 EditMode and PlayMode tests after every change. Every bug fix starts with a failing test.
- **Human in charge:** I review the code, make the design calls and test on real devices.

## Performance

8.3 MB download, ~1,000 framing members, ~100 draw calls.

| Device | First load | Next loads | FPS |
|---|---|---|---|
| Desktop, Windows and Mac | ~1 s | – | 60 |
| Phone, high end | 2.5 s | 1.6 s | 60 |
| Phone, mid range | 10.3 s | 2.5 s | 60 |
| Phone, low end | 45 s | 6.2 s | ~42 |

Phones: Xiaomi 14T, with Chrome throttling for mid and low end.

What keeps it fast:

- **Draw calls:** one combined mesh per material for each wall or truss. Members keep their own colliders for exact picking.
- **Batching:** shared materials only, so the SRP Batcher works.
- **Updates:** only changed meshes rebuild, at most once per frame. No per-frame allocations.
- **Rendering:** no realtime shadows or post effects.
- **Per platform:** PC or Mobile URP asset picked at startup. Phones get a 0.8 render scale, no MSAA or HDR, and a pixel ratio cap of 2.
- **Build size:** Brotli, high code stripping, 512 px normal maps, no splash screen.
