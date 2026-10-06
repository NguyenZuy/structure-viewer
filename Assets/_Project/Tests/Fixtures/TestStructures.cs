using System.Collections.Generic;
using StructureViewer.Domain.Structure;
using UnityEngine;

namespace StructureViewer.Tests.Fixtures
{
    // Code-built models in Unity space (Y-up, metres), independent of the parser and generator.
    public static class TestStructures
    {
        public const string NorthWall = "W-L0-N";
        public const string EastWall = "W-L0-E";
        public const string JoistBay = "FL-L1-A";
        public const string Bearer = "FL-L1-BR";
        public const string Truss1 = "T01";
        public const string Truss2 = "T02";
        public const string RoofNorth = "ROOF-N";
        public const string RoofSouth = "ROOF-S";
        public const string SlabGroup = "SLAB-1";

        public const string VerticalStudId = "W-L0-N-ST01";
        public const string BottomPlateId = "W-L0-N-BP";
        public const string LintelId = "W-L0-N-LT01";
        public const string JoistId = "FL-L1-A-J01";
        public const string SlopedChordId = "T01-TC1";
        public const string RoofPanelId = "RS-N";
        public const string SlabId = "SLAB-1";

        public const int GroundLevel = 0;
        public const int FirstLevel = 1;

        public const float WallLength = 3.6f;
        public const float HouseDepth = 3.0f;
        public const float WallHeight = 2.4f;
        public const float RidgeHeight = 3.4f;
        public const float SlabThickness = 0.1f;

        private static readonly Section Stud = new Section(0.035f, 0.09f);
        private static readonly Section LintelSection = new Section(0.045f, 0.19f);
        private static readonly Section JoistSection = new Section(0.045f, 0.24f);
        private static readonly Section BearerSection = new Section(0.09f, 0.29f);

        public static StructureModel Empty() =>
            new StructureModel("Empty", new Level[0], new Element[0]);

        // 27 elements on 2 levels. North wall has a window opening; studs are vertical; truss top chords slope.
        public static StructureModel MiniHouse()
        {
            var b = new Builder();
            float plateY = Stud.Width * 0.5f;
            float topPlateY = WallHeight - Stud.Width * 0.5f;
            float studBottom = Stud.Width;
            float studTop = WallHeight - Stud.Width;

            // North wall along X at z = 0, window between x 1.2 and 2.4.
            b.Member(BottomPlateId, ElementCategory.Wall, "BottomPlate", NorthWall, GroundLevel, new Vector3(0f, plateY, 0f), new Vector3(WallLength, plateY, 0f), Stud, "MGP10");
            b.Member("W-L0-N-TP", ElementCategory.Wall, "TopPlate", NorthWall, GroundLevel, new Vector3(0f, topPlateY, 0f), new Vector3(WallLength, topPlateY, 0f), Stud, "MGP10");
            b.Member(VerticalStudId, ElementCategory.Wall, "Stud", NorthWall, GroundLevel, new Vector3(0.02f, studBottom, 0f), new Vector3(0.02f, studTop, 0f), Stud, "MGP10");
            b.Member("W-L0-N-ST02", ElementCategory.Wall, "Stud", NorthWall, GroundLevel, new Vector3(3.58f, studBottom, 0f), new Vector3(3.58f, studTop, 0f), Stud, "MGP10");
            b.Member("W-L0-N-TS01", ElementCategory.Wall, "TrimmerStud", NorthWall, GroundLevel, new Vector3(1.2f, studBottom, 0f), new Vector3(1.2f, studTop, 0f), Stud, "MGP10");
            b.Member("W-L0-N-TS02", ElementCategory.Wall, "TrimmerStud", NorthWall, GroundLevel, new Vector3(2.4f, studBottom, 0f), new Vector3(2.4f, studTop, 0f), Stud, "MGP10");
            b.Member(LintelId, ElementCategory.Wall, "Lintel", NorthWall, GroundLevel, new Vector3(1.2f, 2.1f, 0f), new Vector3(2.4f, 2.1f, 0f), LintelSection, "LVL");
            b.Member("W-L0-N-SL01", ElementCategory.Wall, "Sill", NorthWall, GroundLevel, new Vector3(1.2f, 0.9f, 0f), new Vector3(2.4f, 0.9f, 0f), Stud, "MGP10");
            b.Member("W-L0-N-CS01", ElementCategory.Wall, "CrippleStud", NorthWall, GroundLevel, new Vector3(1.8f, studBottom, 0f), new Vector3(1.8f, 0.9f, 0f), Stud, "MGP10");
            b.Member("W-L0-N-NG01", ElementCategory.Wall, "Nogging", NorthWall, GroundLevel, new Vector3(0.02f, 1.2f, 0f), new Vector3(1.2f, 1.2f, 0f), Stud, "MGP10");

            // East wall along Z at x = 3.6, no openings.
            b.Member("W-L0-E-BP", ElementCategory.Wall, "BottomPlate", EastWall, GroundLevel, new Vector3(WallLength, plateY, 0f), new Vector3(WallLength, plateY, HouseDepth), Stud, "MGP10");
            b.Member("W-L0-E-TP", ElementCategory.Wall, "TopPlate", EastWall, GroundLevel, new Vector3(WallLength, topPlateY, 0f), new Vector3(WallLength, topPlateY, HouseDepth), Stud, "MGP10");
            b.Member("W-L0-E-ST01", ElementCategory.Wall, "Stud", EastWall, GroundLevel, new Vector3(WallLength, studBottom, 1.5f), new Vector3(WallLength, studTop, 1.5f), Stud, "MGP10");
            b.Member("W-L0-E-ST02", ElementCategory.Wall, "Stud", EastWall, GroundLevel, new Vector3(WallLength, studBottom, 2.98f), new Vector3(WallLength, studTop, 2.98f), Stud, "MGP10");

            // First floor: two joists spanning Z and a bearer along X.
            b.Member(JoistId, ElementCategory.Floor, "Joist", JoistBay, FirstLevel, new Vector3(1.0f, 2.52f, 0f), new Vector3(1.0f, 2.52f, HouseDepth), JoistSection, "MGP12");
            b.Member("FL-L1-A-J02", ElementCategory.Floor, "Joist", JoistBay, FirstLevel, new Vector3(2.0f, 2.52f, 0f), new Vector3(2.0f, 2.52f, HouseDepth), JoistSection, "MGP12");
            b.Member("FL-L1-BR-B01", ElementCategory.Floor, "Bearer", Bearer, FirstLevel, new Vector3(0f, 2.255f, 1.5f), new Vector3(WallLength, 2.255f, 1.5f), BearerSection, "LVL");

            // Two king-post trusses spanning Z; top chords rise from the eaves to the ridge at z = 1.5.
            AddTruss(b, Truss1, 0.6f, withKingPost: true);
            AddTruss(b, Truss2, 3.0f, withKingPost: false);

            b.Panel(RoofPanelId, "RoofSheathing", RoofNorth, FirstLevel, 0.012f,
                new Vector3(0f, WallHeight, 0f), new Vector3(WallLength, WallHeight, 0f),
                new Vector3(WallLength, RidgeHeight, 1.5f), new Vector3(0f, RidgeHeight, 1.5f));
            b.Panel("RS-S", "RoofSheathing", RoofSouth, FirstLevel, 0.012f,
                new Vector3(0f, RidgeHeight, 1.5f), new Vector3(WallLength, RidgeHeight, 1.5f),
                new Vector3(WallLength, WallHeight, HouseDepth), new Vector3(0f, WallHeight, HouseDepth));

            b.Slab(SlabId, SlabGroup, GroundLevel, 0f, SlabThickness,
                new Vector2(0f, 0f), new Vector2(WallLength, 0f), new Vector2(WallLength, HouseDepth), new Vector2(0f, HouseDepth));

            var levels = new[]
            {
                new Level(GroundLevel, "L0", "Ground Floor", 0f),
                new Level(FirstLevel, "L1", "First Floor", 2.7f)
            };
            return new StructureModel("Mini House", levels, b.Elements);
        }

        private static void AddTruss(Builder b, string group, float x, bool withKingPost)
        {
            var eaveA = new Vector3(x, WallHeight, 0f);
            var eaveB = new Vector3(x, WallHeight, HouseDepth);
            var ridge = new Vector3(x, RidgeHeight, 1.5f);
            b.Member(group + "-BC", ElementCategory.Roof, "TrussBottomChord", group, FirstLevel, eaveA, eaveB, Stud, "MGP10");
            b.Member(group + "-TC1", ElementCategory.Roof, "TrussTopChord", group, FirstLevel, eaveA, ridge, Stud, "MGP10");
            b.Member(group + "-TC2", ElementCategory.Roof, "TrussTopChord", group, FirstLevel, ridge, eaveB, Stud, "MGP10");
            if (withKingPost)
                b.Member(group + "-W1", ElementCategory.Roof, "TrussWeb", group, FirstLevel, new Vector3(x, WallHeight, 1.5f), ridge, Stud, "MGP10");
        }

        private sealed class Builder
        {
            public readonly List<Element> Elements = new List<Element>();

            public void Member(string id, ElementCategory category, string type, string group, int level,
                Vector3 start, Vector3 end, Section section, string material, float roll = 0f)
            {
                var info = new ElementInfo(id, category, type, group, level);
                Elements.Add(Element.ForMember(Elements.Count, info, new Member(start, end, roll, section, material)));
            }

            public void Panel(string id, string type, string group, int level, float thickness, params Vector3[] corners)
            {
                var info = new ElementInfo(id, ElementCategory.Sheathing, type, group, level);
                Elements.Add(Element.ForPanel(Elements.Count, info, new Panel(corners, thickness)));
            }

            public void Slab(string id, string group, int level, float top, float thickness, params Vector2[] outline)
            {
                var info = new ElementInfo(id, ElementCategory.Slab, "Slab", group, level);
                Elements.Add(Element.ForSlab(Elements.Count, info, new Slab(outline, top, thickness)));
            }
        }
    }
}
