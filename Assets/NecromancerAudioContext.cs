using Core.Audio;
using Core.Audio.Data;
using UnityEngine;

public class NecromancerAudioContext : MonoBehaviour
{
    [SerializeField] AudioClipSet AudioClipSet;
    public void PlayFootstepSFX()
    {
        AudioService.TryPlayClipFromSet(AudioClipSet, "Footstep", transform.position);
    }
}
