using System;
using UnityEngine;

public class PauseManager : MonoBehaviour
{
    [SerializeField] GameObject Dimmer;
    [SerializeField] GameObject MainPanel;
    [SerializeField] GameObject SettingsPanel;
    [SerializeField] GameObject QuitConfirmationPanel;
    [SerializeField] private PauseButton _pauseButton;
    [SerializeField] private PauseButton _resumeButton;
    [SerializeField] private PauseButton _settingsButton;
    [SerializeField] private PauseButton _closeSettingsButton;
    [SerializeField] private PauseButton _quitButton;
    [SerializeField] private PauseButton _cancelQuitButton;
    [SerializeField] private PauseButton _confirmQuitButton;

    public static Action OnPauseRequested;
    public static Action OnResumeRequested;
    public static Action OnQuitRequested;

    private void OnEnable()
    {
        _pauseButton.OnButtonPressed += Pause;
        _resumeButton.OnButtonPressed += Resume;
        _settingsButton.OnButtonPressed += OpenCloseSettings;
        _closeSettingsButton.OnButtonPressed += OpenCloseSettings;
        _quitButton.OnButtonPressed += Quit;
        _cancelQuitButton.OnButtonPressed += CancelQuit;
        _confirmQuitButton.OnButtonPressed += ConfirmQuit;

    }

    private void OnDisable()
    {
        _pauseButton.OnButtonPressed -= Pause;
        _resumeButton.OnButtonPressed -= Resume;
        _quitButton.OnButtonPressed -= Quit;
        _cancelQuitButton.OnButtonPressed -= CancelQuit;
        _confirmQuitButton.OnButtonPressed -= ConfirmQuit;
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
        Dimmer.SetActive(true);
        MainPanel.SetActive(true);
        OnPauseRequested?.Invoke();
    }

    private void Resume()
    {
        Dimmer.SetActive(false);
        MainPanel.SetActive(false);
        SettingsPanel.SetActive(false);
        QuitConfirmationPanel.SetActive(false);
        OnResumeRequested?.Invoke();
    }

    private void OpenCloseSettings()
    {
        SettingsPanel.SetActive(!SettingsPanel.activeSelf);
    }

    private void Quit()
    {
        QuitConfirmationPanel.SetActive(true);
    }

    private void CancelQuit()
    {
        QuitConfirmationPanel.SetActive(false);
    }

    private void ConfirmQuit()
    {
        OnQuitRequested?.Invoke();
    }
}
