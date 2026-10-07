using System;

namespace StructureViewer.Presentation.Info
{
    public interface IInfoPanelView
    {
        event Action SelectAssemblyClicked;
        event Action IsolateAssemblyClicked;
        event Action ClearClicked;

        void Render(InfoPanelContent content);
    }
}
