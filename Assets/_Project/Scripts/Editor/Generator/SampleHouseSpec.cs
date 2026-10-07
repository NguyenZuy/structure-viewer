using System.Collections.Generic;

namespace StructureViewer.Editor.Generator
{
    public enum OpeningKind
    {
        Door,
        Window
    }

    // Offsets and heights in mm, measured from the wall's start point and its base.
    public sealed class Opening
    {
        public Opening(string wallId, OpeningKind kind, float offset, float width, float sill, float head)
        {
            WallId = wallId;
            Kind = kind;
            Offset = offset;
            Width = width;
            Sill = sill;
            Head = head;
        }

        public string WallId { get; }
        public OpeningKind Kind { get; }
        public float Offset { get; }
        public float Width { get; }
        public float Sill { get; }
        public float Head { get; }
        public float End => Offset + Width;
    }

    // Hypothetical two-storey timber house; plausible, not engineered. All lengths in mm (model space, Z-up).
    public sealed class SampleHouseSpec
    {
        public float Length { get; set; } = 10000f;
        public float Depth { get; set; } = 8000f;
        public float StoreyHeight { get; set; } = 2700f;
        public float SlabThickness { get; set; } = 300f;

        public float StudSpacing { get; set; } = 600f;
        public float StudWidth { get; set; } = 35f;
        public float StudDepth { get; set; } = 90f;
        public float LintelDepth { get; set; } = 190f;

        public float JoistSpacing { get; set; } = 450f;
        public float JoistWidth { get; set; } = 45f;
        public float JoistDepth { get; set; } = 240f;
        public float BearerWidth { get; set; } = 90f;
        public float BearerDepth { get; set; } = 290f;

        public float TrussSpacing { get; set; } = 600f;
        public float RoofPitch { get; set; } = 22.5f;
        public float TrussWidth { get; set; } = 35f;
        public float TrussDepth { get; set; } = 90f;
        public float EaveOverhang { get; set; } = 450f;
        public float GableOverhang { get; set; } = 300f;
        public float SheathingThickness { get; set; } = 12f;

        public string StudMaterial { get; set; } = "MGP10";
        public string EngineeredMaterial { get; set; } = "LVL";
        public string JoistMaterial { get; set; } = "MGP12";

        public List<Opening> Openings { get; } = new List<Opening>
        {
            new Opening("W-L0-S", OpeningKind.Window, 1400f, 1200f, 900f, 2100f),
            new Opening("W-L0-S", OpeningKind.Door, 4590f, 820f, 0f, 2100f),
            new Opening("W-L0-S", OpeningKind.Window, 7400f, 1200f, 900f, 2100f),
            new Opening("W-L0-E", OpeningKind.Window, 1500f, 900f, 900f, 2100f),
            new Opening("W-L0-W", OpeningKind.Window, 1500f, 900f, 900f, 2100f),
            new Opening("W-L1-S", OpeningKind.Window, 1400f, 1200f, 900f, 2100f),
            new Opening("W-L1-S", OpeningKind.Window, 4400f, 1200f, 900f, 2100f),
            new Opening("W-L1-S", OpeningKind.Window, 7400f, 1200f, 900f, 2100f),
            new Opening("W-L1-E", OpeningKind.Window, 1500f, 900f, 900f, 2100f),
            new Opening("W-L1-W", OpeningKind.Window, 1500f, 900f, 900f, 2100f)
        };
    }
}
