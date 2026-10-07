using System.Collections.Generic;

namespace StructureViewer.Presentation.Shell
{
    public static class ToolbarOverflow
    {
        // Fills `visible` (same length as priorities). Everything fits → all visible, no overflow button. Otherwise the
        // overflow button takes one slot and the highest priorities keep the rest; ties go to the earlier item.
        // Returns whether the overflow button is needed.
        public static bool Split(IReadOnlyList<int> priorities, float available, float itemWidth, float overflowWidth, bool[] visible)
        {
            int count = priorities.Count;
            if (count * itemWidth <= available)
            {
                for (int i = 0; i < count; i++)
                    visible[i] = true;
                return false;
            }

            int capacity = itemWidth > 0f ? (int)((available - overflowWidth) / itemWidth) : 0;
            for (int i = 0; i < count; i++)
            {
                // Rank = how many items beat this one; it stays visible if fewer than `capacity` do.
                int rank = 0;
                for (int j = 0; j < count; j++)
                    if (priorities[j] > priorities[i] || (priorities[j] == priorities[i] && j < i))
                        rank++;
                visible[i] = rank < capacity;
            }
            return true;
        }
    }
}
