using UnityEngine;

namespace StructureViewer.Presentation.Contracts
{
    public interface IStructureRenderer
    {
        int Count { get; }
        Bounds ModelBounds { get; }

        void SetVisible(int index, bool visible);

        // Shared materials only (SRP Batcher); never per-instance copies or property blocks.
        void SetMaterial(int index, Material material);

        Bounds GetWorldBounds(int index);
        bool TryPick(Vector2 screenPosition, out PickHit hit);
    }

    public readonly struct PickHit
    {
        public PickHit(int index, Vector3 point)
        {
            Index = index;
            Point = point;
        }

        public int Index { get; }
        public Vector3 Point { get; }
    }
}
