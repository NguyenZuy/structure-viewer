using UnityEngine;

namespace StructureViewer.Presentation.Contracts
{
    // Single entry point for runtime materials (no Shader.Find at runtime). Filled by the Setup Scene tool.
    [CreateAssetMenu(fileName = "RenderingConfig", menuName = "Structure Viewer/Rendering Config")]
    public sealed class RenderingConfig : ScriptableObject
    {
        [SerializeField] private Material _wood;
        [SerializeField] private Material _concrete;
        [SerializeField] private Material _sheathing;
        [SerializeField] private Material _flatOpaque;
        [SerializeField] private Material _flatTransparent;
        [SerializeField] private Material _overlay;
        [SerializeField] private Material _grid;

        public Material Wood => _wood;
        public Material Concrete => _concrete;

        // Transparent, rendered after opaques.
        public Material Sheathing => _sheathing;

        // Templates cloned by display modes; surface type is never switched at runtime (variants may be stripped).
        public Material FlatOpaque => _flatOpaque;
        public Material FlatTransparent => _flatTransparent;

        // Unlit, ZTest Always (measure line and markers).
        public Material Overlay => _overlay;
        public Material Grid => _grid;
    }
}
