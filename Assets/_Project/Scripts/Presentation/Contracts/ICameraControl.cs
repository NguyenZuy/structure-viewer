using UnityEngine;

namespace StructureViewer.Presentation.Contracts
{
    public interface ICameraControl
    {
        Camera Camera { get; }

        void FitAll(Bounds bounds);
        void Focus(Bounds bounds);
    }
}
