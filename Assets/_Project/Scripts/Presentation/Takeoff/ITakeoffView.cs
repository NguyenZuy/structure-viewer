using System;

namespace StructureViewer.Presentation.Takeoff
{
    public interface ITakeoffView
    {
        event Action<bool> VisibleOnlyToggled;

        void Render(TakeoffContent content);
    }
}
