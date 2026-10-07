using System.Collections.Generic;
using UnityEngine;

namespace StructureViewer.Domain.Labels
{
    // Greedy overlap removal: labels are placed in priority order and any label overlapping one already placed is hidden.
    // Allocation-free so it can run every frame.
    public static class LabelDeclutter
    {
        // order: label indices, highest priority first. keep: in = candidate, out = still shown.
        public static void Apply(IReadOnlyList<Rect> rects, IReadOnlyList<int> order, bool[] keep, float padding)
        {
            for (int a = 0; a < order.Count; a++)
            {
                int i = order[a];
                if (!keep[i])
                    continue;
                for (int b = 0; b < a; b++)
                {
                    int j = order[b];
                    if (keep[j] && Overlaps(rects[i], rects[j], padding))
                    {
                        keep[i] = false;
                        break;
                    }
                }
            }
        }

        // Insertion sort by key (small first): the order barely changes between frames, so this is close to linear.
        public static void SortByKey(int[] order, float[] key, int count)
        {
            for (int a = 1; a < count; a++)
            {
                int item = order[a];
                float value = key[item];
                int b = a - 1;
                while (b >= 0 && key[order[b]] > value)
                {
                    order[b + 1] = order[b];
                    b--;
                }
                order[b + 1] = item;
            }
        }

        private static bool Overlaps(Rect a, Rect b, float padding) =>
            a.xMin < b.xMax + padding && b.xMin < a.xMax + padding && a.yMin < b.yMax + padding && b.yMin < a.yMax + padding;
    }
}
