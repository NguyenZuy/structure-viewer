using UnityEngine;

namespace StructureViewer.Editor.Generator
{
    // Door leaves and window glazing so the house reads as finished in Realistic. Each fills its framed opening (between the
    // trimmers, from sill to lintel), centred in the wall's depth and grouped with the wall like its sheathing.
    public static class OpeningFillBuilder
    {
        public const string Category = "Opening";
        public const string DoorType = "Door";
        public const string WindowType = "Window";

        public const float DoorThickness = 40f;
        public const float GlassThickness = 6f;
        private const float DoorGap = 3f;
        private const float DoorFloorGap = 10f;

        public static void Build(SampleHouseSpec spec, StructureDtoBuilder sink)
        {
            foreach (var wall in SampleHouseLayout.Walls(spec))
            {
                int doors = 0;
                int windows = 0;
                foreach (var opening in wall.Openings)
                {
                    if (opening.Kind == OpeningKind.Door)
                        Fill(sink, wall, $"{wall.Id}-DR{++doors:00}", DoorType, DoorThickness,
                            opening.Offset + DoorGap, opening.End - DoorGap, opening.Sill + DoorFloorGap, opening.Head - DoorGap);
                    else
                        Fill(sink, wall, $"{wall.Id}-WN{++windows:00}", WindowType, GlassThickness,
                            opening.Offset, opening.End, opening.Sill, opening.Head);
                }
            }
        }

        // Corners walk along the wall then up, so the panel extrudes along (direction × up); starting half the thickness
        // behind the centreline centres it in the frame.
        private static void Fill(StructureDtoBuilder sink, WallSpec wall, string id, string type, float thickness,
            float u0, float u1, float v0, float v1)
        {
            var dir = wall.Direction;
            var normal = new Vector2(dir.y, -dir.x);
            var a = wall.Start + dir * u0 - normal * (thickness * 0.5f);
            var b = wall.Start + dir * u1 - normal * (thickness * 0.5f);
            float z0 = wall.Base + v0;
            float z1 = wall.Base + v1;
            sink.Panel(id, Category, type, wall.Id, wall.LevelId, thickness,
                new[] { new Vector3(a.x, a.y, z0), new Vector3(b.x, b.y, z0), new Vector3(b.x, b.y, z1), new Vector3(a.x, a.y, z1) });
        }
    }
}
