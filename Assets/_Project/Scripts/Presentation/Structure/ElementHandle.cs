using UnityEngine;

namespace StructureViewer.Presentation.Structure
{
    // Sits on each element's pick collider so a raycast hit maps back to the element index.
    public sealed class ElementHandle : MonoBehaviour
    {
        public int Index { get; private set; }
        public StructureRenderer Owner { get; private set; }

        public void Init(StructureRenderer owner, int index)
        {
            Owner = owner;
            Index = index;
        }
    }
}
