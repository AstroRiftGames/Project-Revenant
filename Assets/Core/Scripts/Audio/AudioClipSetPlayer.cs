using Core.Audio.Data;
using UnityEngine;

namespace Core.Audio
{
    /// <summary>
    /// Drop this on any entity (workstation, NPC, door, etc.) and assign an AudioClipSet.
    /// Call <see cref="Play"/> from animation events, interactable callbacks, or any gameplay code.
    /// </summary>
    public class AudioClipSetPlayer : MonoBehaviour
    {
        [SerializeField] private AudioClipSet _clipSet;

        /// <summary>
        /// Plays the clip mapped to <paramref name="key"/> in the assigned AudioClipSet.
        /// For spatial clips, the sound originates from this GameObject's world position.
        /// </summary>
        public void Play(string key)
        {
            if (_clipSet == null)
            {
                Debug.LogWarning($"[{nameof(AudioClipSetPlayer)}] No AudioClipSet assigned on '{gameObject.name}'.", this);
                return;
            }

            if (!_clipSet.TryGetClip(key, out AudioClipConfig config))
            {
                Debug.LogWarning($"[{nameof(AudioClipSetPlayer)}] Key '{key}' not found in set '{_clipSet.name}'.", this);
                return;
            }

            if (AudioService.Instance == null)
            {
                Debug.LogWarning($"[{nameof(AudioClipSetPlayer)}] AudioService instance is missing.", this);
                return;
            }

            Vector3? position = config.SpatialSound ? transform.position : null;
            AudioService.Instance.PlaySFX(config, position);
        }
    }
}
