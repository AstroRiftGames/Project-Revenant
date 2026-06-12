using UnityEngine;

namespace Core.Audio.Data
{
    /// <summary>
    /// Designer-facing asset that fully describes a single audio clip and its playback settings.
    /// Create via: right-click → Audio → Clip Config.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAudioClipConfig", menuName = "Audio/Clip Config")]
    public class AudioClipConfig : ScriptableObject
    {
        [Header("Clip")]
        [SerializeField] private AudioClip _clip;
        [SerializeField] private AudioChannel _channel = AudioChannel.SFX;

        [Header("Playback")]
        [SerializeField, Range(0f, 1f)] private float _volume = 1f;
        [SerializeField] private bool _useRandomPitch;
        [SerializeField, Range(0.1f, 3f)] private float _pitch = 1f;
        [SerializeField, Range(0.1f, 3f)] private float _minPitch = 0.9f;
        [SerializeField, Range(0.1f, 3f)] private float _maxPitch = 1.1f;
        [SerializeField] private bool _loop;

        [Header("Spatial Audio")]
        [SerializeField] private bool _spatialSound;
        [SerializeField, Range(0f, 1f)] private float _spatialBlend = 1f;
        [SerializeField, Min(0f)] private float _minDistance = 1f;
        [SerializeField, Min(0f)] private float _maxDistance = 500f;

        public AudioClip Clip        => _clip;
        public AudioChannel Channel  => _channel;
        public float Volume          => _volume;
        public float Pitch           => _useRandomPitch ? Random.Range(_minPitch, _maxPitch) : _pitch;
        public bool Loop             => _loop;
        public bool SpatialSound     => _spatialSound;
        public float SpatialBlend    => _spatialSound ? _spatialBlend : 0f;
        public float MinDistance     => _minDistance;
        public float MaxDistance     => _maxDistance;
    }
}
