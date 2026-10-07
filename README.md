# Structure Viewer

A 3D viewer for timber-frame houses. It runs in the browser on both desktop and phone.

[![Live demo: play in browser](https://img.shields.io/badge/Live%20demo-Play%20in%20browser-2ea44f?style=for-the-badge&logo=unity&logoColor=white)](https://www.zuyzuygames.com/structure-viewer/)

Unity 6 · C# · URP · WebGL

[![Orbit, select a wall, isolate it, then X-ray, Color by and Clay](docs/media/demo.gif)](https://www.zuyzuygames.com/structure-viewer/)

You can orbit around the house, select a stud or a whole wall, hide or isolate parts, switch between four display modes, measure distances and see a material list. It all works with mouse and touch, and you can undo what you hide or isolate.

## Code

I used MVP with use cases, commands and a small event bus. Domain and Application are plain C#, so most of the logic is tested without Unity. Each layer is a separate assembly, so a wrong dependency won't compile. Everything is wired by hand in one Bootstrap class, with no singletons.

| Pattern | Used for |
|---|---|
| MVP | Every UI panel. Presenters are tested with fake views. |
| Command | Hide, isolate and show all, with undo and redo |
| Strategy | The four display modes |
| Observer | The event bus between features |

There are about 390 tests, EditMode and PlayMode.

## Performance

The house has about 1,000 framing members but needs only around 100 draw calls. Each wall or truss is one mesh per material, and every member keeps its own collider, so picking is still exact. The build is 8.3 MB and runs at 60 FPS on a Xiaomi 14T.

## Run it

Open the project in Unity `6000.6.3f1`, run **Tools > Structure Viewer > Setup Scene**, then **Build WebGL**. The full spec is in [docs/DESIGN.md](docs/DESIGN.md).

The sample house is generated and not engineered. Textures are from [ambientCG](https://ambientcg.com/) (CC0).
