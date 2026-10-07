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

            var trusses = _dto.members.Where(m => m.category == "Roof" && m.group != PorchBuilder.RoofGroup).Select(m => m.group).Distinct().ToList();

            Assert.AreEqual(expected, trusses.Count);
            Assert.AreEqual(21, expected);
        }

        [Test]
        public void Generate_EveryTruss_HasFinkMembers()
        {
            foreach (var truss in _dto.members.Where(m => m.category == "Roof" && m.group != PorchBuilder.RoofGroup).GroupBy(m => m.group))
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
                float front = Mathf.Max(_spec.EaveOverhang, _spec.PorchDepth + _spec.PorchOverhang);
                Assert.That(p.y, Is.InRange(-front - 1f, _spec.Depth + _spec.EaveOverhang + 1f));
                Assert.That(p.z, Is.InRange(0f, topZ));
            }
        }

        [Test]
        public void Generate_RoofPanels_AreCoplanarAndFaceOutward()
        {
            var roof = _dto.panels.Where(p => p.type == "RoofSheathing").ToArray();
            Assert.AreEqual(3, roof.Length, "two main slopes and the porch roof");
            foreach (var panel in roof)
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
            CollectionAssert.IsSupersetOf(_dto.panels.Select(p => p.id), new[] { "RS-S", "RS-N", "PORCH-R-RS01", "W-L1-E-GB01", "W-L1-W-GB01" });
            Assert.IsTrue(groups.Contains(PorchBuilder.FrameGroup));
            CollectionAssert.AreEquivalent(new[] { "SLAB-1", PorchBuilder.SlabId }, _dto.slabs.Select(s => s.id));
        }

        [Test]
        public void Generate_WallSheathing_OnEveryExternalWall_InTheWallsGroup_FacingOutward()
        {
            var centre = new Vector2(_spec.Length * 0.5f, _spec.Depth * 0.5f);
            foreach (var wall in _walls.Where(w => !w.Id.EndsWith("-I")))
            {
                var panels = _dto.panels.Where(p => p.group == wall.Id && IsSheathing(p) && !IsGable(p)).ToArray();
                Assert.IsNotEmpty(panels, wall.Id);
                foreach (var panel in panels)
                {
                    Assert.AreEqual(EnvelopeBuilder.WallType, panel.type, panel.id);
                    Assert.AreEqual(wall.LevelId, panel.level, panel.id);
                    var c = Corners(panel.corners).ToArray();
                    var normal = Vector3.Cross(c[1] - c[0], c[3] - c[0]).normalized;
                    var mid = (c[0] + c[2]) * 0.5f;
                    Assert.Greater(Vector2.Dot(new Vector2(normal.x, normal.y), new Vector2(mid.x, mid.y) - centre), 0f, $"{panel.id} faces inward");
                    Assert.AreEqual(0f, normal.z, 1e-4f, $"{panel.id} is not vertical");
                }
            }
            Assert.IsFalse(_dto.panels.Any(p => IsSheathing(p) && p.group != null && p.group.EndsWith("-I")), "internal walls stay unsheathed");
        }

        [Test]
        public void Generate_WallSheathing_CoversEachStoreyExceptOpenings()
        {
            foreach (var wall in _walls.Where(w => !w.Id.EndsWith("-I")))
            {
                float area = _dto.panels.Where(p => p.group == wall.Id && IsSheathing(p) && !IsGable(p)).Sum(p => QuadArea(Corners(p.corners).ToArray()));
                bool side = wall.RunsAlongY;
                float faceLength = side ? _spec.Depth + 2f * _spec.SheathingThickness : _spec.Length;
                float openings = wall.Openings.Sum(o => o.Width * (o.Head - o.Sill));

                Assert.AreEqual(faceLength * _spec.StoreyHeight - openings, area, 1f * faceLength, wall.Id);
            }
        }

        [Test]
        public void Generate_WallSheathing_NeverCoversAnOpening()
        {
            foreach (var wall in _walls)
            {
                foreach (var opening in wall.Openings)
                {
                    var a = wall.Start + wall.Direction * opening.Offset;
                    var b = wall.Start + wall.Direction * opening.End;
                    var openingMid = new Vector3((a.x + b.x) * 0.5f, (a.y + b.y) * 0.5f, wall.Base + (opening.Sill + opening.Head) * 0.5f);
                    foreach (var panel in _dto.panels.Where(p => p.group == wall.Id && IsSheathing(p)))
                    {
                        var c = Corners(panel.corners).ToArray();
                        var min = Vector3.Min(Vector3.Min(c[0], c[1]), Vector3.Min(c[2], c[3]));
                        var max = Vector3.Max(Vector3.Max(c[0], c[1]), Vector3.Max(c[2], c[3]));
                        // Compare along the wall and in height only (the panel sits on the outer face, not on the centreline).
                        bool alongInside = wall.RunsAlongY
                            ? openingMid.y > min.y && openingMid.y < max.y
                            : openingMid.x > min.x && openingMid.x < max.x;
                        bool heightInside = openingMid.z > min.z && openingMid.z < max.z;
                        Assert.IsFalse(alongInside && heightInside, $"{panel.id} covers an opening of {wall.Id}");
                    }
                }
            }
        }

        [Test]
        public void Generate_FloorDeck_CoversTheFirstFloorAndFacesDown()
        {
            var decks = _dto.panels.Where(p => p.type == EnvelopeBuilder.FloorType).ToArray();

            Assert.AreEqual(2, decks.Length);
            Assert.AreEqual(_spec.Length * _spec.Depth, decks.Sum(p => QuadArea(Corners(p.corners).ToArray())), 1f);
            foreach (var deck in decks)
            {
                var c = Corners(deck.corners).ToArray();
                Assert.Less(Vector3.Cross(c[1] - c[0], c[3] - c[0]).z, 0f, deck.id);
                Assert.IsTrue(c.All(p => Mathf.Approximately(p.z, _spec.StoreyHeight)), deck.id);
            }
        }

        [Test]
        public void Generate_Gables_CloseTheSideWallsUpToTheRidge_FacingOutward()
        {
            var gables = _dto.panels.Where(IsGable).ToArray();
            float ridgeUnderside = TrussBuilder.ChordBaseZ(_spec) + _spec.Depth * 0.5f * TrussBuilder.Slope(_spec);

            Assert.AreEqual(2, gables.Length);
            foreach (var gable in gables)
            {
                var c = Corners(gable.corners).ToArray();
                var normal = Vector3.Cross(c[1] - c[0], c[3] - c[0]);
                bool east = c[0].x > _spec.Length * 0.5f;
                Assert.Greater(east ? normal.x : -normal.x, 0f, $"{gable.id} faces inward");
                Assert.AreEqual(2f * _spec.StoreyHeight, Mathf.Min(c[0].z, c[1].z), 0.5f, gable.id);
                Assert.That(c[2].z, Is.GreaterThan(ridgeUnderside), gable.id);
            }
        }

        [Test]
        public void Generate_Porch_PostsStandOnThePadAndCarryTheBeam_WithHeadroom()
        {
            var posts = _dto.members.Where(m => m.type == "Post").ToArray();
            var beam = _dto.members.Single(m => m.type == "PorchBeam");
            float beamBottom = Point(beam.start).z - beam.section.depth * 0.5f;

            Assert.AreEqual(_spec.PorchPosts, posts.Length);
            foreach (var post in posts)
            {
                Assert.AreEqual(0f, Point(post.start).z, 0.5f, post.id);
                Assert.AreEqual(beamBottom, Point(post.end).z, 0.5f, post.id);
            }
            Assert.Greater(beamBottom, 2100f, "walkable under the beam");
            float rafterTopAtWall = _spec.PorchLedgerHeight + _spec.RafterDepth;
            Assert.Less(rafterTopAtWall, _spec.StoreyHeight + _spec.Openings.Where(o => o.WallId == "W-L1-S").Min(o => o.Sill),
                "porch roof stays under the first-floor sills");
        }

        [Test]
        public void Generate_EveryOpening_IsFilledByOneDoorOrWindow_InsideItsFrame()
        {
            var fills = _dto.panels.Where(p => p.category == OpeningFillBuilder.Category).ToList();

            Assert.AreEqual(_spec.Openings.Count, fills.Count);
            foreach (var wall in _walls)
            {
                foreach (var opening in wall.Openings)
                {
                    float uMid = (opening.Offset + opening.End) * 0.5f;
                    float zMid = wall.Base + (opening.Sill + opening.Head) * 0.5f;
                    var inside = fills.Where(p => p.group == wall.Id && Covers(wall, p, uMid, zMid)).ToList();

                    Assert.AreEqual(1, inside.Count, $"{wall.Id} opening at {opening.Offset}");
                    var fill = inside[0];
                    Assert.AreEqual(opening.Kind == OpeningKind.Door ? OpeningFillBuilder.DoorType : OpeningFillBuilder.WindowType, fill.type, fill.id);
                    Assert.AreEqual(wall.LevelId, fill.level, fill.id);
                    foreach (var c in Corners(fill.corners))
                    {
                        float u = AlongWall(wall, c);
                        Assert.That(u, Is.InRange(opening.Offset - 0.5f, opening.End + 0.5f), fill.id);
                        Assert.That(c.z, Is.InRange(wall.Base + opening.Sill - 0.5f, wall.Base + opening.Head + 0.5f), fill.id);
                    }
                }
            }
        }

        [Test]
        public void Generate_OpeningFills_AreCentredInTheWallDepth()
        {
            foreach (var wall in _walls)
            {
                var normal = new Vector2(wall.Direction.y, -wall.Direction.x);
                foreach (var fill in _dto.panels.Where(p => p.group == wall.Id && p.category == OpeningFillBuilder.Category))
                {
                    var c = Corners(fill.corners).ToArray();
                    var extrusion = Vector3.Cross(c[1] - c[0], c[3] - c[0]).normalized;
                    float back = Vector2.Dot(new Vector2(c[0].x, c[0].y) - wall.Start, normal);
                    float front = back + fill.thickness * Vector2.Dot(new Vector2(extrusion.x, extrusion.y), normal);

                    Assert.AreEqual(0f, back + front, 0.5f, $"{fill.id} is off-centre");
                    Assert.Less(Mathf.Abs(back), _spec.StudDepth * 0.5f, $"{fill.id} sticks out of the frame");
                }
            }
        }

        private static bool Covers(WallSpec wall, PanelDto panel, float u, float z)
        {
            var c = Corners(panel.corners).ToArray();
            float u0 = c.Min(p => AlongWall(wall, p));
            float u1 = c.Max(p => AlongWall(wall, p));
            return u > u0 && u < u1 && z > c.Min(p => p.z) && z < c.Max(p => p.z);
        }

        private static bool IsSheathing(PanelDto panel) => panel.category == "Sheathing";

        private static bool IsGable(PanelDto panel) => panel.id.EndsWith("-GB01");

        private static float QuadArea(Vector3[] c) => 0.5f * Vector3.Cross(c[2] - c[0], c[3] - c[1]).magnitude;

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
