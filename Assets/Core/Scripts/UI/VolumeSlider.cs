using Core.Audio;
using UnityEngine;
using UnityEngine.UI;

public enum VolumeType
{
    Master,
    Music,
    SFX
}
public class VolumeSlider : MonoBehaviour
{
    [SerializeField] private VolumeType _volumeType;
    Slider _slider;

    private void Awake()
    {
        TryGetComponent(out Slider slider);
        _slider = slider;
        SetVolume();
    }

    private void OnEnable()
    {
        _slider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    private void OnDisable()
    {
        _slider.onValueChanged.RemoveListener(OnSliderValueChanged);
    }

    private void SetVolume()
    {
        _slider.value = _volumeType switch
        {
            VolumeType.Master => AudioService.Instance.MasterVolume,
            VolumeType.Music => AudioService.Instance.MusicVolume,
            VolumeType.SFX => AudioService.Instance.SfxVolume,
            _ => _slider.value
        };
    }

    private void OnSliderValueChanged(float volume)
    {
        switch (_volumeType)
        {
            case VolumeType.Master:
                AudioService.Instance.MasterVolume = volume; break;
            case VolumeType.Music:
                AudioService.Instance.MusicVolume = volume; break;
            case VolumeType.SFX:
                AudioService.Instance.SfxVolume = volume; break;
        }
        
    }
}
