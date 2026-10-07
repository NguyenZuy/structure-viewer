namespace StructureViewer.Presentation.Input
{
    public readonly struct GestureSettings
    {
        // At 96 dpi; scaled by dpi / 96 so a physical finger wobble is treated the same on dense screens.
        public const float BaseDragThreshold = 8f;
        public const float BaseDoubleTapRadius = 24f;
        public const float DefaultDoubleTapWindow = 0.3f;
        // Wheel delta per notch: Windows reports 120, browsers vary, so amounts are normalised to notches.
        public const float DefaultScrollPerNotch = 120f;
        public const float DefaultZoomPerNotch = 1.15f;

        public GestureSettings(float dragThreshold, float doubleTapRadius, float doubleTapWindow, float scrollPerNotch, float zoomPerNotch)
        {
            DragThreshold = dragThreshold;
            DoubleTapRadius = doubleTapRadius;
            DoubleTapWindow = doubleTapWindow;
            ScrollPerNotch = scrollPerNotch;
            ZoomPerNotch = zoomPerNotch;
        }

        public float DragThreshold { get; }
        public float DoubleTapRadius { get; }
        public float DoubleTapWindow { get; }
        public float ScrollPerNotch { get; }
        public float ZoomPerNotch { get; }

        // dpi <= 0 means unknown (common on WebGL); fall back to 96.
        public static GestureSettings ForDpi(float dpi)
        {
            float scale = dpi > 0f ? dpi / 96f : 1f;
            if (scale < 1f)
                scale = 1f;
            return new GestureSettings(BaseDragThreshold * scale, BaseDoubleTapRadius * scale, DefaultDoubleTapWindow,
                DefaultScrollPerNotch, DefaultZoomPerNotch);
        }
    }
}
