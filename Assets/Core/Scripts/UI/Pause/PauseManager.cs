using System;
using UnityEngine;

public class PauseManager : MonoBehaviour
{
    [SerializeField] GameObject MainPanel;
    [SerializeField] GameObject SettingsPanel;
    [SerializeField] private PauseButton _pauseButton;
    [SerializeField] private PauseButton _resumeButton;

    public static Action OnPauseRequested;
    public static Action OnResumeRequested;

    private void OnEnable()
    {
        _pauseButton.OnButtonPressed += Pause;
        _resumeButton.OnButtonPressed += Resume;
    }

    private void OnDisable()
    {
        _pauseButton.OnButtonPressed -= Pause;
        _resumeButton.OnButtonPressed -= Resume;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameManager.Instance.StateManager.CurrentState == GameState.Paused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    private void Pause()
    {
        MainPanel.SetActive(true);
        OnPauseRequested?.Invoke();
    }

    private void Resume()
    {
        MainPanel.SetActive(false);
        OnResumeRequested?.Invoke();
    }
}
