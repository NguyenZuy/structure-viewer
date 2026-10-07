using UnityEngine;

namespace StructureViewer.Presentation.Shell
{
    public readonly struct Insets
    {
        public Insets(float left, float right, float top, float bottom)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }

        public float Left { get; }
        public float Right { get; }
        public float Top { get; }
        public float Bottom { get; }
    }

    // UI Toolkit has no media queries; these decide what USS classes and paddings the shell applies.
    public static class ResponsiveRules
    {
        public const float CompactBreakpoint = 768f;

        public static bool IsCompact(float panelWidth) => panelWidth < CompactBreakpoint;

        // Screen.safeArea is in screen pixels with a bottom-left origin; the panel works in points with a top-left one.
        public static Insets SafeArea(Rect safeArea, Vector2 screenSize, float pixelsPerPoint)
        {
            if (pixelsPerPoint <= 0f)
                pixelsPerPoint = 1f;
            return new Insets(
                Mathf.Max(0f, safeArea.xMin) / pixelsPerPoint,
                Mathf.Max(0f, screenSize.x - safeArea.xMax) / pixelsPerPoint,
                Mathf.Max(0f, screenSize.y - safeArea.yMax) / pixelsPerPoint,
                Mathf.Max(0f, safeArea.yMin) / pixelsPerPoint);
        }
    }
}
