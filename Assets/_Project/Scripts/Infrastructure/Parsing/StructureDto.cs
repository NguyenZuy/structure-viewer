using System;

// Field names mirror the JSON schema in docs/DESIGN.md exactly (JsonUtility maps by field name),
// hence public lower-case fields. Point lists are flat because JsonUtility can't read nested arrays.
namespace StructureViewer.Infrastructure.Parsing
{
    [Serializable]
    public sealed class StructureDto
    {
        public string name;
        public string units;
        public string upAxis;
        public LevelDto[] levels;
        public MemberDto[] members;
        public SlabDto[] slabs;
        public PanelDto[] panels;
    }

    [Serializable]
    public sealed class LevelDto
    {
        public string id;
        public string name;
        public float elevation;
    }

    [Serializable]
    public sealed class MemberDto
    {
        public string id;
        public string category;
        public string type;
        public string group;
        public string level;
        public float[] start;
        public float[] end;
        public float roll;
        public SectionDto section;
        public string material;
    }

    [Serializable]
    public sealed class SectionDto
    {
        public float width;
        public float depth;
    }

    [Serializable]
    public sealed class SlabDto
    {
        public string id;
        public string level;
        public float[] outline;
        public float top;
        public float thickness;
    }

    [Serializable]
    public sealed class PanelDto
    {
        public string id;
        public string category;
        public string type;
        public string group;
        public string level;
        public float[] corners;
        public float thickness;
    }
}
