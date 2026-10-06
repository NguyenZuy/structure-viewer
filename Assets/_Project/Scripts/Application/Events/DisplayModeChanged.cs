using StructureViewer.Domain.Display;

namespace StructureViewer.Application.Events
{
    public readonly struct DisplayModeChanged
    {
        public DisplayModeChanged(DisplayMode mode, ColorByField field)
        {
            Mode = mode;
            Field = field;
        }

        public DisplayMode Mode { get; }
        public ColorByField Field { get; }
    }
}
