#if UNITY_EDITOR
using System.Collections.Generic;
using Core.Audio.Data;
using UnityEditor;
using UnityEngine;

namespace Core.Audio.Editor
{
    /// <summary>
    /// Runtime audio balancer. Open via: Window → Audio → Balancer.
    ///
    /// In Play Mode: adjust master channel volumes and tweak individual clip
    /// volume/pitch in real time. "Apply to Asset" persists changes to the SO on disk.
    ///
    /// In Edit Mode: browse and preview every AudioClipConfig in the project.
    /// </summary>
    public class AudioBalancerWindow : EditorWindow
    {
        private Vector2      _scroll;
        private AudioClipConfig[] _configs;
        private AudioSource  _previewSource;

        [MenuItem("Window/Audio/Balancer")]
        public static void Open() => GetWindow<AudioBalancerWindow>("Audio Balancer");

        private void OnEnable()  => RefreshConfigs();

        private void OnDisable()
        {
            DestroyPreviewSource();
        }

        private void OnGUI()
        {
            DrawChannelMasterSection();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Clip Library", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh", GUILayout.Width(80)))
                    RefreshConfigs();

                EditorGUILayout.LabelField(
                    _configs != null ? $"{_configs.Length} configs found" : "—",
                    EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(4);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (_configs != null)
            {
                foreach (AudioClipConfig config in _configs)
                    DrawConfigRow(config);
            }
            EditorGUILayout.EndScrollView();
        }

        // ── Channel master controls ───────────────────────────────────────────

        private void DrawChannelMasterSection()
        {
            EditorGUILayout.LabelField("Channel Master Volume", EditorStyles.boldLabel);

            bool liveService = Application.isPlaying && AudioService.Instance != null;

            using (new EditorGUI.DisabledScope(!liveService))
            {
                float sfxCurrent = liveService ? AudioService.Instance.SfxVolume : 1f;
                float sfxNew = EditorGUILayout.Slider("SFX", sfxCurrent, 0f, 1f);
                if (liveService && !Mathf.Approximately(sfxNew, sfxCurrent))
                    AudioService.Instance.SfxVolume = sfxNew;

                float musicCurrent = liveService ? AudioService.Instance.MusicVolume : 1f;
                float musicNew = EditorGUILayout.Slider("Music", musicCurrent, 0f, 1f);
                if (liveService && !Mathf.Approximately(musicNew, musicCurrent))
                    AudioService.Instance.MusicVolume = musicNew;
            }

            if (!liveService)
                EditorGUILayout.HelpBox("Enter Play Mode to control live channel volumes.", MessageType.Info);
        }

        // ── Per-clip rows ─────────────────────────────────────────────────────

        private void DrawConfigRow(AudioClipConfig config)
        {
            if (config == null) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(config.name, EditorStyles.boldLabel);

                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                        Selection.activeObject = config;

                    if (GUILayout.Button("▶ Preview", GUILayout.Width(72)))
                        PreviewClip(config);

                    if (GUILayout.Button("■ Stop", GUILayout.Width(52)))
                        StopPreview();
                }

                SerializedObject so = new SerializedObject(config);
                so.Update();

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(so.FindProperty("_volume"), new GUIContent("Volume"));
                EditorGUILayout.PropertyField(so.FindProperty("_pitch"),  new GUIContent("Pitch"));
                bool dirty = EditorGUI.EndChangeCheck();

                if (dirty)
                    so.ApplyModifiedProperties();

                if (dirty && GUILayout.Button("Apply to Asset"))
                    EditorUtility.SetDirty(config);
            }
        }

        // ── Preview AudioSource ───────────────────────────────────────────────

        private void PreviewClip(AudioClipConfig config)
        {
            if (config.Clip == null) return;

            EnsurePreviewSource();
            _previewSource.clip         = config.Clip;
            _previewSource.volume       = config.Volume;
            _previewSource.pitch        = config.Pitch;
            _previewSource.spatialBlend = 0f;
            _previewSource.loop         = false;
            _previewSource.Play();
        }

        private void StopPreview()
        {
            if (_previewSource != null && _previewSource.isPlaying)
                _previewSource.Stop();
        }

        private void EnsurePreviewSource()
        {
            if (_previewSource != null) return;

            var go = EditorUtility.CreateGameObjectWithHideFlags(
                "AudioBalancer_Preview", HideFlags.HideAndDontSave);
            _previewSource = go.AddComponent<AudioSource>();
        }

        private void DestroyPreviewSource()
        {
            if (_previewSource != null)
                DestroyImmediate(_previewSource.gameObject);
        }

        // ── Asset discovery ───────────────────────────────────────────────────

        private void RefreshConfigs()
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioClipConfig");
            var list = new List<AudioClipConfig>(guids.Length);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var config  = AssetDatabase.LoadAssetAtPath<AudioClipConfig>(path);
                if (config != null) list.Add(config);
            }

            _configs = list.ToArray();
        }
    }
}
#endif
