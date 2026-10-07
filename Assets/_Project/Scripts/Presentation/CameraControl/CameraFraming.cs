using UnityEngine;

namespace StructureViewer.Presentation.CameraControl
{
    public static class CameraFraming
    {
        public const float DefaultMargin = 1.1f;
        private const float NearClearance = 0.1f;

        // Smallest distance from the bounds centre, along the view direction, that keeps all 8 corners inside both
        // the vertical and the horizontal FOV. Exact per corner, so portrait screens back off only as much as needed.
        public static float FitDistance(Bounds bounds, Quaternion rotation, float verticalFov, float aspect,
            float margin = DefaultMargin)
        {
            float tanV = Mathf.Tan(verticalFov * 0.5f * Mathf.Deg2Rad) / margin;
            float tanH = tanV * aspect;
            // Conjugate = inverse for a unit quaternion; Quaternion.Inverse is a native call.
            var toCamera = new Quaternion(-rotation.x, -rotation.y, -rotation.z, rotation.w);
            var e = bounds.extents;

            float distance = 0f;
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
                var p = toCamera * corner;
                distance = Mathf.Max(distance, Mathf.Abs(p.x) / tanH - p.z);
                distance = Mathf.Max(distance, Mathf.Abs(p.y) / tanV - p.z);
                distance = Mathf.Max(distance, p.z + NearClearance);
            }
            return distance;
        }
    }
}
