namespace StructureViewer.Domain.Structure
{
    public sealed class ElementInfo
    {
        public ElementInfo(string id, ElementCategory category, string type, string group, int levelIndex)
        {
            Id = id;
            Category = category;
            Type = type;
            Group = group;
            LevelIndex = levelIndex;
        }

        public string Id { get; }
        public ElementCategory Category { get; }
        public string Type { get; }

        // Assembly id (W-L0-N, T03). Empty when the element belongs to no assembly.
        public string Group { get; }
        public int LevelIndex { get; }
    }
}
