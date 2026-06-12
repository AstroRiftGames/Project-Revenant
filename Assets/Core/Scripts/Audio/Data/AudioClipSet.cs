using System.Collections.Generic;
using UnityEngine;

namespace Core.Audio.Data
{
    /// <summary>
    /// Groups all audio clips for a single entity (workstation, creature, etc.)
    /// under keyword-based entries. Assign to an AudioClipSetPlayer component.
    /// Create via: right-click → Audio → Clip Set.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAudioClipSet", menuName = "Audio/Clip Set")]
    public class AudioClipSet : ScriptableObject
    {
        [SerializeField] private List<AudioClipEntry> _entries = new();

        /// <summary>
        /// Retrieves the clip config associated with the given keyword (case-sensitive).
        /// Returns false and sets config to null if the key is not found.
        /// </summary>
        public bool TryGetClip(string key, out AudioClipConfig config)
        {
            foreach (AudioClipEntry entry in _entries)
            {
                if (entry.Key == key)
                {
                    config = entry.Config;
                    return true;
                }
            }

            config = null;
            return false;
        }
    }
}
