using UnityEngine;

namespace StructureViewer.Domain.Structure
{
    // Model files are right-handed Z-up in millimetres; Unity is left-handed Y-up in metres.
    // Swapping Y and Z converts both the up axis and the handedness.
    public static class ModelAxes
    {
        public const float MillimetresPerMetre = 1000f;

        public static Vector3 ToUnity(Vector3 modelMm) =>
            new Vector3(modelMm.x, modelMm.z, modelMm.y) / MillimetresPerMetre;

        public static Vector3 ToUnity(float x, float y, float z) => ToUnity(new Vector3(x, y, z));

        public static Vector3 ToModel(Vector3 unity) =>
            new Vector3(unity.x, unity.z, unity.y) * MillimetresPerMetre;

        // Model plan point (x, y) mm → Unity XZ plane (x, z) m.
        public static Vector2 PlanToUnity(float x, float y) => new Vector2(x, y) / MillimetresPerMetre;

        public static float ToUnityLength(float mm) => mm / MillimetresPerMetre;

        public static float ToModelLength(float metres) => metres * MillimetresPerMetre;
    }
}
