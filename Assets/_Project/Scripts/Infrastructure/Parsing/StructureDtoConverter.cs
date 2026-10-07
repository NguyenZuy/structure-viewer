using System;
using System.Collections.Generic;
using StructureViewer.Application.Loading;
using StructureViewer.Domain.Structure;
using UnityEngine;

namespace StructureViewer.Infrastructure.Parsing
{
    // Validates a deserialized file (mm, Z-up) and converts it to a StructureModel in Unity space.
    // Collects every problem instead of stopping at the first, and never throws on bad data.
    public static class StructureDtoConverter
    {
        public const string ExpectedUnits = "mm";
        public const string ExpectedUpAxis = "Z";

        // Model units (mm).
        private const float MinMemberLength = 1f;
        private const float MaxPanelOutOfPlane = 1f;
        private const float MinPanelEdge = 1f;

        private static readonly Dictionary<string, ElementCategory> Categories = new Dictionary<string, ElementCategory>(StringComparer.OrdinalIgnoreCase)
        {
            { "Wall", ElementCategory.Wall },
            { "Floor", ElementCategory.Floor },
            { "Roof", ElementCategory.Roof },
            { "Sheathing", ElementCategory.Sheathing },
            { "Slab", ElementCategory.Slab },
            { "Opening", ElementCategory.Opening }
        };

        public static ParseResult Convert(StructureDto dto)
        {
            if (dto == null)
                return ParseResult.Fail(new[] { "File is empty." });

            var errors = new List<string>();
            if (dto.units != ExpectedUnits)
                errors.Add($"Unsupported units '{dto.units}', expected '{ExpectedUnits}'.");
            if (dto.upAxis != ExpectedUpAxis)
                errors.Add($"Unsupported upAxis '{dto.upAxis}', expected '{ExpectedUpAxis}'.");

            var levels = ConvertLevels(dto.levels, errors, out var levelIndexById);
            var elements = new List<Element>();
            var usedIds = new HashSet<string>();

            ForEach(dto.members, "Member", m => m.id, errors, (m, label) => ConvertMember(m, label, elements, usedIds, levelIndexById, errors));
            ForEach(dto.panels, "Panel", p => p.id, errors, (p, label) => ConvertPanel(p, label, elements, usedIds, levelIndexById, errors));
            ForEach(dto.slabs, "Slab", s => s.id, errors, (s, label) => ConvertSlab(s, label, elements, usedIds, levelIndexById, errors));

            if (errors.Count > 0)
                return ParseResult.Fail(errors);
            return ParseResult.Ok(new StructureModel(string.IsNullOrEmpty(dto.name) ? "Untitled" : dto.name, levels, elements));
        }

        private static List<Level> ConvertLevels(LevelDto[] dtos, List<string> errors, out Dictionary<string, int> indexById)
        {
            indexById = new Dictionary<string, int>();
            var levels = new List<Level>();
            if (dtos == null || dtos.Length == 0)
            {
                errors.Add("No levels defined.");
                return levels;
            }

            for (int i = 0; i < dtos.Length; i++)
            {
                var dto = dtos[i];
                if (dto == null || string.IsNullOrEmpty(dto.id))
                {
                    errors.Add($"Level #{i + 1} has no id.");
                    continue;
                }
                if (indexById.ContainsKey(dto.id))
                {
                    errors.Add($"Level '{dto.id}': duplicate id.");
                    continue;
                }
                indexById.Add(dto.id, levels.Count);
                levels.Add(new Level(levels.Count, dto.id, string.IsNullOrEmpty(dto.name) ? dto.id : dto.name, ModelAxes.ToUnityLength(dto.elevation)));
            }
            return levels;
        }

        private static void ConvertMember(MemberDto dto, string label, List<Element> elements, HashSet<string> usedIds,
            Dictionary<string, int> levels, List<string> errors)
        {
            int errorCount = errors.Count;
            ClaimId(dto.id, label, usedIds, errors);
            var category = RequireCategory(dto.category, label, errors);
            RequireType(dto.type, label, errors);
            int level = RequireLevel(dto.level, label, levels, errors);
            bool hasStart = TryPoint(dto.start, label, "start", errors, out var start);
            bool hasEnd = TryPoint(dto.end, label, "end", errors, out var end);
            if (hasStart && hasEnd && Vector3.Distance(start, end) < MinMemberLength)
                errors.Add($"{label}: zero length.");
            if (dto.section == null || dto.section.width <= 0f || dto.section.depth <= 0f)
                errors.Add($"{label}: section width and depth must be > 0.");
            if (errors.Count > errorCount)
                return;

            var section = new Section(ModelAxes.ToUnityLength(dto.section.width), ModelAxes.ToUnityLength(dto.section.depth));
            var member = new Member(ModelAxes.ToUnity(start), ModelAxes.ToUnity(end), dto.roll, section, dto.material ?? string.Empty);
            var info = new ElementInfo(dto.id, category, dto.type, dto.group ?? string.Empty, level);
            elements.Add(Element.ForMember(elements.Count, info, member));
        }

        private static void ConvertPanel(PanelDto dto, string label, List<Element> elements, HashSet<string> usedIds,
            Dictionary<string, int> levels, List<string> errors)
        {
            int errorCount = errors.Count;
            ClaimId(dto.id, label, usedIds, errors);
            var category = RequireCategory(dto.category, label, errors);
            RequireType(dto.type, label, errors);
            int level = RequireLevel(dto.level, label, levels, errors);
            if (dto.thickness <= 0f)
                errors.Add($"{label}: thickness must be > 0.");

            var corners = new Vector3[4];
            if (dto.corners == null || dto.corners.Length != 12)
            {
                errors.Add($"{label}: corners must be 4 points (12 numbers), got {dto.corners?.Length ?? 0} numbers.");
            }
            else
            {
                for (int i = 0; i < 4; i++)
                    corners[i] = new Vector3(dto.corners[i * 3], dto.corners[i * 3 + 1], dto.corners[i * 3 + 2]);
                ValidatePanelShape(corners, label, errors);
            }
            if (errors.Count > errorCount)
                return;

            for (int i = 0; i < 4; i++)
                corners[i] = ModelAxes.ToUnity(corners[i]);
            var info = new ElementInfo(dto.id, category, dto.type, dto.group ?? string.Empty, level);
            elements.Add(Element.ForPanel(elements.Count, info, new Panel(corners, ModelAxes.ToUnityLength(dto.thickness))));
        }

        private static void ConvertSlab(SlabDto dto, string label, List<Element> elements, HashSet<string> usedIds,
            Dictionary<string, int> levels, List<string> errors)
        {
            int errorCount = errors.Count;
            ClaimId(dto.id, label, usedIds, errors);
            int level = RequireLevel(dto.level, label, levels, errors);
            if (dto.thickness <= 0f)
                errors.Add($"{label}: thickness must be > 0.");

            Vector2[] outline = null;
            if (dto.outline == null || dto.outline.Length < 6 || dto.outline.Length % 2 != 0)
            {
                errors.Add($"{label}: outline needs at least 3 points as x,y pairs.");
            }
            else
            {
                outline = new Vector2[dto.outline.Length / 2];
                for (int i = 0; i < outline.Length; i++)
                    outline[i] = new Vector2(dto.outline[i * 2], dto.outline[i * 2 + 1]);
                if (!IsConvex(outline))
                    errors.Add($"{label}: outline must be a convex, non-degenerate polygon.");
            }
            if (errors.Count > errorCount)
                return;

            for (int i = 0; i < outline.Length; i++)
                outline[i] = ModelAxes.PlanToUnity(outline[i].x, outline[i].y);
            // A slab is its own assembly so it can be selected/isolated like one.
            var info = new ElementInfo(dto.id, ElementCategory.Slab, "Slab", dto.id, level);
            var slab = new Slab(outline, ModelAxes.ToUnityLength(dto.top), ModelAxes.ToUnityLength(dto.thickness));
            elements.Add(Element.ForSlab(elements.Count, info, slab));
        }

        // Corners in model space (mm). Degenerate = a collapsed edge or zero area; planar within 1 mm of the best-fit plane.
        private static void ValidatePanelShape(Vector3[] corners, string label, List<string> errors)
        {
            for (int i = 0; i < 4; i++)
            {
                if (Vector3.Distance(corners[i], corners[(i + 1) % 4]) < MinPanelEdge)
                {
                    errors.Add($"{label}: degenerate quad (corners {i + 1} and {(i + 1) % 4 + 1} coincide).");
                    return;
                }
            }

            var normal = Vector3.Cross(corners[2] - corners[0], corners[3] - corners[1]);
            if (normal.magnitude < MinPanelEdge * MinPanelEdge)
            {
                errors.Add($"{label}: degenerate quad (zero area).");
                return;
            }

            normal.Normalize();
            var centre = (corners[0] + corners[1] + corners[2] + corners[3]) * 0.25f;
            for (int i = 0; i < 4; i++)
            {
                float distance = Mathf.Abs(Vector3.Dot(corners[i] - centre, normal));
                if (distance > MaxPanelOutOfPlane)
                {
                    errors.Add($"{label}: corners are not coplanar ({distance:F1} mm off plane).");
                    return;
                }
            }
        }

        private static bool IsConvex(Vector2[] polygon)
        {
            int sign = 0;
            float twiceArea = 0f;
            for (int i = 0; i < polygon.Length; i++)
            {
                var a = polygon[i];
                var b = polygon[(i + 1) % polygon.Length];
                var c = polygon[(i + 2) % polygon.Length];
                float cross = (b.x - a.x) * (c.y - b.y) - (b.y - a.y) * (c.x - b.x);
                twiceArea += a.x * b.y - b.x * a.y;
                if (Mathf.Abs(cross) < 1e-3f)
                    continue;
                int s = cross > 0f ? 1 : -1;
                if (sign != 0 && s != sign)
                    return false;
                sign = s;
            }
            return sign != 0 && Mathf.Abs(twiceArea) > 1e-3f;
        }

        private static ElementCategory RequireCategory(string value, string label, List<string> errors)
        {
            if (value != null && Categories.TryGetValue(value, out var category))
                return category;
            errors.Add($"{label}: unknown category '{value}'.");
            return default;
        }

        private static void RequireType(string value, string label, List<string> errors)
        {
            if (string.IsNullOrEmpty(value))
                errors.Add($"{label}: missing type.");
        }

        private static int RequireLevel(string levelId, string label, Dictionary<string, int> levels, List<string> errors)
        {
            if (levelId != null && levels.TryGetValue(levelId, out int index))
                return index;
            errors.Add($"{label}: unknown level '{levelId}'.");
            return -1;
        }

        private static bool TryPoint(float[] values, string label, string field, List<string> errors, out Vector3 point)
        {
            if (values == null || values.Length != 3)
            {
                errors.Add($"{label}: {field} must be 3 numbers.");
                point = default;
                return false;
            }
            point = new Vector3(values[0], values[1], values[2]);
            return true;
        }

        private static void ClaimId(string id, string label, HashSet<string> usedIds, List<string> errors)
        {
            if (!usedIds.Add(id))
                errors.Add($"{label}: duplicate id.");
        }

        // Label is "Member 'T03-TC1'"; an element without an id is reported by position and skipped.
        private static void ForEach<T>(T[] items, string kind, Func<T, string> idOf, List<string> errors, Action<T, string> convert) where T : class
        {
            if (items == null)
                return;
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                string id = item == null ? null : idOf(item);
                if (string.IsNullOrEmpty(id))
                {
                    errors.Add($"{kind} #{i + 1} has no id.");
                    continue;
                }
                convert(item, $"{kind} '{id}'");
            }
        }
    }
}
