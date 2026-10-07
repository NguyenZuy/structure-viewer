using StructureViewer.Infrastructure.Parsing;
using UnityEngine;

namespace StructureViewer.Editor.Generator
{
    // Builds the sample house as a model-space DTO, exactly what an exporter would write.
    public static class SampleHouseGenerator
    {
        public const string SlabId = "SLAB-1";
        public const string RoofFront = "ROOF-S";
        public const string RoofBack = "ROOF-N";

        public static StructureDto Generate(SampleHouseSpec spec)
        {
            var sink = new StructureDtoBuilder();

            sink.Slab(SlabId, "L0", 0f, spec.SlabThickness,
                new Vector2(0f, 0f), new Vector2(spec.Length, 0f), new Vector2(spec.Length, spec.Depth), new Vector2(0f, spec.Depth));
            foreach (var wall in SampleHouseLayout.Walls(spec))
                WallFrameBuilder.Build(wall, spec, sink);
            FloorFrameBuilder.Build(spec, sink);
            TrussBuilder.Build(spec, sink);
            RoofSheathing(spec, sink);

            var levels = new[]
            {
                new LevelDto { id = "L0", name = "Ground Floor", elevation = 0f },
                new LevelDto { id = "L1", name = "First Floor", elevation = spec.StoreyHeight },
                new LevelDto { id = "RF", name = "Roof", elevation = 2f * spec.StoreyHeight }
            };
            return sink.Build("Sample House", levels);
        }

        // One panel per slope on top of the top chords, covering eave and gable overhangs. Corners wind so the normal points outward.
        private static void RoofSheathing(SampleHouseSpec spec, StructureDtoBuilder sink)
        {
            float slope = TrussBuilder.Slope(spec);
            float lift = spec.TrussDepth * 0.5f / Mathf.Cos(spec.RoofPitch * Mathf.Deg2Rad);
            float z0 = TrussBuilder.ChordBaseZ(spec) + lift;
            float half = spec.Depth * 0.5f;
            float eaveZ = z0 - spec.EaveOverhang * slope;
            float ridgeZ = z0 + half * slope;
            float x0 = -spec.GableOverhang;
            float x1 = spec.Length + spec.GableOverhang;
            float front = -spec.EaveOverhang;
            float back = spec.Depth + spec.EaveOverhang;

            sink.Panel("RS-S", "RoofSheathing", RoofFront, "RF", spec.SheathingThickness,
                new Vector3(x0, front, eaveZ), new Vector3(x1, front, eaveZ), new Vector3(x1, half, ridgeZ), new Vector3(x0, half, ridgeZ));
            sink.Panel("RS-N", "RoofSheathing", RoofBack, "RF", spec.SheathingThickness,
                new Vector3(x0, half, ridgeZ), new Vector3(x1, half, ridgeZ), new Vector3(x1, back, eaveZ), new Vector3(x0, back, eaveZ));
        }
    }
}
