using System.Collections.Generic;
using UnityEngine;

namespace StructureViewer.Domain.Measure
{
    public enum SnapKind
    {
        Free,
        Endpoint,
        Midpoint
    }

    public readonly struct SnapCandidate
    {
        public SnapCandidate(Vector3 world, Vector2 screen, SnapKind kind)
        {
            World = world;
            Screen = screen;
            Kind = kind;
        }

        public Vector3 World { get; }
        public Vector2 Screen { get; }
        public SnapKind Kind { get; }
    }

    public readonly struct MeasurePoint
    {
        public MeasurePoint(Vector3 world, SnapKind kind)
        {
            World = world;
            Kind = kind;
        }

        // Unity space, metres.
        public Vector3 World { get; }
        public SnapKind Kind { get; }
    }

    public static class MeasureSnapper
    {
        // Distances are measured on screen, so the radius means the same thing at any zoom.
        public static MeasurePoint Snap(Vector3 hitWorld, Vector2 hitScreen, IReadOnlyList<SnapCandidate> candidates, float radiusPx)
        {
            int best = -1;
            float bestSqr = radiusPx * radiusPx;
            for (int i = 0; candidates != null && i < candidates.Count; i++)
            {
                float sqr = (candidates[i].Screen - hitScreen).sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    best = i;
                }
            }
            return best < 0 ? new MeasurePoint(hitWorld, SnapKind.Free) : new MeasurePoint(candidates[best].World, candidates[best].Kind);
        }
    }
}
