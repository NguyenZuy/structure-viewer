using System.Collections.Generic;
using UnityEngine;

namespace StructureViewer.Editor.Generator
{
    // A straight wall: centreline from Start to End (plan, mm), standing on Base for Height.
    public sealed class WallSpec
    {
        public WallSpec(string id, string levelId, Vector2 start, Vector2 end, float baseZ, float height, IReadOnlyList<Opening> openings)
        {
            Id = id;
            LevelId = levelId;
            Start = start;
            End = end;
            Base = baseZ;
            Height = height;
            Openings = openings;
        }

        public string Id { get; }
        public string LevelId { get; }
        public Vector2 Start { get; }
        public Vector2 End { get; }
        public float Base { get; }
        public float Height { get; }
        public IReadOnlyList<Opening> Openings { get; }
        public float Length => Vector2.Distance(Start, End);
        public Vector2 Direction => (End - Start).normalized;
        public bool RunsAlongY => Mathf.Abs(Direction.y) > Mathf.Abs(Direction.x);
    }

    // Where the walls go. External walls sit inside the footprint; front (S) and back (N) run full length,
    // side walls (E/W) and the internal wall (I, y = Depth / 2) fit between them.
    public static class SampleHouseLayout
    {
        public static readonly string[] LevelIds = { "L0", "L1", "RF" };

        public static List<WallSpec> Walls(SampleHouseSpec spec)
        {
            var walls = new List<WallSpec>();
            float half = spec.StudDepth * 0.5f;
            float inner = spec.StudDepth;
            float midY = spec.Depth * 0.5f;

            for (int level = 0; level < 2; level++)
            {
                string levelId = LevelIds[level];
                float baseZ = level * spec.StoreyHeight;
                // Ground-floor walls stop under the first-floor joists; the internal one also under the bearer.
                float height = level == 0 ? spec.StoreyHeight - spec.JoistDepth : spec.StoreyHeight;
                float internalHeight = level == 0 ? height - spec.BearerDepth : height;

                walls.Add(Wall(spec, $"W-{levelId}-S", levelId, new Vector2(0f, half), new Vector2(spec.Length, half), baseZ, height));
                walls.Add(Wall(spec, $"W-{levelId}-N", levelId, new Vector2(0f, spec.Depth - half), new Vector2(spec.Length, spec.Depth - half), baseZ, height));
                walls.Add(Wall(spec, $"W-{levelId}-E", levelId, new Vector2(spec.Length - half, inner), new Vector2(spec.Length - half, spec.Depth - inner), baseZ, height));
                walls.Add(Wall(spec, $"W-{levelId}-W", levelId, new Vector2(half, inner), new Vector2(half, spec.Depth - inner), baseZ, height));
                walls.Add(Wall(spec, $"W-{levelId}-I", levelId, new Vector2(inner, midY), new Vector2(spec.Length - inner, midY), baseZ, internalHeight));
            }
            return walls;
        }

        private static WallSpec Wall(SampleHouseSpec spec, string id, string levelId, Vector2 start, Vector2 end, float baseZ, float height)
        {
            var openings = spec.Openings.FindAll(o => o.WallId == id);
            openings.Sort((a, b) => a.Offset.CompareTo(b.Offset));
            return new WallSpec(id, levelId, start, end, baseZ, height, openings);
        }
    }
}
