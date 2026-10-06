using StructureViewer.Domain.Visibility;

namespace StructureViewer.Application.Events
{
    public readonly struct VisibilityChanged
    {
        public VisibilityChanged(IVisibility visibility) => Visibility = visibility;

        public IVisibility Visibility { get; }
    }
}
