using System.Collections.Generic;
using StructureViewer.Domain.Labels;

namespace StructureViewer.Presentation.Labels
{
    // The view projects anchors every frame; the presenter only pushes what changes (anchors, visibility, emphasis).
    public interface ILabelsView
    {
        // Distances in metres from the camera where labels start and finish fading.
        void ShowAnchors(IReadOnlyList<AssemblyAnchor> anchors, float nearFade, float farFade);
        void SetGroupVisible(int anchorIndex, bool visible);

        // -1 for none.
        void SetEmphasized(int anchorIndex);
        void SetEnabled(bool enabled);
    }
}
