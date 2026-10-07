using System;
using UnityEngine;

namespace StructureViewer.Domain.Geometry
{
    public static class MemberGeometry
    {
        // |dot(axis, up)| above this counts as vertical: world up can't define the section orientation then.
        public const float VerticalThreshold = 0.999f;

        // Same as LookRotation(axis, refUp) followed by a roll about the axis; refUp is world up,
        // or world forward for (near) vertical members such as studs.
        public static MemberPose ComputePose(Vector3 start, Vector3 end, float rollDegrees)
        {
            var delta = end - start;
            float length = delta.magnitude;
            if (length < 1e-6f)
                throw new ArgumentException("A member needs two distinct end points.");

            var forward = delta / length;
            var refUp = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > VerticalThreshold ? Vector3.forward : Vector3.up;
            var right = Vector3.Cross(refUp, forward).normalized;
            var up = Vector3.Cross(forward, right);

            float roll = rollDegrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(roll);
            float sin = Mathf.Sin(roll);
            var rolledRight = right * cos + up * sin;
            var rolledUp = up * cos - right * sin;
            return new MemberPose((start + end) * 0.5f, rolledRight, rolledUp, forward, length);
        }
    }
}
