using Core.Audio;
using Core.Audio.Data;
using System;
using UnityEngine;
using UnityEngine.UI;

public class PauseButton : MonoBehaviour
{
    private Button _button;
    public Action OnButtonPressed;

    private void Awake()
    {
        TryGetComponent(out Button button);
        _button = button;
    }
    private void OnEnable()
    {
        if (_button == null)
            return;

        if (HasPersistentOnPressedListener())
            return;

        _button.onClick.AddListener(OnPressed);
    }

    private void OnDisable()
    {
        if (_button == null)
            return;

        if (HasPersistentOnPressedListener())
            return;

        _button.onClick.RemoveListener(OnPressed);
    }

    public void OnPressed()
    {
        UIManager.Instance.AudioClipSet.TryGetClip("Button Press", out AudioClipConfig clip);
        AudioService.Instance.PlaySFX(clip);

        OnButtonPressed?.Invoke();
    }

    private bool HasPersistentOnPressedListener()
    {
        if (_button == null)
            return false;

        int eventCount = _button.onClick.GetPersistentEventCount();
        for (int i = 0; i < eventCount; i++)
        {
            if (_button.onClick.GetPersistentTarget(i) == this &&
                _button.onClick.GetPersistentMethodName(i) == nameof(OnPressed))
            {
                return true;
            }
        }

        return false;
    }
}
