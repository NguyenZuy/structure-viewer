using StructureViewer.Domain.Structure;
using UnityEngine;

namespace StructureViewer.Domain.Measure
{
    public enum MeasureState
    {
        Idle,
        AwaitingA,
        AwaitingB,
        Done
    }

    // One measurement at a time: A, then B; a pick after Done starts the next measurement at that point.
    public sealed class MeasureSession
    {
        public MeasureState State { get; private set; } = MeasureState.Idle;
        public MeasurePoint A { get; private set; }
        public MeasurePoint B { get; private set; }

        public bool HasA => State == MeasureState.AwaitingB || State == MeasureState.Done;
        public bool HasResult => State == MeasureState.Done;
        public MeasureResult Result => HasResult ? MeasureResult.Between(A.World, B.World) : default;

        // Also restarts a running measurement.
        public void Start()
        {
            A = default;
            B = default;
            State = MeasureState.AwaitingA;
        }

        // False when idle (picks are ignored).
        public bool Pick(MeasurePoint point)
        {
            switch (State)
            {
                case MeasureState.AwaitingA:
                case MeasureState.Done:
                    A = point;
                    B = default;
                    State = MeasureState.AwaitingB;
                    return true;
                case MeasureState.AwaitingB:
                    B = point;
                    State = MeasureState.Done;
                    return true;
                default:
                    return false;
            }
        }

        public void Cancel()
        {
            A = default;
            B = default;
            State = MeasureState.Idle;
        }
    }

    public readonly struct MeasureResult
    {
        private MeasureResult(float distance, Vector3 delta)
        {
            Distance = distance;
            Delta = delta;
        }

        // Millimetres.
        public float Distance { get; }

        // |dx|, |dy|, |dz| in model axes (Z-up, mm), so dz is height rather than plan depth.
        public Vector3 Delta { get; }

        public static MeasureResult Between(Vector3 unityA, Vector3 unityB)
        {
            var delta = ModelAxes.ToModel(unityB) - ModelAxes.ToModel(unityA);
            return new MeasureResult(delta.magnitude, new Vector3(Mathf.Abs(delta.x), Mathf.Abs(delta.y), Mathf.Abs(delta.z)));
        }
    }
}
