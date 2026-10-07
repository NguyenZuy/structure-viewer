using UnityEngine;

namespace StructureViewer.Editor.Generator
{
    // Gable roof: identical Fink trusses spanning Y, ridge along X, on the first-floor wall plates.
    public static class TrussBuilder
    {
        private const string Level = "RF";

        public static int TrussCount(SampleHouseSpec spec) => Mathf.FloorToInt(spec.Length / spec.TrussSpacing) + 1;

        // Trusses at exact spacing, centred along the house.
        public static float TrussX(SampleHouseSpec spec, int index) =>
            (spec.Length - (TrussCount(spec) - 1) * spec.TrussSpacing) * 0.5f + index * spec.TrussSpacing;

        // Bottom-chord centreline height; top chords meet it at the wall line (y = 0 / Depth).
        public static float ChordBaseZ(SampleHouseSpec spec) => 2f * spec.StoreyHeight + spec.TrussDepth * 0.5f;

        public static float Slope(SampleHouseSpec spec) => Mathf.Tan(spec.RoofPitch * Mathf.Deg2Rad);

        public static void Build(SampleHouseSpec spec, StructureDtoBuilder sink)
        {
            for (int i = 0; i < TrussCount(spec); i++)
                BuildTruss(spec, sink, $"T{i + 1:00}", TrussX(spec, i));
        }

        // Fink: bottom chord, two top chords with eave overhang, four webs in a W from the bottom-chord thirds.
        private static void BuildTruss(SampleHouseSpec spec, StructureDtoBuilder sink, string group, float x)
        {
            float z0 = ChordBaseZ(spec);
            float slope = Slope(spec);
            float span = spec.Depth;
            float half = span * 0.5f;
            Vector3 Top(float y) => new Vector3(x, y, z0 + (y <= half ? y : span - y) * slope);
            Vector3 Bottom(float y) => new Vector3(x, y, z0);

            var ridge = Top(half);
            var eaveFront = new Vector3(x, -spec.EaveOverhang, z0 - spec.EaveOverhang * slope);
            var eaveBack = new Vector3(x, span + spec.EaveOverhang, z0 - spec.EaveOverhang * slope);

            Chord(spec, sink, group, "BC", "TrussBottomChord", Bottom(0f), Bottom(span));
            Chord(spec, sink, group, "TC1", "TrussTopChord", eaveFront, ridge);
            Chord(spec, sink, group, "TC2", "TrussTopChord", ridge, eaveBack);
            Chord(spec, sink, group, "W1", "TrussWeb", Bottom(span / 3f), Top(span / 4f));
            Chord(spec, sink, group, "W2", "TrussWeb", Bottom(span / 3f), ridge);
            Chord(spec, sink, group, "W3", "TrussWeb", Bottom(span * 2f / 3f), ridge);
            Chord(spec, sink, group, "W4", "TrussWeb", Bottom(span * 2f / 3f), Top(span * 3f / 4f));
        }

        private static void Chord(SampleHouseSpec spec, StructureDtoBuilder sink, string group, string code, string type, Vector3 start, Vector3 end) =>
            sink.Member($"{group}-{code}", "Roof", type, group, Level, start, end, 0f, spec.TrussWidth, spec.TrussDepth, spec.StudMaterial);
    }
}
