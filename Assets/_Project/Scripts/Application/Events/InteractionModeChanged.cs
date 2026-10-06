using StructureViewer.Domain.Interaction;

namespace StructureViewer.Application.Events
{
    public readonly struct InteractionModeChanged
    {
        public InteractionModeChanged(InteractionMode mode) => Mode = mode;

        public InteractionMode Mode { get; }
    }
}
