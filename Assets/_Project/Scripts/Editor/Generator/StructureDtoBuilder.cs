using System.Collections.Generic;
using StructureViewer.Infrastructure.Parsing;
using UnityEngine;

namespace StructureViewer.Editor.Generator
{
    // Collects elements in model space (mm, Z-up) and emits the flat-array DTO the parser reads.
    public sealed class StructureDtoBuilder
    {
        private readonly List<MemberDto> _members = new List<MemberDto>();
        private readonly List<PanelDto> _panels = new List<PanelDto>();
        private readonly List<SlabDto> _slabs = new List<SlabDto>();

        public int MemberCount => _members.Count;

        public void Member(string id, string category, string type, string group, string level,
            Vector3 start, Vector3 end, float roll, float width, float depth, string material)
        {
            _members.Add(new MemberDto
            {
                id = id,
                category = category,
                type = type,
                group = group,
                level = level,
                start = Point(start),
                end = Point(end),
                roll = roll,
                section = new SectionDto { width = width, depth = depth },
                material = material
            });
        }

        public void Panel(string id, string type, string group, string level, float thickness, params Vector3[] corners) =>
            Panel(id, "Sheathing", type, group, level, thickness, corners);

        public void Panel(string id, string category, string type, string group, string level, float thickness, Vector3[] corners)
        {
            var flat = new float[corners.Length * 3];
            for (int i = 0; i < corners.Length; i++)
                Point(corners[i]).CopyTo(flat, i * 3);
            _panels.Add(new PanelDto { id = id, category = category, type = type, group = group, level = level, corners = flat, thickness = thickness });
        }

        public void Slab(string id, string level, float top, float thickness, params Vector2[] outline)
        {
            var flat = new float[outline.Length * 2];
            for (int i = 0; i < outline.Length; i++)
            {
                flat[i * 2] = Round(outline[i].x);
                flat[i * 2 + 1] = Round(outline[i].y);
            }
            _slabs.Add(new SlabDto { id = id, level = level, outline = flat, top = top, thickness = thickness });
        }

        public StructureDto Build(string name, LevelDto[] levels) => new StructureDto
        {
            name = name,
            units = "mm",
            upAxis = "Z",
            levels = levels,
            members = _members.ToArray(),
            slabs = _slabs.ToArray(),
            panels = _panels.ToArray()
        };

        // 0.1 mm keeps the JSON short without visible error.
        private static float Round(float mm) => Mathf.Round(mm * 10f) / 10f;

        private static float[] Point(Vector3 p) => new[] { Round(p.x), Round(p.y), Round(p.z) };
    }
}
