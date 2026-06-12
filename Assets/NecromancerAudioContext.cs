using Core.Audio;
using Core.Audio.Data;
using UnityEngine;

public class NecromancerAudioContext : MonoBehaviour
{
    [SerializeField] AudioClipSet AudioClipSet;
    public void PlayFootstepSFX()
    {
        AudioClipSet.TryGetClip("Footstep", out AudioClipConfig clip);
        AudioService.Instance.PlaySFX(clip, transform.position);
    }
}
