using StructureViewer.Infrastructure.Parsing;

namespace StructureViewer.Tests.EditMode.Parsing
{
    // A valid file in model space (mm, Z-up) matching the DESIGN.md example; tests break one field at a time.
    internal static class SampleDto
    {
        public static StructureDto Create() => new StructureDto
        {
            name = "Sample House",
            units = "mm",
            upAxis = "Z",
            levels = new[]
            {
                new LevelDto { id = "L0", name = "Ground Floor", elevation = 0f },
                new LevelDto { id = "L1", name = "First Floor", elevation = 2700f },
                new LevelDto { id = "RF", name = "Roof", elevation = 5400f }
            },
            members = new[]
            {
                Member("T03-TC1", "Roof", "TrussTopChord", "T03", "RF", new[] { 0f, 3600f, 5400f }, new[] { 4200f, 3600f, 7000f }),
                Member("W-L0-N-ST01", "Wall", "Stud", "W-L0-N", "L0", new[] { 1000f, 0f, 35f }, new[] { 1000f, 0f, 2665f })
            },
            slabs = new[]
            {
                new SlabDto { id = "SLAB-1", level = "L0", outline = new[] { 0f, 0f, 10000f, 0f, 10000f, 8000f, 0f, 8000f }, top = 0f, thickness = 300f }
            },
            panels = new[]
            {
                new PanelDto
                {
                    id = "RS-N", category = "Sheathing", type = "RoofSheathing", group = "ROOF-N", level = "RF",
                    corners = new[] { 0f, 0f, 5400f, 10000f, 0f, 5400f, 10000f, 4000f, 7000f, 0f, 4000f, 7000f },
                    thickness = 12f
                }
            }
        };

        public static MemberDto Member(string id, string category, string type, string group, string level, float[] start, float[] end) => new MemberDto
        {
            id = id,
            category = category,
            type = type,
            group = group,
            level = level,
            start = start,
            end = end,
            section = new SectionDto { width = 35f, depth = 90f },
            material = "MGP10"
        };

        public const string Json = @"{
  ""name"": ""Sample House"",
  ""units"": ""mm"",
  ""upAxis"": ""Z"",
  ""levels"": [
    { ""id"": ""L0"", ""name"": ""Ground Floor"", ""elevation"": 0 },
    { ""id"": ""L1"", ""name"": ""First Floor"", ""elevation"": 2700 },
    { ""id"": ""RF"", ""name"": ""Roof"", ""elevation"": 5400 }
  ],
  ""members"": [
    {
      ""id"": ""T03-TC1"", ""category"": ""Roof"", ""type"": ""TrussTopChord"", ""group"": ""T03"", ""level"": ""RF"",
      ""start"": [0, 3600, 5400], ""end"": [4200, 3600, 7000], ""roll"": 0,
      ""section"": { ""width"": 35, ""depth"": 90 }, ""material"": ""MGP10""
    }
  ],
  ""slabs"": [
    { ""id"": ""SLAB-1"", ""level"": ""L0"", ""outline"": [0,0, 10000,0, 10000,8000, 0,8000], ""top"": 0, ""thickness"": 300 }
  ],
  ""panels"": [
    {
      ""id"": ""RS-N"", ""category"": ""Sheathing"", ""type"": ""RoofSheathing"", ""group"": ""ROOF-N"", ""level"": ""RF"",
      ""corners"": [0,0,5400, 10000,0,5400, 10000,4000,7000, 0,4000,7000], ""thickness"": 12
    }
  ]
}";
    }
}
