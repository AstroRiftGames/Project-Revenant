using System;
using UnityEngine;

/// <summary>
/// Manages <see cref="Time.timeScale"/> with configurable speed steps.
/// Works alongside the existing pause/resume system — when the game pauses,
/// timeScale goes to 0; on resume it restores to the current speed step.
/// </summary>
public class TimeScaleController : MonoBehaviour
{
    public static TimeScaleController Instance { get; private set; }

    [Header("Speed Steps")]
    [Tooltip("Ordered list of speed multipliers the player can cycle through.")]
    [SerializeField] private float[] _speedSteps = { 0.5f, 1f, 1.5f, 2f, 2.5f, 3f };

    [Tooltip("Index into _speedSteps used on startup. Defaults to 1x.")]
    [SerializeField] private int _defaultStepIndex = 1;

    private int _currentStepIndex;
    private bool _isPaused;

    /// <summary>Current speed multiplier (e.g. 1, 2, 2.5).</summary>
    public float CurrentSpeed => _speedSteps[_currentStepIndex];

    /// <summary>Current step index into the speed array.</summary>
    public int CurrentStepIndex => _currentStepIndex;

    /// <summary>Total number of available speed steps.</summary>
    public int StepCount => _speedSteps.Length;

    /// <summary>Raised whenever the speed multiplier changes. Arg = new speed value.</summary>
    public static event Action<float> OnSpeedChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        _currentStepIndex = Mathf.Clamp(_defaultStepIndex, 0, _speedSteps.Length - 1);
    }

    private void OnEnable()
    {
        PauseManager.OnPauseRequested += HandlePauseRequested;
        PauseManager.OnResumeRequested += HandleResumeRequested;
    }

    private void OnDisable()
    {
        PauseManager.OnPauseRequested -= HandlePauseRequested;
        PauseManager.OnResumeRequested -= HandleResumeRequested;
    }

    /// <summary>
    /// Move one step faster. Clamps at the maximum speed.
    /// </summary>
    public void IncreaseSpeed()
    {
        if (_currentStepIndex >= _speedSteps.Length - 1)
            return;

        _currentStepIndex++;
        ApplyTimeScale();
        OnSpeedChanged?.Invoke(CurrentSpeed);
    }

    /// <summary>
    /// Move one step slower. Clamps at the minimum speed.
    /// </summary>
    public void DecreaseSpeed()
    {
        if (_currentStepIndex <= 0)
            return;

        _currentStepIndex--;
        ApplyTimeScale();
        OnSpeedChanged?.Invoke(CurrentSpeed);
    }

    /// <summary>
    /// Jump directly to a specific step index.
    /// </summary>
    public void SetStepIndex(int index)
    {
        index = Mathf.Clamp(index, 0, _speedSteps.Length - 1);
        if (index == _currentStepIndex)
            return;

        _currentStepIndex = index;
        ApplyTimeScale();
        OnSpeedChanged?.Invoke(CurrentSpeed);
    }

    /// <summary>
    /// Reset speed back to the default (1x) step.
    /// </summary>
    public void ResetToDefault()
    {
        SetStepIndex(_defaultStepIndex);
    }

    /// <summary>
    /// Returns the current speed formatted for display (e.g. "1x", "2.5x").
    /// </summary>
    public string GetSpeedLabel()
    {
        return FormatSpeedLabel(CurrentSpeed);
    }

    /// <summary>
    /// Returns the speed at a given step index formatted for display.
    /// </summary>
    public string GetSpeedLabel(int stepIndex)
    {
        stepIndex = Mathf.Clamp(stepIndex, 0, _speedSteps.Length - 1);
        return FormatSpeedLabel(_speedSteps[stepIndex]);
    }

    private void HandlePauseRequested()
    {
        _isPaused = true;
        // Time.timeScale is set to 0 by GameManager.RequestPause — no need to touch it here.
    }

    private void HandleResumeRequested()
    {
        _isPaused = false;
        // GameManager.RequestResume will set timeScale to 1, but we override to our speed.
        // We apply on a slight delay so our value wins over GameManager's default restore.
        ApplyTimeScale();
    }

    private void ApplyTimeScale()
    {
        if (_isPaused)
            return;

        Time.timeScale = CurrentSpeed;
    }

    private static string FormatSpeedLabel(float speed)
    {
        // Use whole-number format when there are no decimals, fractional otherwise.
        return speed % 1f == 0f
            ? $"{speed:0}x"
            : $"{speed:0.#}x";
    }
}
