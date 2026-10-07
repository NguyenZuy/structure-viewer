namespace StructureViewer.Presentation.Contracts
{
    // Drawn as vectors (Presentation.Shell.Icons): Unicode symbols render in the Editor through OS font fallback but
    // vanish in WebGL, where the runtime font is all there is.
    public enum ToolbarIcon
    {
        FitAll,
        Focus,
        Undo,
        Redo,
        Display,
        Layers,
        Isolate,
        Hide,
        ShowAll,
        Measure,
        Takeoff,
        Labels,
        Multi,
        More,
        ChevronLeft,
        ChevronRight
    }
}
