using System.Collections.Generic;
using UnityEngine;

namespace StructureViewer.Infrastructure.Quality
{
    // Quality levels "Mobile"/"PC" each point at their URP asset (Mobile_RPAsset / PC_RPAsset).
    public static class QualitySelector
    {
        public const string MobileLevel = "Mobile";
        public const string PcLevel = "PC";

        // Index of the level to use, or -1 when the project has no level with the expected name.
        public static int SelectLevel(bool isMobile, IReadOnlyList<string> levelNames)
        {
            string wanted = isMobile ? MobileLevel : PcLevel;
            for (int i = 0; i < levelNames.Count; i++)
            {
                if (levelNames[i] == wanted)
                    return i;
            }
            return -1;
        }

        public static void Apply()
        {
            int level = SelectLevel(UnityEngine.Application.isMobilePlatform, QualitySettings.names);
            if (level < 0)
            {
                Debug.LogWarning($"QualitySelector: no '{MobileLevel}'/'{PcLevel}' quality level; keeping '{QualitySettings.names[QualitySettings.GetQualityLevel()]}'.");
                return;
            }
            if (level != QualitySettings.GetQualityLevel())
                QualitySettings.SetQualityLevel(level, applyExpensiveChanges: true);
        }
    }
}
