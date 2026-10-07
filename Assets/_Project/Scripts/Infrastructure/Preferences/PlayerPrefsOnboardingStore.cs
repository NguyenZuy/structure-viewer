using System;
using StructureViewer.Application.Onboarding;
using UnityEngine;

namespace StructureViewer.Infrastructure.Preferences
{
    // PlayerPrefs is IndexedDB/localStorage on WebGL, which can throw when storage is blocked; a hint that shows again is harmless.
    public sealed class PlayerPrefsOnboardingStore : IOnboardingStore
    {
        private const string Key = "structure-viewer.hint-seen";

        public bool HasSeenHint
        {
            get
            {
                try
                {
                    return PlayerPrefs.GetInt(Key, 0) == 1;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Preferences unavailable: {e.Message}");
                    return false;
                }
            }
        }

        public void MarkHintSeen()
        {
            try
            {
                PlayerPrefs.SetInt(Key, 1);
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Preferences unavailable: {e.Message}");
            }
        }
    }
}
