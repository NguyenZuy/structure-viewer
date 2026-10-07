using UnityEngine;

namespace StructureViewer.Editor.Generator
{
    // First floor: a bearer along X on the internal wall, joists spanning Y in two bays (front → bearer, bearer → back).
    public static class FloorFrameBuilder
    {
        public const string BayA = "FL-L1-A";
        public const string BayB = "FL-L1-B";
        public const string BearerGroup = "FL-L1-BR";
        private const string Level = "L1";

        public static void Build(SampleHouseSpec spec, StructureDtoBuilder sink)
        {
            float wallTop = spec.StoreyHeight - spec.JoistDepth;
            float midY = spec.Depth * 0.5f;

            float bearerZ = wallTop - spec.BearerDepth * 0.5f;
            sink.Member($"{BearerGroup}-B01", "Floor", "Bearer", BearerGroup, Level,
                new Vector3(spec.StudDepth, midY, bearerZ), new Vector3(spec.Length - spec.StudDepth, midY, bearerZ),
                0f, spec.BearerWidth, spec.BearerDepth, spec.EngineeredMaterial);

            float joistZ = wallTop + spec.JoistDepth * 0.5f;
            float first = spec.JoistWidth * 0.5f;
            float last = spec.Length - spec.JoistWidth * 0.5f;
            int n = 0;
            for (float x = first; x <= last + 0.01f; x = NextJoist(x, last, spec.JoistSpacing))
            {
                n++;
                sink.Member($"{BayA}-J{n:00}", "Floor", "Joist", BayA, Level, new Vector3(x, 0f, joistZ), new Vector3(x, midY, joistZ),
                    0f, spec.JoistWidth, spec.JoistDepth, spec.JoistMaterial);
                sink.Member($"{BayB}-J{n:00}", "Floor", "Joist", BayB, Level, new Vector3(x, midY, joistZ), new Vector3(x, spec.Depth, joistZ),
                    0f, spec.JoistWidth, spec.JoistDepth, spec.JoistMaterial);
            }
        }

        // Regular spacing, plus an end joist at the edge unless the last regular one is already close to it.
        private static float NextJoist(float x, float last, float spacing)
        {
            if (x >= last)
                return float.MaxValue;
            float next = x + spacing;
            return next > last - spacing * 0.25f ? last : next;
        }
    }
}
