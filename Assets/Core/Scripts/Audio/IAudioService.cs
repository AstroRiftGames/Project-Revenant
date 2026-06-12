using System;
using Core.Audio.Data;
using UnityEngine;

namespace Core.Audio
{
    /// <summary>
    /// Contract for the audio subsystem. Expose this interface to any UI or system
    /// that needs to control playback or channel volumes without coupling to AudioService.
    /// </summary>
    public interface IAudioService
    {
        // ── Playback ──────────────────────────────────────────────────────────

        /// <summary>Plays a one-shot or looping SFX clip, optionally at a world position.</summary>
        void PlaySFX(AudioClipConfig config, Vector3? worldPosition = null);

        /// <summary>Plays a music clip on the dedicated music channel (cross-fades if already playing).</summary>
        void PlayMusic(AudioClipConfig config);

        /// <summary>Fades out and stops the current music track.</summary>
        void StopMusic();

        // ── Channel volumes ────────────────────────────────────────────────────

        /// <summary>Master volume for the SFX channel [0, 1].</summary>
        float SfxVolume { get; set; }

        /// <summary>Master volume for the Music channel [0, 1].</summary>
        float MusicVolume { get; set; }

        // ── Events (for UI bindings) ──────────────────────────────────────────

        /// <summary>Fired whenever SfxVolume changes.</summary>
        event Action<float> OnSfxVolumeChanged;

        /// <summary>Fired whenever MusicVolume changes.</summary>
        event Action<float> OnMusicVolumeChanged;
    }
}
