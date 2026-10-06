using UnityEngine;

namespace StructureViewer.Domain.Structure
{
    public sealed class Member
    {
        // Start/End are centreline endpoints in Unity space (Y-up, metres). Roll is degrees about the member axis.
        public Member(Vector3 start, Vector3 end, float roll, Section section, string material)
        {
            Start = start;
            End = end;
            Roll = roll;
            Section = section;
            Material = material;
        }

        public Vector3 Start { get; }
        public Vector3 End { get; }
        public float Roll { get; }
        public Section Section { get; }
        public string Material { get; }

        public float Length => Vector3.Distance(Start, End);
        public Vector3 Midpoint => (Start + End) * 0.5f;
        public float Volume => Length * Section.Area;

        // Roll-independent: the box fits inside a cylinder of radius half-diagonal, whose extent on
        // axis k is r * sqrt(1 - dir_k^2). End caps are flat, so the endpoints bound the axial extent.
        public Bounds ComputeBounds()
        {
            var dir = End - Start;
            float length = dir.magnitude;
            dir = length > 0f ? dir / length : Vector3.zero;
            float r = 0.5f * Mathf.Sqrt(Section.Width * Section.Width + Section.Depth * Section.Depth);
            var expand = new Vector3(
                r * Mathf.Sqrt(Mathf.Max(0f, 1f - dir.x * dir.x)),
                r * Mathf.Sqrt(Mathf.Max(0f, 1f - dir.y * dir.y)),
                r * Mathf.Sqrt(Mathf.Max(0f, 1f - dir.z * dir.z)));

            var bounds = new Bounds(Start, Vector3.zero);
            bounds.Encapsulate(End);
            bounds.Expand(expand * 2f);
            return bounds;
        }
    }
}
