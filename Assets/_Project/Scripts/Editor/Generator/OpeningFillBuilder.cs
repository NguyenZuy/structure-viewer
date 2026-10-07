using System.Collections.Generic;
using UnityEngine;

namespace StructureViewer.Editor.Generator
{
    // Door leaves, window glazing and colonial trim so the house reads as finished in Realistic. Leaves and glazing fill their
    // framed opening (between the trimmers, from sill to lintel), centred in the wall's depth; windows get a 6-over-6 muntin
    // grid, and every external opening a white casing on the sheathing. All of it is grouped with its wall like the sheathing.
    public static class OpeningFillBuilder
    {
        public const string Category = "Opening";
        public const string DoorType = "Door";
        public const string WindowType = "Window";
        public const string CasingType = "Casing";
        public const string MuntinType = "Muntin";

        public const float DoorThickness = 40f;
        public const float GlassThickness = 6f;
        public const float CasingWidth = 90f;
        public const float CasingThickness = 20f;
        public const float MuntinWidth = 22f;
        public const float MuntinDepth = 20f;
        public const float MeetingRailWidth = 40f;
        public const float MeetingRailDepth = 40f;
        private const float DoorGap = 3f;
        private const float DoorFloorGap = 10f;
        private const float WideWindow = 1100f;

        public static void Build(SampleHouseSpec spec, StructureDtoBuilder sink)
        {
            var centre = new Vector2(spec.Length * 0.5f, spec.Depth * 0.5f);
            foreach (var wall in SampleHouseLayout.Walls(spec))
            {
                var ids = new Counter(wall.Id);
                bool external = !wall.Id.EndsWith("-I");
                // +1 when (direction × up) already points out of the house.
                var normal = Normal(wall);
                float outward = Vector2.Dot(normal, (wall.Start + wall.End) * 0.5f - centre) > 0f ? 1f : -1f;
                float face = spec.StudDepth * 0.5f + spec.SheathingThickness;

                foreach (var opening in wall.Openings)
                {
                    float u0 = opening.Offset, u1 = opening.End, v0 = opening.Sill, v1 = opening.Head;
                    if (opening.Kind == OpeningKind.Door)
                    {
                        Centred(sink, wall, ids.Next("DR"), DoorType, DoorThickness, u0 + DoorGap, u1 - DoorGap, v0 + DoorFloorGap, v1 - DoorGap);
                    }
                    else
                    {
                        Centred(sink, wall, ids.Next("WN"), WindowType, GlassThickness, u0, u1, v0, v1);
                        Muntins(sink, wall, ids, u0, u1, v0, v1);
                    }

                    if (!external)
                        continue;
                    float w = CasingWidth;
                    Casing(sink, wall, ids.Next("CA"), outward, face, u0 - w, u1 + w, v1, v1 + w);
                    Casing(sink, wall, ids.Next("CA"), outward, face, u0 - w, u0, v0, v1);
                    Casing(sink, wall, ids.Next("CA"), outward, face, u1, u1 + w, v0, v1);
                    if (opening.Kind == OpeningKind.Window)
                        Casing(sink, wall, ids.Next("CA"), outward, face, u0 - w, u1 + w, v0 - w, v0);
                }
            }
        }

        // Two sashes of 3 (or 4 on wide windows) × 2 panes, split by a deeper meeting rail.
        private static void Muntins(StructureDtoBuilder sink, WallSpec wall, Counter ids, float u0, float u1, float v0, float v1)
        {
            int columns = u1 - u0 >= WideWindow ? 4 : 3;
            for (int i = 1; i < columns; i++)
            {
                float u = Mathf.Lerp(u0, u1, i / (float)columns);
                Centred(sink, wall, ids.Next("MU"), MuntinType, MuntinDepth, u - MuntinWidth * 0.5f, u + MuntinWidth * 0.5f, v0, v1);
            }
            foreach (float t in new[] { 0.25f, 0.75f })
            {
                float v = Mathf.Lerp(v0, v1, t);
                Centred(sink, wall, ids.Next("MU"), MuntinType, MuntinDepth, u0, u1, v - MuntinWidth * 0.5f, v + MuntinWidth * 0.5f);
            }
            float rail = (v0 + v1) * 0.5f;
            Centred(sink, wall, ids.Next("MU"), MuntinType, MeetingRailDepth, u0, u1, rail - MeetingRailWidth * 0.5f, rail + MeetingRailWidth * 0.5f);
        }

        // Starting half the thickness behind the centreline centres the panel in the frame.
        private static void Centred(StructureDtoBuilder sink, WallSpec wall, string id, string type, float thickness,
            float u0, float u1, float v0, float v1) =>
            Quad(sink, wall, id, type, thickness, 1f, -thickness * 0.5f, u0, u1, v0, v1);

        // Sits on the sheathing's outer face and extrudes away from the house.
        private static void Casing(StructureDtoBuilder sink, WallSpec wall, string id, float outward, float face,
            float u0, float u1, float v0, float v1) =>
            Quad(sink, wall, id, CasingType, CasingThickness, outward, face, u0, u1, v0, v1);

        // Panels extrude along (along × up), with along the corner walk. Walking the wall backwards flips the extrusion,
        // so side = -1 extrudes against Normal(wall). offset is measured from the centreline along the extrusion side.
        private static void Quad(StructureDtoBuilder sink, WallSpec wall, string id, string type, float thickness, float side,
            float offset, float u0, float u1, float v0, float v1)
        {
            if (side < 0f)
                (u0, u1) = (u1, u0);
            var dir = wall.Direction;
            var shift = Normal(wall) * (side * offset);
            var a = wall.Start + dir * u0 + shift;
            var b = wall.Start + dir * u1 + shift;
            float z0 = wall.Base + v0;
            float z1 = wall.Base + v1;
            sink.Panel(id, Category, type, wall.Id, wall.LevelId, thickness,
                new[] { new Vector3(a.x, a.y, z0), new Vector3(b.x, b.y, z0), new Vector3(b.x, b.y, z1), new Vector3(a.x, a.y, z1) });
        }

        private static Vector2 Normal(WallSpec wall) => new Vector2(wall.Direction.y, -wall.Direction.x);

        private sealed class Counter
        {
            private readonly string _prefix;
            private readonly Dictionary<string, int> _counts = new Dictionary<string, int>();

            public Counter(string prefix) => _prefix = prefix;

            public string Next(string code)
            {
                _counts.TryGetValue(code, out int n);
                _counts[code] = ++n;
                return $"{_prefix}-{code}{n:00}";
            }
        }
    }
}
