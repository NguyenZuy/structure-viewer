namespace StructureViewer.Application.Events
{
    public readonly struct HoverChanged
    {
        public const int None = -1;

        public HoverChanged(int index) => Index = index;

        public int Index { get; }
        public bool HasHover => Index != None;
    }
}
