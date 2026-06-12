using System;
using System.Collections;
using System.Collections.Generic;
using Core.Audio.Data;
using UnityEngine;

namespace Core.Audio
{
    /// <summary>
    /// Central audio singleton. Manages a dynamic SFX pool and a dedicated music channel
    /// with configurable cross-fade. Channel volumes are persisted via PlayerPrefs.
    ///
    /// Setup: place the [Manager_Audio] prefab in your persistent scene.
    /// The prefab must have a child AudioSource named "Music_Channel" assigned
    /// to <see cref="_musicSource"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class AudioService : MonoBehaviour, IAudioService
    {
        private const string PrefsSfxKey   = "Audio_SFX_Volume";
        private const string PrefsMusicKey = "Audio_Music_Volume";

        public static AudioService Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Music Channel")]
        [SerializeField] private AudioSource _musicSource;
        [SerializeField, Min(0f)] private float _fadeDuration = 0.5f;

        [Header("SFX Pool")]
        [Tooltip("Parent transform for pooled SFX AudioSources. Defaults to this transform.")]
        [SerializeField] private Transform _sfxPoolRoot;

        // ── Private state ─────────────────────────────────────────────────────

        private readonly List<AudioSource>          _sfxPool           = new();
        private readonly Dictionary<AudioSource, float> _sfxBaseVolumes = new();

        private float      _sfxVolume   = 1f;
        private float      _musicVolume = 1f;
        private Coroutine  _musicFadeCoroutine;

        // ── IAudioService events ──────────────────────────────────────────────

        public event Action<float> OnSfxVolumeChanged;
        public event Action<float> OnMusicVolumeChanged;

        // ── IAudioService properties ──────────────────────────────────────────

        public float SfxVolume
        {
            get => _sfxVolume;
            set
            {
                _sfxVolume = Mathf.Clamp01(value);
                ApplySfxVolume();
                PlayerPrefs.SetFloat(PrefsSfxKey, _sfxVolume);
                OnSfxVolumeChanged?.Invoke(_sfxVolume);
            }
        }

        public float MusicVolume
        {
            get => _musicVolume;
            set
            {
                _musicVolume = Mathf.Clamp01(value);
                if (_musicSource != null)
                    _musicSource.volume = _musicVolume;
                PlayerPrefs.SetFloat(PrefsMusicKey, _musicVolume);
                OnMusicVolumeChanged?.Invoke(_musicVolume);
            }
        }

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadVolumes();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // ── IAudioService — Playback ──────────────────────────────────────────

        public void PlaySFX(AudioClipConfig config, Vector3? worldPosition = null)
        {
            if (config == null || config.Clip == null)
            {
                Debug.LogWarning($"[{nameof(AudioService)}] PlaySFX called with a null or empty config.");
                return;
            }

            AudioSource source = GetOrCreateSfxSource();
            ConfigureSfxSource(source, config);

            source.transform.position = worldPosition ?? transform.position;
            source.Play();
        }

        public void PlayMusic(AudioClipConfig config)
        {
            if (config == null || config.Clip == null)
            {
                Debug.LogWarning($"[{nameof(AudioService)}] PlayMusic called with a null or empty config.");
                return;
            }

            if (_musicFadeCoroutine != null)
                StopCoroutine(_musicFadeCoroutine);

            _musicFadeCoroutine = StartCoroutine(CrossFadeMusic(config));
        }

        public void StopMusic()
        {
            if (_musicFadeCoroutine != null)
                StopCoroutine(_musicFadeCoroutine);

            _musicFadeCoroutine = StartCoroutine(FadeOutMusic());
        }

        // ── SFX Pool ──────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the first idle AudioSource from the pool.
        /// If all sources are busy, allocates a new one (pool grows on demand).
        /// </summary>
        private AudioSource GetOrCreateSfxSource()
        {
            foreach (AudioSource source in _sfxPool)
            {
                if (!source.isPlaying)
                    return source;
            }

            return CreatePooledSource();
        }

        private AudioSource CreatePooledSource()
        {
            Transform root = _sfxPoolRoot != null ? _sfxPoolRoot : transform;
            var go = new GameObject($"SFX_Pool_{_sfxPool.Count:00}");
            go.transform.SetParent(root);
            go.transform.localPosition = Vector3.zero;

            var source = go.AddComponent<AudioSource>();
            _sfxPool.Add(source);
            _sfxBaseVolumes[source] = 1f;
            return source;
        }

        private void ConfigureSfxSource(AudioSource source, AudioClipConfig config)
        {
            _sfxBaseVolumes[source] = config.Volume;

            source.clip         = config.Clip;
            source.volume       = config.Volume * _sfxVolume;
            source.pitch        = config.Pitch;
            source.loop         = config.Loop;
            source.spatialBlend = config.SpatialBlend;
            source.minDistance  = config.MinDistance;
            source.maxDistance  = config.MaxDistance;
        }

        /// <summary>
        /// Re-applies the current SFX master volume to all active pool sources,
        /// preserving each clip's individual base volume.
        /// </summary>
        private void ApplySfxVolume()
        {
            foreach (AudioSource source in _sfxPool)
            {
                if (_sfxBaseVolumes.TryGetValue(source, out float baseVol))
                    source.volume = baseVol * _sfxVolume;
            }
        }

        // ── Music Fades ───────────────────────────────────────────────────────

        private IEnumerator CrossFadeMusic(AudioClipConfig config)
        {
            // Fade out the current track
            if (_musicSource.isPlaying && _fadeDuration > 0f)
            {
                float startVol = _musicSource.volume;
                for (float t = 0f; t < _fadeDuration; t += Time.unscaledDeltaTime)
                {
                    _musicSource.volume = Mathf.Lerp(startVol, 0f, t / _fadeDuration);
                    yield return null;
                }
            }

            _musicSource.Stop();
            _musicSource.clip  = config.Clip;
            _musicSource.loop  = config.Loop;
            _musicSource.pitch = config.Pitch;
            _musicSource.Play();

            // Fade in the new track
            float targetVolume = config.Volume * _musicVolume;
            if (_fadeDuration > 0f)
            {
                _musicSource.volume = 0f;
                for (float t = 0f; t < _fadeDuration; t += Time.unscaledDeltaTime)
                {
                    _musicSource.volume = Mathf.Lerp(0f, targetVolume, t / _fadeDuration);
                    yield return null;
                }
            }

            _musicSource.volume = targetVolume;
        }

        private IEnumerator FadeOutMusic()
        {
            if (_musicSource.isPlaying && _fadeDuration > 0f)
            {
                float startVol = _musicSource.volume;
                for (float t = 0f; t < _fadeDuration; t += Time.unscaledDeltaTime)
                {
                    _musicSource.volume = Mathf.Lerp(startVol, 0f, t / _fadeDuration);
                    yield return null;
                }
            }

            _musicSource.Stop();
            _musicSource.volume = _musicVolume;
        }

        // ── Persistence ───────────────────────────────────────────────────────

        private void LoadVolumes()
        {
            _sfxVolume   = PlayerPrefs.GetFloat(PrefsSfxKey,   1f);
            _musicVolume = PlayerPrefs.GetFloat(PrefsMusicKey, 1f);

            if (_musicSource != null)
                _musicSource.volume = _musicVolume;
        }

        // ── Editor helpers ────────────────────────────────────────────────────

        [ContextMenu("Debug – Reset Volumes to 1")]
        private void EditorResetVolumes()
        {
            SfxVolume   = 1f;
            MusicVolume = 1f;
            Debug.Log($"[{nameof(AudioService)}] Volumes reset to 1.");
        }
    }
}
