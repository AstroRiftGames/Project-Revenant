using System;

namespace Core.Audio.Data
{
    /// <summary>
    /// Maps a keyword (e.g. "Interact", "Use", "Close") to a concrete AudioClipConfig.
    /// Used inside AudioClipSet to group clips per entity.
    /// </summary>
    [Serializable]
    public struct AudioClipEntry
    {
        public string Key;
        public AudioClipConfig Config;
    }
}
