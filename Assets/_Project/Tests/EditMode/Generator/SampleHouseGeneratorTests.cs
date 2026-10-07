using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StructureViewer.Editor.Generator;
using StructureViewer.Infrastructure.Parsing;
using UnityEngine;

namespace StructureViewer.Tests.EditMode.Generator
{
    public sealed class SampleHouseGeneratorTests
    {
        private SampleHouseSpec _spec;
        private StructureDto _dto;
        private List<WallSpec> _walls;

        [SetUp]
        public void SetUp()
        {
            _spec = new SampleHouseSpec();
            _dto = SampleHouseGenerator.Generate(_spec);
            _walls = SampleHouseLayout.Walls(_spec);
        }

        [Test]
        public void Generate_AllIds_AreUnique()
        {
            var ids = _dto.members.Select(m => m.id).Concat(_dto.panels.Select(p => p.id)).Concat(_dto.slabs.Select(s => s.id)).ToList();

            var duplicates = ids.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            CollectionAssert.IsEmpty(duplicates);
        }

        [Test]
        public void Generate_MemberCount_IsWithinTarget()
        {
            Assert.That(_dto.members.Length, Is.InRange(500, 800));
        }

        [Test]
        public void Generate_EveryMember_HasLengthAndKnownLevel()
        {
            var levels = new HashSet<string>(_dto.levels.Select(l => l.id));
            foreach (var m in _dto.members)
            {
                Assert.Greater(Vector3.Distance(Point(m.start), Point(m.end)), 1f, m.id);
                Assert.IsTrue(levels.Contains(m.level), $"{m.id} level {m.level}");
                Assert.Greater(m.section.width, 0f, m.id);
                Assert.Greater(m.section.depth, 0f, m.id);
            }
        }

        [Test]
        public void Generate_NoCommonStud_OverlapsAnOpening()
        {
            float half = _spec.StudWidth * 0.5f;
            foreach (var wall in _walls)
            {
                foreach (var stud in MembersOf(wall, "Stud"))
                {
                    float u = AlongWall(wall, Point(stud.start));
                    foreach (var opening in wall.Openings)
                        Assert.IsTrue(u + half <= opening.Offset || u - half >= opening.End, $"{stud.id} at u={u} crosses opening at {opening.Offset}..{opening.End}");
                }
            }
        }

        [Test]
        public void Generate_NoNogging_CrossesAnOpening()
        {
            foreach (var wall in _walls)
            {
                foreach (var nog in MembersOf(wall, "Nogging"))
                {
                    var mid = (Point(nog.start) + Point(nog.end)) * 0.5f;
                    float u = AlongWall(wall, mid);
                    float v = mid.z - wall.Base;
                    foreach (var opening in wall.Openings)
                        Assert.IsFalse(u > opening.Offset && u < opening.End && v > opening.Sill && v < opening.Head, $"{nog.id} crosses an opening");
                }
            }
        }

        [Test]
        public void Generate_EveryOpening_HasTwoTrimmersALintelAndASillForWindows()
        {
            float half = _spec.StudWidth * 0.5f;
            foreach (var wall in _walls)
            {
                var trimmers = MembersOf(wall, "TrimmerStud").Select(m => AlongWall(wall, Point(m.start))).ToList();
                var lintels = MembersOf(wall, "Lintel").ToList();
                var sills = MembersOf(wall, "Sill").ToList();

                Assert.AreEqual(2 * wall.Openings.Count, trimmers.Count, wall.Id);
                Assert.AreEqual(wall.Openings.Count, lintels.Count, wall.Id);
                Assert.AreEqual(wall.Openings.Count(o => o.Kind == OpeningKind.Window), sills.Count, wall.Id);
                foreach (var opening in wall.Openings)
                {
                    Assert.IsTrue(trimmers.Any(u => Mathf.Abs(u - (opening.Offset - half)) < 0.5f), $"{wall.Id}: left trimmer at {opening.Offset}");
                    Assert.IsTrue(trimmers.Any(u => Mathf.Abs(u - (opening.End + half)) < 0.5f), $"{wall.Id}: right trimmer at {opening.End}");
                    Assert.IsTrue(lintels.Any(l => Spans(wall, l, opening)), $"{wall.Id}: lintel over {opening.Offset}");
                }
            }
        }

        [Test]
        public void Generate_Openings_KeepClearOfCorners()
        {
            foreach (var wall in _walls)
            {
                foreach (var opening in wall.Openings)
                {
                    Assert.GreaterOrEqual(opening.Offset, 300f, wall.Id);
                    Assert.LessOrEqual(opening.End, wall.Length - 300f, wall.Id);
                }
            }
        }

        [Test]
        public void Generate_VerticalMembers_KeepDepthAcrossTheWall()
        {
            // Roll 90 on walls along Y turns the 90 mm depth across the wall (viewer convention, see WallFrameBuilder).
            foreach (var wall in _walls)
            {
                float expected = wall.RunsAlongY ? 90f : 0f;
                foreach (var stud in MembersOf(wall, "Stud"))
                    Assert.AreEqual(expected, stud.roll, stud.id);
            }
        }

        [Test]
        public void Generate_TrussCount_MatchesSpacing()
        {
            int expected = Mathf.FloorToInt(_spec.Length / _spec.TrussSpacing) + 1;

            var trusses = _dto.members.Where(m => m.category == "Roof").Select(m => m.group).Distinct().ToList();

            Assert.AreEqual(expected, trusses.Count);
            Assert.AreEqual(17, expected);
        }

        [Test]
        public void Generate_EveryTruss_HasFinkMembers()
        {
            foreach (var truss in _dto.members.Where(m => m.category == "Roof").GroupBy(m => m.group))
            {
                Assert.AreEqual(1, truss.Count(m => m.type == "TrussBottomChord"), truss.Key);
                Assert.AreEqual(2, truss.Count(m => m.type == "TrussTopChord"), truss.Key);
                Assert.AreEqual(4, truss.Count(m => m.type == "TrussWeb"), truss.Key);
            }
        }

        [Test]
        public void Generate_TopChords_HaveRoofPitch()
        {
            foreach (var chord in _dto.members.Where(m => m.type == "TrussTopChord"))
            {
                var d = Point(chord.end) - Point(chord.start);
                float pitch = Mathf.Atan2(Mathf.Abs(d.z), new Vector2(d.x, d.y).magnitude) * Mathf.Rad2Deg;
                Assert.AreEqual(_spec.RoofPitch, pitch, 0.5f, chord.id);
            }
        }

        [Test]
        public void Generate_AllPoints_StayWithinFootprintAndOverhangs()
        {
            float topZ = 2f * _spec.StoreyHeight + _spec.Depth * 0.5f * Mathf.Tan(_spec.RoofPitch * Mathf.Deg2Rad) + 200f;
            var points = _dto.members.SelectMany(m => new[] { Point(m.start), Point(m.end) })
                .Concat(_dto.panels.SelectMany(p => Corners(p.corners)));
            foreach (var p in points)
            {
                Assert.That(p.x, Is.InRange(-_spec.GableOverhang - 1f, _spec.Length + _spec.GableOverhang + 1f));
                Assert.That(p.y, Is.InRange(-_spec.EaveOverhang - 1f, _spec.Depth + _spec.EaveOverhang + 1f));
                Assert.That(p.z, Is.InRange(0f, topZ));
            }
        }

        [Test]
        public void Generate_RoofPanels_AreCoplanarAndFaceOutward()
        {
            Assert.AreEqual(2, _dto.panels.Length);
            foreach (var panel in _dto.panels)
            {
                var c = Corners(panel.corners).ToArray();
                var normal = Vector3.Cross(c[1] - c[0], c[3] - c[0]).normalized;
                Assert.Less(Mathf.Abs(Vector3.Dot(c[2] - c[0], normal)), 1f, panel.id);
                Assert.Greater(normal.z, 0f, $"{panel.id} faces down");
                var centre = (c[0] + c[1] + c[2] + c[3]) * 0.25f;
                float outward = centre.y < _spec.Depth * 0.5f ? -1f : 1f;
                Assert.Greater(normal.y * outward, 0f, $"{panel.id} faces inward");
            }
        }

        [Test]
        public void Generate_Groups_FollowNamingScheme()
        {
            var groups = new HashSet<string>(_dto.members.Select(m => m.group));

            Assert.IsTrue(groups.Contains("W-L0-S"));
            Assert.IsTrue(groups.Contains("W-L1-I"));
            Assert.IsTrue(groups.Contains("FL-L1-A"));
            Assert.IsTrue(groups.Contains("FL-L1-B"));
            Assert.IsTrue(groups.Contains("FL-L1-BR"));
            Assert.IsTrue(groups.Contains("T01"));
            Assert.IsTrue(_dto.members.Any(m => m.id == "T03-TC1"));
            Assert.IsTrue(_dto.members.Any(m => m.id == "W-L0-S-LT01"));
            CollectionAssert.AreEquivalent(new[] { "RS-S", "RS-N" }, _dto.panels.Select(p => p.id));
            Assert.AreEqual("SLAB-1", _dto.slabs.Single().id);
        }

        private IEnumerable<MemberDto> MembersOf(WallSpec wall, string type) =>
            _dto.members.Where(m => m.group == wall.Id && m.type == type);

        private static float AlongWall(WallSpec wall, Vector3 p) => Vector2.Dot(new Vector2(p.x, p.y) - wall.Start, wall.Direction);

        private static bool Spans(WallSpec wall, MemberDto member, Opening opening)
        {
            float a = AlongWall(wall, Point(member.start));
            float b = AlongWall(wall, Point(member.end));
            return Mathf.Abs(Mathf.Min(a, b) - opening.Offset) < 0.5f && Mathf.Abs(Mathf.Max(a, b) - opening.End) < 0.5f;
        }

        private static Vector3 Point(float[] p) => new Vector3(p[0], p[1], p[2]);

        private static IEnumerable<Vector3> Corners(float[] flat)
        {
            for (int i = 0; i + 2 < flat.Length; i += 3)
                yield return new Vector3(flat[i], flat[i + 1], flat[i + 2]);
        }
    }
}
