namespace StructureViewer.Domain.Structure
{
    public sealed class Level
    {
        public Level(int index, string id, string name, float elevation)
        {
            Index = index;
            Id = id;
            Name = name;
            Elevation = elevation;
        }

        public int Index { get; }
        public string Id { get; }
        public string Name { get; }

        // Unity Y, metres.
        public float Elevation { get; }
    }
}
