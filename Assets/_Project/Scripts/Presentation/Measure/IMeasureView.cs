using StructureViewer.Domain.Measure;
using UnityEngine;

namespace StructureViewer.Presentation.Measure
{
    public interface IMeasureView
    {
        // Screen.dpi; 0 when unknown.
        float Dpi { get; }

        // Screen pixels, bottom-left origin (same space as pointer events). False when the point is behind the camera.
        bool TryWorldToScreen(Vector3 world, out Vector2 screen);

        void Render(MeasureVisual visual);
    }

    public readonly struct MeasureVisual
    {
        public static readonly MeasureVisual None = default;

        public MeasureVisual(bool hasA, MeasurePoint a, bool hasB, MeasurePoint b, string label)
        {
            HasA = hasA;
            A = a;
            HasB = hasB;
            B = b;
            Label = label;
        }

        public bool HasA { get; }
        public MeasurePoint A { get; }
        public bool HasB { get; }
        public MeasurePoint B { get; }

        // Null until both points exist.
        public string Label { get; }
    }
}
