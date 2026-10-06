# B02 — Sample house generator

**Goal**: editor menu that writes a believable two-storey timber house (500–800 members) as `StructureDto` JSON.
**Estimate**: 1.5 h · **Needs**: A2
**Owns**: `Scripts/Editor/Generator/`, `Data/sample-house.json`, `Tests/EditMode/Generator/`

## Key decisions
- Pure C# producing `StructureDto` in **model space** (mm, Z-up) — exactly what a real exporter would write. Menu item only serialises.
- Plausible, not engineered (README says so).

## Spec (`SampleHouseSpec` defaults)
| Item | Value |
|---|---|
| Footprint | 10 000 × 8 000 mm |
| Levels | L0 @ 0, L1 @ 2 700, RF @ 5 400 |
| Walls | 90 × 35 studs @ 600, single bottom plate, double top plate, mid-height noggings; 4 external + 1 internal (y = 4 000) per level |
| Openings | L0: door 820 × 2 100 + 2 windows (front); L1: 3 windows (front); 1 window per side wall per level. Kept ≥ 300 mm from corners |
| Floor L1 | Joists 240 × 45 @ 450 in two bays, bearer 290 × 90 along X at y = 4 000 |
| Roof | Gable 22.5°, Fink trusses @ 600 along X (~17), 90 × 35 |
| Sheathing | 2 roof panels, 12 mm |
| Slab | 10 000 × 8 000 × 300, top at 0 |

## Tasks
- [ ] `SampleHouseSpec`, `Opening`.
- [ ] `WallFrameBuilder` — plates, studs skipping openings, per opening: 2 trimmers, lintel, sill (windows), cripples above/below, noggings split around studs/openings.
- [ ] `FloorFrameBuilder` — bearer + joists.
- [ ] `TrussBuilder` — one Fink truss (BC, 2 TC, 4 webs) at X.
- [ ] `SampleHouseGenerator.Generate(spec) → StructureDto` with ids/groups: walls `W-L0-N` (`W-L0-N-ST07`, `-LT01`…), floor `FL-L1-A/B`, bearer `FL-L1-BR`, trusses `T01…` (`T03-TC1`, `T03-BC`, `T03-W2`), sheathing `ROOF-N/S`, slab `SLAB-1`.
- [ ] Menu `Tools > Structure Viewer > Generate Sample House` → writes `Assets/_Project/Data/sample-house.json` (pretty), refreshes, logs counts.

## Tests (EditMode — on the DTO, no parser)
- Ids unique; member count 500–800; every member has non-zero length and a known level id.
- **No stud intersects an opening**; each opening has 2 trimmers + 1 lintel (+ 1 sill for windows).
- Truss count = `floor(length / spacing) + 1`; top chord pitch 22.5° ± 0.5°.
- All points within footprint (+ overhang tolerance); panel corners coplanar.

## Hand-off to C
- C1: generator ↔ parser contract test; reference `sample-house.json` from `AppBootstrap`.
