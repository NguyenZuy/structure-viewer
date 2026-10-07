using System.Collections.Generic;
using UnityEngine;

namespace StructureViewer.Editor.Generator
{
    // Frames one wall in wall-local coordinates (u along the wall from its start, v up from its base), then maps to model space.
    // Section orientation follows the viewer's convention (width = local X, depth = local Y, LookRotation(dir, refUp) then roll):
    // vertical members need roll 90 on walls running along Y to keep their 90 mm depth across the wall;
    // horizontal plates/noggings/sills need roll 90 to lie flat; lintels stand on edge with roll 0.
    public static class WallFrameBuilder
    {
        private const float FlatRoll = 90f;
        private const float MinPieceLength = 50f;

        public static void Build(WallSpec wall, SampleHouseSpec spec, StructureDtoBuilder sink)
        {
            var frame = new Frame(wall, spec, sink);
            frame.Plates();
            var verticals = frame.StudsAndTrimmers();
            frame.OpeningMembers();
            frame.Noggings(verticals);
        }

        private sealed class Frame
        {
            private readonly WallSpec _wall;
            private readonly SampleHouseSpec _spec;
            private readonly StructureDtoBuilder _sink;
            private readonly Dictionary<string, int> _counters = new Dictionary<string, int>();
            private readonly float _half;
            private readonly float _studBottom;
            private readonly float _studTop;
            private readonly float _verticalRoll;

            public Frame(WallSpec wall, SampleHouseSpec spec, StructureDtoBuilder sink)
            {
                _wall = wall;
                _spec = spec;
                _sink = sink;
                _half = spec.StudWidth * 0.5f;
                _studBottom = spec.StudWidth;
                _studTop = wall.Height - 2f * spec.StudWidth;
                _verticalRoll = wall.RunsAlongY ? 90f : 0f;
            }

            public void Plates()
            {
                // The bottom plate is cut at doors.
                float cursor = 0f;
                foreach (var opening in _wall.Openings)
                {
                    if (opening.Kind != OpeningKind.Door)
                        continue;
                    Horizontal("BP", "BottomPlate", cursor, opening.Offset, _half);
                    cursor = opening.End;
                }
                Horizontal("BP", "BottomPlate", cursor, _wall.Length, _half);

                Horizontal("TP", "TopPlate", 0f, _wall.Length, _wall.Height - _half);
                Horizontal("TP", "TopPlate", 0f, _wall.Length, _wall.Height - _spec.StudWidth - _half);
            }

            // Returns the u position of every full-height vertical, sorted, for noggings.
            public List<float> StudsAndTrimmers()
            {
                var verticals = new List<float>();
                foreach (var opening in _wall.Openings)
                {
                    foreach (float u in new[] { opening.Offset - _half, opening.End + _half })
                    {
                        Vertical("TS", "TrimmerStud", u, _studBottom, _studTop);
                        verticals.Add(u);
                    }
                }

                var candidates = new List<float> { _half };
                for (float u = _spec.StudSpacing; u < _wall.Length - _half; u += _spec.StudSpacing)
                    candidates.Add(u);
                candidates.Add(_wall.Length - _half);

                var studs = new List<float>();
                foreach (float u in candidates)
                {
                    if (BlockedByOpening(u) || TooClose(u, verticals) || TooClose(u, studs))
                        continue;
                    studs.Add(u);
                }
                foreach (float u in studs)
                    Vertical("ST", "Stud", u, _studBottom, _studTop);

                verticals.AddRange(studs);
                verticals.Sort();
                return verticals;
            }

            public void OpeningMembers()
            {
                foreach (var opening in _wall.Openings)
                {
                    float lintelTop = opening.Head + _spec.LintelDepth;
                    Member("LT", "Lintel", opening.Offset, opening.Head + _spec.LintelDepth * 0.5f, opening.End, opening.Head + _spec.LintelDepth * 0.5f,
                        0f, _spec.StudDepth, _spec.LintelDepth, _spec.EngineeredMaterial);
                    if (opening.Kind == OpeningKind.Window)
                        Horizontal("SL", "Sill", opening.Offset, opening.End, opening.Sill - _half);

                    foreach (float u in CripplePositions(opening))
                    {
                        if (_studTop - lintelTop >= MinPieceLength)
                            Vertical("CS", "CrippleStud", u, lintelTop, _studTop);
                        if (opening.Kind == OpeningKind.Window && opening.Sill - _spec.StudWidth - _studBottom >= MinPieceLength)
                            Vertical("CS", "CrippleStud", u, _studBottom, opening.Sill - _spec.StudWidth);
                    }
                }
            }

            public void Noggings(List<float> verticals)
            {
                float v = (_studBottom + _studTop) * 0.5f;
                for (int i = 0; i + 1 < verticals.Count; i++)
                {
                    float a = verticals[i] + _half;
                    float b = verticals[i + 1] - _half;
                    if (b - a < MinPieceLength || CrossesOpening((a + b) * 0.5f, v))
                        continue;
                    Horizontal("NG", "Nogging", a, b, v);
                }
            }

            // Cripples follow the stud grid inside the opening; a narrow opening gets one in the middle.
            private List<float> CripplePositions(Opening opening)
            {
                var positions = new List<float>();
                for (float u = _spec.StudSpacing; u < _wall.Length; u += _spec.StudSpacing)
                {
                    if (u - _half > opening.Offset + MinPieceLength && u + _half < opening.End - MinPieceLength)
                        positions.Add(u);
                }
                if (positions.Count == 0)
                    positions.Add((opening.Offset + opening.End) * 0.5f);
                return positions;
            }

            // A common stud may not overlap an opening or its trimmers.
            private bool BlockedByOpening(float u)
            {
                foreach (var opening in _wall.Openings)
                {
                    if (u + _half > opening.Offset - _spec.StudWidth && u - _half < opening.End + _spec.StudWidth)
                        return true;
                }
                return false;
            }

            private bool TooClose(float u, List<float> others)
            {
                foreach (float other in others)
                {
                    if (Mathf.Abs(u - other) - _spec.StudWidth < MinPieceLength)
                        return true;
                }
                return false;
            }

            private bool CrossesOpening(float u, float v)
            {
                foreach (var opening in _wall.Openings)
                {
                    if (u > opening.Offset && u < opening.End && v > opening.Sill - _spec.StudWidth && v < opening.Head + _spec.LintelDepth)
                        return true;
                }
                return false;
            }

            private void Vertical(string code, string type, float u, float v0, float v1) =>
                Member(code, type, u, v0, u, v1, _verticalRoll, _spec.StudWidth, _spec.StudDepth, _spec.StudMaterial);

            private void Horizontal(string code, string type, float u0, float u1, float v)
            {
                if (u1 - u0 < MinPieceLength)
                    return;
                Member(code, type, u0, v, u1, v, FlatRoll, _spec.StudWidth, _spec.StudDepth, _spec.StudMaterial);
            }

            private void Member(string code, string type, float u0, float v0, float u1, float v1, float roll, float width, float depth, string material)
            {
                _counters.TryGetValue(code, out int n);
                _counters[code] = ++n;
                _sink.Member($"{_wall.Id}-{code}{n:00}", "Wall", type, _wall.Id, _wall.LevelId,
                    ToModel(u0, v0), ToModel(u1, v1), roll, width, depth, material);
            }

            private Vector3 ToModel(float u, float v)
            {
                var p = _wall.Start + _wall.Direction * u;
                return new Vector3(p.x, p.y, _wall.Base + v);
            }
        }
    }
}
