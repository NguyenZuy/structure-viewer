using System;

namespace StructureViewer.Domain.Structure
{
    public readonly struct Section : IEquatable<Section>
    {
        // Metres. Width lies on the member's local X, depth on local Y.
        public Section(float width, float depth)
        {
            Width = width;
            Depth = depth;
        }

        public float Width { get; }
        public float Depth { get; }
        public float Area => Width * Depth;

        // "35 × 90" in millimetres, as shown in the info panel and takeoff.
        public string Label => $"{Math.Round(Width * 1000f)} × {Math.Round(Depth * 1000f)}";

        public bool Equals(Section other) => Width.Equals(other.Width) && Depth.Equals(other.Depth);
        public override bool Equals(object obj) => obj is Section other && Equals(other);
        public override int GetHashCode() => (Width.GetHashCode() * 397) ^ Depth.GetHashCode();
        public override string ToString() => Label;
    }
}
