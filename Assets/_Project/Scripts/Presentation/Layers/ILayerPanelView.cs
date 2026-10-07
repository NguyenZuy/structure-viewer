using System;

namespace StructureViewer.Presentation.Layers
{
    public interface ILayerPanelView
    {
        // Row key and the new toggle value.
        event Action<int, bool> CategoryToggled;
        event Action<int, bool> LevelToggled;
        event Action<int> IsolateLevelClicked;
        event Action ShowAllClicked;

        void Render(LayerPanelContent content);
    }
}
