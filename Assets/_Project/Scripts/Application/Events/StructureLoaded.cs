using StructureViewer.Domain.Structure;

namespace StructureViewer.Application.Events
{
    public readonly struct StructureLoaded
    {
        public StructureLoaded(StructureModel model) => Model = model;

        public StructureModel Model { get; }
    }
}
