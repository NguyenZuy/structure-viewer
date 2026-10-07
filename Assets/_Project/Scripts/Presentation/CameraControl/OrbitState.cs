using UnityEngine;

namespace StructureViewer.Presentation.CameraControl
{
    // Orbit camera pose around a pivot. Immutable; every operation returns the next state with limits applied.
    // Rotation is built by hand (yaw about Y, then pitch about X, as Quaternion.Euler) to stay engine-free for tests.
    public readonly struct OrbitState
    {
        public const float MinPitch = 5f;
        public const float MaxPitch = 89f;

        public OrbitState(Vector3 pivot, float yaw, float pitch, float distance, float minDistance, float maxDistance)
        {
            Pivot = pivot;
            Yaw = yaw;
            Pitch = Mathf.Clamp(pitch, MinPitch, MaxPitch);
            MinDistance = minDistance;
            MaxDistance = maxDistance;
            Distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        public Vector3 Pivot { get; }
        public float Yaw { get; }
        public float Pitch { get; }
        public float Distance { get; }
        public float MinDistance { get; }
        public float MaxDistance { get; }

        public Quaternion Rotation
        {
            get
            {
                float yaw = Yaw * 0.5f * Mathf.Deg2Rad;
                float pitch = Pitch * 0.5f * Mathf.Deg2Rad;
                return new Quaternion(0f, Mathf.Sin(yaw), 0f, Mathf.Cos(yaw)) *
                       new Quaternion(Mathf.Sin(pitch), 0f, 0f, Mathf.Cos(pitch));
            }
        }

        public Vector3 Forward => Rotation * Vector3.forward;
        public Vector3 Position => Pivot - Forward * Distance;

        // Grab-style: dragging right/up swings the camera left/down, so the model turns with the pointer.
        public OrbitState Orbit(Vector2 degrees) =>
            new OrbitState(Pivot, Yaw + degrees.x, Pitch - degrees.y, Distance, MinDistance, MaxDistance);

        // Moves the pivot against the drag so the content under the pointer follows it.
        public OrbitState Pan(Vector2 pixels, float worldPerPixel)
        {
            var rotation = Rotation;
            var offset = (rotation * Vector3.right * pixels.x + rotation * Vector3.up * pixels.y) * worldPerPixel;
            return WithPivot(Pivot - offset);
        }

        // Amount > 1 zooms in. The camera slides along the pointer ray, so the point under the pointer stays put and
        // the pivot shifts sideways toward it on the same depth plane.
        public OrbitState Zoom(float amount, Vector3 focusDirection)
        {
            if (amount <= 0f)
                return this;

            float distance = Mathf.Clamp(Distance / amount, MinDistance, MaxDistance);
            var forward = Forward;
            float along = Vector3.Dot(focusDirection, forward);
            if (along < 0.01f)
                return new OrbitState(Pivot, Yaw, Pitch, distance, MinDistance, MaxDistance);

            var position = Position + focusDirection * ((Distance - distance) / along);
            return new OrbitState(position + forward * distance, Yaw, Pitch, distance, MinDistance, MaxDistance);
        }

        // World direction from the camera through a viewport point (0..1).
        public Vector3 ViewportDirection(Vector2 viewport, float verticalFov, float aspect)
        {
            float tanV = Mathf.Tan(verticalFov * 0.5f * Mathf.Deg2Rad);
            var local = new Vector3((viewport.x * 2f - 1f) * tanV * aspect, (viewport.y * 2f - 1f) * tanV, 1f);
            return Rotation * local.normalized;
        }

        public OrbitState WithPivot(Vector3 pivot) =>
            new OrbitState(pivot, Yaw, Pitch, Distance, MinDistance, MaxDistance);

        public OrbitState WithFrame(Vector3 pivot, float distance) =>
            new OrbitState(pivot, Yaw, Pitch, distance, MinDistance, MaxDistance);

        public OrbitState WithLimits(float minDistance, float maxDistance) =>
            new OrbitState(Pivot, Yaw, Pitch, Distance, minDistance, maxDistance);

        // Distance interpolates in log space so zooming feels uniform from close-up to far.
        public static OrbitState Lerp(OrbitState from, OrbitState to, float t) =>
            new OrbitState(
                Vector3.Lerp(from.Pivot, to.Pivot, t),
                Mathf.Lerp(from.Yaw, to.Yaw, t),
                Mathf.Lerp(from.Pitch, to.Pitch, t),
                Mathf.Exp(Mathf.Lerp(Mathf.Log(from.Distance), Mathf.Log(to.Distance), t)),
                to.MinDistance,
                to.MaxDistance);
    }
}
