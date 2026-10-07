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

    // Hypothetical two-storey timber house in an American colonial / farmhouse style: symmetric front with a centred door,
    // a covered front porch, steep gable roof. Plausible, not engineered. All lengths in mm (model space, Z-up).
    public sealed class SampleHouseSpec
    {
        public float Length { get; set; } = 12000f;
        public float Depth { get; set; } = 8400f;
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
        public float RoofPitch { get; set; } = 35f;
        public float TrussWidth { get; set; } = 35f;
        public float TrussDepth { get; set; } = 90f;
        public float EaveOverhang { get; set; } = 500f;
        public float GableOverhang { get; set; } = 400f;
        public float SheathingThickness { get; set; } = 12f;
        public float FloorSheathingThickness { get; set; } = 19f;

        // Front porch: posts on a line PorchDepth in front of the house, a lean-to roof hung from a ledger on the front wall.
        public float PorchStart { get; set; } = 2700f;
        public float PorchEnd { get; set; } = 9300f;
        public int PorchPosts { get; set; } = 4;
        public float PorchDepth { get; set; } = 1800f;
        public float PorchOverhang { get; set; } = 300f;
        public float PorchPitch { get; set; } = 15f;
        public float PorchLedgerHeight { get; set; } = 3000f;
        public float PorchSlabThickness { get; set; } = 150f;
        public float PostSize { get; set; } = 90f;
        public float RafterWidth { get; set; } = 45f;
        public float RafterDepth { get; set; } = 140f;
        public float RafterSpacing { get; set; } = 600f;

        public string StudMaterial { get; set; } = "MGP10";
        public string EngineeredMaterial { get; set; } = "LVL";
        public string JoistMaterial { get; set; } = "MGP12";

        // Offsets are from each wall frame's start: front/back from x = 0, sides from y = StudDepth (they sit between front and back).
        public List<Opening> Openings { get; } = new List<Opening>
        {
            new Opening("W-L0-S", OpeningKind.Window, 1050f, 900f, 600f, 2100f),
            new Opening("W-L0-S", OpeningKind.Window, 3450f, 900f, 600f, 2100f),
            new Opening("W-L0-S", OpeningKind.Door, 5500f, 1000f, 0f, 2100f),
            new Opening("W-L0-S", OpeningKind.Window, 7650f, 900f, 600f, 2100f),
            new Opening("W-L0-S", OpeningKind.Window, 10050f, 900f, 600f, 2100f),
            new Opening("W-L1-S", OpeningKind.Window, 1050f, 900f, 600f, 2100f),
            new Opening("W-L1-S", OpeningKind.Window, 3450f, 900f, 600f, 2100f),
            new Opening("W-L1-S", OpeningKind.Window, 5550f, 900f, 600f, 2100f),
            new Opening("W-L1-S", OpeningKind.Window, 7650f, 900f, 600f, 2100f),
            new Opening("W-L1-S", OpeningKind.Window, 10050f, 900f, 600f, 2100f),

            new Opening("W-L0-N", OpeningKind.Window, 1800f, 1200f, 900f, 2100f),
            new Opening("W-L0-N", OpeningKind.Door, 5550f, 900f, 0f, 2100f),
            new Opening("W-L0-N", OpeningKind.Window, 9000f, 1200f, 900f, 2100f),
            new Opening("W-L1-N", OpeningKind.Window, 1950f, 900f, 600f, 2100f),
            new Opening("W-L1-N", OpeningKind.Window, 5550f, 900f, 600f, 2100f),
            new Opening("W-L1-N", OpeningKind.Window, 9150f, 900f, 600f, 2100f),

            new Opening("W-L0-E", OpeningKind.Window, 1560f, 900f, 600f, 2100f),
            new Opening("W-L0-E", OpeningKind.Window, 5760f, 900f, 600f, 2100f),
            new Opening("W-L1-E", OpeningKind.Window, 1560f, 900f, 600f, 2100f),
            new Opening("W-L1-E", OpeningKind.Window, 5760f, 900f, 600f, 2100f),
            new Opening("W-L0-W", OpeningKind.Window, 1560f, 900f, 600f, 2100f),
            new Opening("W-L0-W", OpeningKind.Window, 5760f, 900f, 600f, 2100f),
            new Opening("W-L1-W", OpeningKind.Window, 1560f, 900f, 600f, 2100f),
            new Opening("W-L1-W", OpeningKind.Window, 5760f, 900f, 600f, 2100f),

            // Upstairs hall door; the ground-floor internal wall is too low under the bearer for a door with a lintel.
            new Opening("W-L1-I", OpeningKind.Door, 3000f, 820f, 0f, 2100f)
        };
    }
}
