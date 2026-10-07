using UnityEngine;

namespace StructureViewer.Domain.Geometry
{
    // Placement of a member's local box: X = section width, Y = section depth, Z = member axis, origin at mid-length.
    public readonly struct MemberPose
    {
        public static readonly MemberPose Identity = new MemberPose(Vector3.zero, Vector3.right, Vector3.up, Vector3.forward, 0f);

        public MemberPose(Vector3 position, Vector3 right, Vector3 up, Vector3 forward, float length)
        {
            Position = position;
            Right = right;
            Up = up;
            Forward = forward;
            Length = length;
        }

        public Vector3 Position { get; }
        public Vector3 Right { get; }
        public Vector3 Up { get; }
        public Vector3 Forward { get; }
        public float Length { get; }

        public Vector3 TransformPoint(Vector3 local) => Position + TransformDirection(local);

        public Vector3 TransformDirection(Vector3 local) => Right * local.x + Up * local.y + Forward * local.z;

        // Rotation-matrix → quaternion, computed by hand so the domain stays free of engine calls.
        public Quaternion Rotation
        {
            get
            {
                float m00 = Right.x, m01 = Up.x, m02 = Forward.x;
                float m10 = Right.y, m11 = Up.y, m12 = Forward.y;
                float m20 = Right.z, m21 = Up.z, m22 = Forward.z;
                float trace = m00 + m11 + m22;
                if (trace > 0f)
                {
                    float s = Mathf.Sqrt(trace + 1f) * 2f;
                    return new Quaternion((m21 - m12) / s, (m02 - m20) / s, (m10 - m01) / s, 0.25f * s);
                }
                if (m00 > m11 && m00 > m22)
                {
                    float s = Mathf.Sqrt(1f + m00 - m11 - m22) * 2f;
                    return new Quaternion(0.25f * s, (m01 + m10) / s, (m02 + m20) / s, (m21 - m12) / s);
                }
                if (m11 > m22)
                {
                    float s = Mathf.Sqrt(1f + m11 - m00 - m22) * 2f;
                    return new Quaternion((m01 + m10) / s, 0.25f * s, (m12 + m21) / s, (m02 - m20) / s);
                }
                float t = Mathf.Sqrt(1f + m22 - m00 - m11) * 2f;
                return new Quaternion((m02 + m20) / t, (m12 + m21) / t, 0.25f * t, (m10 - m01) / t);
            }
        }
    }
}
