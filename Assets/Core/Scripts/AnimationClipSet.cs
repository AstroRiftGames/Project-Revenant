using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "AnimationClipSet",
    menuName = "Dungeon's Game/Animation Clip Set")]
public class AnimationClipSet : ScriptableObject
{
    [SerializeField]
    private List<AnimationClipEntry> _clips = new();

    private Dictionary<string, AnimationClip> _cache;

    public Dictionary<string, AnimationClip> Clips
    {
        get
        {
            if (_cache == null)
            {
                _cache = new Dictionary<string, AnimationClip>();

                foreach (var clip in _clips)
                {
                    if (!_cache.ContainsKey(clip.clipKey))
                    {
                        _cache.Add(
                            clip.clipKey,
                            clip.animationClip);
                    }
                }
            }

            return _cache;
        }
    }
}

[Serializable]
public class AnimationClipEntry
{
    public string clipKey;
    public AnimationClip animationClip;
}