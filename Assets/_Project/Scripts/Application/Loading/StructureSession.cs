using System;
using StructureViewer.Domain.Structure;

namespace StructureViewer.Application.Loading
{
    // The currently loaded model. Written by LoadStructureUseCase, read by features.
    public sealed class StructureSession
    {
        public StructureModel Current { get; private set; }
        public bool HasModel => Current != null;

        public void Set(StructureModel model) => Current = model ?? throw new ArgumentNullException(nameof(model));
    }
}
