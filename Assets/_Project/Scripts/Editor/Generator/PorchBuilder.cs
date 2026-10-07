using UnityEngine;

namespace StructureViewer.Editor.Generator
{
    // Covered front porch: a concrete pad, posts carrying a beam, and a lean-to roof whose rafters hang from a ledger
    // on the front wall (above the ground-floor window heads, below the first-floor sills).
    public static class PorchBuilder
    {
        public const string FrameGroup = "PORCH";
        public const string RoofGroup = "PORCH-R";
        public const string SlabId = "SLAB-PORCH";
        private const string Level = "L0";

        public static void Build(SampleHouseSpec spec, StructureDtoBuilder sink)
        {
            float slope = Mathf.Tan(spec.PorchPitch * Mathf.Deg2Rad);
            float wallFace = -spec.SheathingThickness;
            float postY = -spec.PorchDepth;
            float outerY = postY - spec.PorchOverhang;
            // Rafter centreline height at a distance d in front of the wall face.
            float RafterZ(float d) => spec.PorchLedgerHeight - d * slope;

            float x0 = spec.PorchStart - spec.PostSize * 2f;
            float x1 = spec.PorchEnd + spec.PostSize * 2f;
            sink.Slab(SlabId, Level, 0f, spec.PorchSlabThickness,
                new Vector2(x0, outerY), new Vector2(x1, outerY), new Vector2(x1, 0f), new Vector2(x0, 0f));

            // Beam on edge right under the rafters at the post line; posts stand on the pad up to the beam.
            float rafterHalf = spec.RafterDepth * 0.5f / Mathf.Cos(spec.PorchPitch * Mathf.Deg2Rad);
            float beamTop = RafterZ(spec.PorchDepth + wallFace) - rafterHalf;
            float beamZ = beamTop - spec.LintelDepth * 0.5f;
            float postTop = beamTop - spec.LintelDepth;
            sink.Member($"{FrameGroup}-BM01", "Wall", "PorchBeam", FrameGroup, Level,
                new Vector3(x0, postY, beamZ), new Vector3(x1, postY, beamZ), 0f, spec.StudDepth, spec.LintelDepth, spec.EngineeredMaterial);

            float spacing = (spec.PorchEnd - spec.PorchStart) / (spec.PorchPosts - 1);
            for (int i = 0; i < spec.PorchPosts; i++)
            {
                float x = spec.PorchStart + i * spacing;
                sink.Member($"{FrameGroup}-PO{i + 1:00}", "Wall", "Post", FrameGroup, Level,
                    new Vector3(x, postY, 0f), new Vector3(x, postY, postTop), 0f, spec.PostSize, spec.PostSize, spec.EngineeredMaterial);
            }

            // Ledger flat against the sheathing, then rafters from it to past the beam.
            float ledgerY = wallFace - spec.RafterWidth * 0.5f;
            sink.Member($"{RoofGroup}-LG01", "Roof", "Ledger", RoofGroup, Level,
                new Vector3(x0, ledgerY, RafterZ(0f)), new Vector3(x1, ledgerY, RafterZ(0f)), 0f, spec.RafterWidth, spec.RafterDepth, spec.StudMaterial);

            int n = 0;
            for (float x = x0 + spec.RafterWidth * 0.5f; x <= x1 - spec.RafterWidth * 0.5f + 0.01f; x = Next(x, x1 - spec.RafterWidth * 0.5f, spec.RafterSpacing))
            {
                n++;
                sink.Member($"{RoofGroup}-RF{n:00}", "Roof", "Rafter", RoofGroup, Level,
                    new Vector3(x, ledgerY - spec.RafterWidth * 0.5f, RafterZ(spec.RafterWidth)),
                    new Vector3(x, outerY, RafterZ(-outerY + wallFace)), 0f, spec.RafterWidth, spec.RafterDepth, spec.StudMaterial);
            }

            // Sheathing on top of the rafters, from the wall to the eave; corner order makes the normal face up and out.
            float lift = rafterHalf;
            float zLow = RafterZ(-outerY + wallFace) + lift;
            float zHigh = RafterZ(0f) + lift;
            sink.Panel($"{RoofGroup}-RS01", "RoofSheathing", RoofGroup, Level, spec.SheathingThickness,
                new Vector3(x0, outerY, zLow), new Vector3(x1, outerY, zLow), new Vector3(x1, wallFace, zHigh), new Vector3(x0, wallFace, zHigh));
        }

        // Regular spacing, ending with a rafter at the far edge.
        private static float Next(float x, float last, float spacing)
        {
            if (x >= last)
                return float.MaxValue;
            float next = x + spacing;
            return next > last - spacing * 0.25f ? last : next;
        }
    }
}
