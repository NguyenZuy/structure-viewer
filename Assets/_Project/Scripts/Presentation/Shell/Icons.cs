using System.Collections.Generic;
using StructureViewer.Presentation.Contracts;
using UnityEngine;
using UnityEngine.UIElements;

namespace StructureViewer.Presentation.Shell
{
    // Toolbar icons drawn once as white vector images and shown as background images, so USS tints them per state
    // (-unity-background-image-tint-color) and they stay sharp at any DPI. No font or texture involved.
    public static class Icons
    {
        public const float Size = 24f;
        private const float Stroke = 2f;

        private static readonly Dictionary<ToolbarIcon, VectorImage> Cache = new Dictionary<ToolbarIcon, VectorImage>();

        public static VisualElement Create(ToolbarIcon icon, string className)
        {
            var element = new VisualElement { pickingMode = PickingMode.Ignore };
            element.AddToClassList(className);
            Set(element, icon);
            return element;
        }

        public static void Set(VisualElement element, ToolbarIcon icon) =>
            element.style.backgroundImage = new StyleBackground(Background.FromVectorImage(Get(icon)));

        public static VectorImage Get(ToolbarIcon icon)
        {
            if (Cache.TryGetValue(icon, out var image) && image != null)
                return image;

            var painter = new Painter2D { lineWidth = Stroke, lineCap = LineCap.Round, lineJoin = LineJoin.Round };
            try
            {
                // A near-invisible full-size square gives every icon the same 24 × 24 bounds, so they scale alike.
                painter.fillColor = new Color(1f, 1f, 1f, 0.004f);
                Polygon(painter, true, new Vector2(0f, 0f), new Vector2(Size, 0f), new Vector2(Size, Size), new Vector2(0f, Size));
                painter.strokeColor = Color.white;
                painter.fillColor = Color.white;
                Draw(painter, icon);

                image = ScriptableObject.CreateInstance<VectorImage>();
                image.name = $"Icon {icon}";
                // Created at runtime and only referenced from this cache: keep asset GC from unloading it.
                image.hideFlags = HideFlags.HideAndDontSave;
                painter.SaveToVectorImage(image);
            }
            finally
            {
                painter.Dispose();
            }
            Cache[icon] = image;
            return image;
        }

        // 24 × 24 design grid, y down.
        private static void Draw(Painter2D p, ToolbarIcon icon)
        {
            switch (icon)
            {
                case ToolbarIcon.FitAll:
                    Line(p, V(3, 8), V(3, 3), V(8, 3));
                    Line(p, V(16, 3), V(21, 3), V(21, 8));
                    Line(p, V(21, 16), V(21, 21), V(16, 21));
                    Line(p, V(8, 21), V(3, 21), V(3, 16));
                    break;
                case ToolbarIcon.Focus:
                    Circle(p, V(12, 12), 7f, fill: false);
                    Circle(p, V(12, 12), 2.5f, fill: true);
                    Line(p, V(12, 1.5f), V(12, 4));
                    Line(p, V(12, 20), V(12, 22.5f));
                    Line(p, V(1.5f, 12), V(4, 12));
                    Line(p, V(20, 12), V(22.5f, 12));
                    break;
                case ToolbarIcon.Undo:
                    UTurn(p, mirror: false);
                    break;
                case ToolbarIcon.Redo:
                    UTurn(p, mirror: true);
                    break;
                case ToolbarIcon.Display:
                    Circle(p, V(12, 12), 8.5f, fill: false);
                    p.BeginPath();
                    p.Arc(V(12, 12), 8.5f, Angle.Degrees(90f), Angle.Degrees(270f));
                    p.ClosePath();
                    p.Fill();
                    break;
                case ToolbarIcon.Layers:
                    Polygon(p, false, V(12, 3), V(21, 8), V(12, 13), V(3, 8));
                    Line(p, V(3, 12.5f), V(12, 17.5f), V(21, 12.5f));
                    Line(p, V(3, 16.5f), V(12, 21.5f), V(21, 16.5f));
                    break;
                case ToolbarIcon.Isolate:
                    Polygon(p, false, V(3, 3), V(21, 3), V(21, 21), V(3, 21));
                    Polygon(p, true, V(8, 8), V(16, 8), V(16, 16), V(8, 16));
                    break;
                case ToolbarIcon.Hide:
                    Eye(p);
                    Line(p, V(4, 4), V(20, 20));
                    break;
                case ToolbarIcon.ShowAll:
                    Eye(p);
                    Circle(p, V(12, 12), 1.5f, fill: true);
                    break;
                case ToolbarIcon.Measure:
                    Line(p, V(3, 12), V(21, 12));
                    Line(p, V(7, 8), V(3, 12), V(7, 16));
                    Line(p, V(17, 8), V(21, 12), V(17, 16));
                    Line(p, V(12, 9), V(12, 15));
                    break;
                case ToolbarIcon.Takeoff:
                    Polygon(p, false, V(3, 4), V(21, 4), V(21, 20), V(3, 20));
                    Line(p, V(3, 9.5f), V(21, 9.5f));
                    Line(p, V(3, 15), V(21, 15));
                    Line(p, V(9.5f, 9.5f), V(9.5f, 20));
                    break;
                case ToolbarIcon.Labels:
                    Polygon(p, false, V(3, 3.5f), V(11.5f, 3.5f), V(20.5f, 12.5f), V(12.5f, 20.5f), V(3, 11));
                    Circle(p, V(8, 8.5f), 1.6f, fill: true);
                    break;
                case ToolbarIcon.Multi:
                    Polygon(p, false, V(3, 7), V(15, 7), V(15, 21), V(3, 21));
                    Line(p, V(8, 3), V(21, 3), V(21, 16));
                    Line(p, V(9, 11), V(9, 17));
                    Line(p, V(6, 14), V(12, 14));
                    break;
                case ToolbarIcon.More:
                    Circle(p, V(5, 12), 2f, fill: true);
                    Circle(p, V(12, 12), 2f, fill: true);
                    Circle(p, V(19, 12), 2f, fill: true);
                    break;
                case ToolbarIcon.ChevronLeft:
                    Line(p, V(15, 5), V(8, 12), V(15, 19));
                    break;
                case ToolbarIcon.ChevronRight:
                    Line(p, V(9, 5), V(16, 12), V(9, 19));
                    break;
            }
        }

        // An arrow that heads one way and bends back: undo points left, redo is its mirror image.
        private static void UTurn(Painter2D p, bool mirror)
        {
            Vector2 M(float x, float y) => V(mirror ? Size - x : x, y);
            Line(p, M(9, 5), M(4, 10), M(9, 15));
            p.BeginPath();
            p.MoveTo(M(4, 10));
            p.LineTo(M(15, 10));
            p.Arc(M(15, 15), 5f, Angle.Degrees(mirror ? 270f : -90f), Angle.Degrees(90f),
                mirror ? ArcDirection.CounterClockwise : ArcDirection.Clockwise);
            p.LineTo(M(10, 20));
            p.Stroke();
        }

        private static void Eye(Painter2D p)
        {
            // Two open lids with round caps: a closed outline would need joins at the near-zero-angle corners,
            // which Painter2D tessellates into spikes.
            foreach (float lid in new[] { 5.5f, 18.5f })
            {
                p.BeginPath();
                p.MoveTo(V(3, 12));
                p.BezierCurveTo(V(6, lid), V(18, lid), V(21, 12));
                p.Stroke();
            }
            Circle(p, V(12, 12), 3.5f, fill: false);
        }

        private static void Line(Painter2D p, params Vector2[] points)
        {
            p.BeginPath();
            p.MoveTo(points[0]);
            for (int i = 1; i < points.Length; i++)
                p.LineTo(points[i]);
            p.Stroke();
        }

        private static void Polygon(Painter2D p, bool fill, params Vector2[] points)
        {
            p.BeginPath();
            p.MoveTo(points[0]);
            for (int i = 1; i < points.Length; i++)
                p.LineTo(points[i]);
            p.ClosePath();
            if (fill)
                p.Fill();
            else
                p.Stroke();
        }

        private static void Circle(Painter2D p, Vector2 centre, float radius, bool fill)
        {
            p.BeginPath();
            p.Arc(centre, radius, Angle.Degrees(0f), Angle.Degrees(360f));
            p.ClosePath();
            if (fill)
                p.Fill();
            else
                p.Stroke();
        }

        private static Vector2 V(float x, float y) => new Vector2(x, y);
    }
}
