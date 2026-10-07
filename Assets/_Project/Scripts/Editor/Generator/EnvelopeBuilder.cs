using System.Collections.Generic;
using UnityEngine;

namespace StructureViewer.Editor.Generator
{
    // Sheathing that makes the frame read as a house: external wall sheathing (cut around openings) on both storeys,
    // gable ends and first-floor decking. Panels share their frame's group, so selecting or isolating a wall includes its sheathing.
    public static class EnvelopeBuilder
    {
        public const string WallType = "WallSheathing";
        public const string FloorType = "FloorSheathing";

        // An external wall's outer face, walked so that (along × up) points outward: that is the panel extrusion side
        // (right-handed corner normal, see PanelMeshBuilder).
        private readonly struct Face
        {
            public Face(string side, Vector2 origin, Vector2 along, float length)
            {
                Side = side;
                Origin = origin;
                Along = along;
                Length = length;
            }

            public string Side { get; }
            public Vector2 Origin { get; }
            public Vector2 Along { get; }
            public float Length { get; }

            public float Project(Vector2 plan) => Vector2.Dot(plan - Origin, Along);
        }

        public static void Build(SampleHouseSpec spec, StructureDtoBuilder sink)
        {
            float t = spec.SheathingThickness;
            // Front and back run the full length; the side faces also cover the front/back sheathing edges to close the corners.
            var faces = new[]
            {
                new Face("S", new Vector2(0f, 0f), Vector2.right, spec.Length),
                new Face("N", new Vector2(spec.Length, spec.Depth), Vector2.left, spec.Length),
                new Face("E", new Vector2(spec.Length, -t), Vector2.up, spec.Depth + 2f * t),
                new Face("W", new Vector2(0f, spec.Depth + t), Vector2.down, spec.Depth + 2f * t)
            };
            var walls = new Dictionary<string, WallSpec>();
            foreach (var wall in SampleHouseLayout.Walls(spec))
                walls.Add(wall.Id, wall);

            for (int level = 0; level < 2; level++)
            {
                string levelId = SampleHouseLayout.LevelIds[level];
                float bottom = level * spec.StoreyHeight;
                float top = bottom + spec.StoreyHeight;
                foreach (var face in faces)
                {
                    var wall = walls[$"W-{levelId}-{face.Side}"];
                    int n = 0;
                    foreach (var (u0, u1, z0, z1) in Pieces(face.Length, bottom, top, Openings(wall, face)))
                    {
                        n++;
                        WallPanel(sink, face, $"{wall.Id}-SH{n:00}", wall.Id, levelId, t, u0, u1, z0, z1);
                    }
                }
            }

            Gable(spec, sink, faces[2]);
            Gable(spec, sink, faces[3]);
            FloorDeck(spec, sink);
        }

        // Panels are quads, so the gable triangle is a quad whose top edge is 2 mm wide, under the roof sheathing at the ridge.
        private static void Gable(SampleHouseSpec spec, StructureDtoBuilder sink, Face face)
        {
            float bottom = 2f * spec.StoreyHeight;
            float lift = spec.TrussDepth * 0.5f / Mathf.Cos(spec.RoofPitch * Mathf.Deg2Rad);
            float apex = TrussBuilder.ChordBaseZ(spec) + spec.Depth * 0.5f * TrussBuilder.Slope(spec) + lift;
            float mid = face.Length * 0.5f;
            var a = face.Origin;
            var b = face.Origin + face.Along * face.Length;
            var c = face.Origin + face.Along * (mid + 1f);
            var d = face.Origin + face.Along * (mid - 1f);
            string group = $"W-L1-{face.Side}";
            sink.Panel($"{group}-GB01", WallType, group, "RF", spec.SheathingThickness,
                new Vector3(a.x, a.y, bottom), new Vector3(b.x, b.y, bottom), new Vector3(c.x, c.y, apex), new Vector3(d.x, d.y, apex));
        }

        // The frame's openings projected onto the face: (from, to) along it, sill and head as absolute heights.
        private static List<(float From, float To, float Sill, float Head)> Openings(WallSpec wall, Face face)
        {
            var result = new List<(float, float, float, float)>();
            foreach (var opening in wall.Openings)
            {
                float a = face.Project(wall.Start + wall.Direction * opening.Offset);
                float b = face.Project(wall.Start + wall.Direction * opening.End);
                result.Add((Mathf.Min(a, b), Mathf.Max(a, b), wall.Base + opening.Sill, wall.Base + opening.Head));
            }
            result.Sort((x, y) => x.Item1.CompareTo(y.Item1));
            return result;
        }

        // Full-height strips between openings, plus the pieces above (and below, for windows) each opening.
        private static IEnumerable<(float U0, float U1, float Z0, float Z1)> Pieces(float length, float bottom, float top,
            List<(float From, float To, float Sill, float Head)> openings)
        {
            float cursor = 0f;
            foreach (var (from, to, sill, head) in openings)
            {
                if (from > cursor)
                    yield return (cursor, from, bottom, top);
                if (sill > bottom)
                    yield return (from, to, bottom, sill);
                if (head < top)
                    yield return (from, to, head, top);
                cursor = to;
            }
            if (cursor < length)
                yield return (cursor, length, bottom, top);
        }

        private static void WallPanel(StructureDtoBuilder sink, Face face, string id, string group, string level, float thickness,
            float u0, float u1, float z0, float z1)
        {
            var a = face.Origin + face.Along * u0;
            var b = face.Origin + face.Along * u1;
            sink.Panel(id, WallType, group, level, thickness,
                new Vector3(a.x, a.y, z0), new Vector3(b.x, b.y, z0), new Vector3(b.x, b.y, z1), new Vector3(a.x, a.y, z1));
        }

        // Flush with the top of the joists and extruded down into them, so first-floor walls stand on it. One sheet per joist bay.
        private static void FloorDeck(SampleHouseSpec spec, StructureDtoBuilder sink)
        {
            float z = spec.StoreyHeight;
            float mid = spec.Depth * 0.5f;
            Deck(sink, $"{FloorFrameBuilder.BayA}-DK01", FloorFrameBuilder.BayA, spec.FloorSheathingThickness, 0f, mid, spec.Length, z);
            Deck(sink, $"{FloorFrameBuilder.BayB}-DK01", FloorFrameBuilder.BayB, spec.FloorSheathingThickness, mid, spec.Depth, spec.Length, z);
        }

        // Corners walk +Y first, then +X: the right-handed normal points down.
        private static void Deck(StructureDtoBuilder sink, string id, string group, float thickness, float y0, float y1, float length, float z) =>
            sink.Panel(id, FloorType, group, "L1", thickness,
                new Vector3(0f, y0, z), new Vector3(0f, y1, z), new Vector3(length, y1, z), new Vector3(length, y0, z));
    }
}
