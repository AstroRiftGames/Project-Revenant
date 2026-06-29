using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI controller for the timescale widget. Wire up buttons and a text label
/// in the inspector — the script handles the rest via <see cref="TimeScaleController"/>.
/// </summary>
public class TimeScaleUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _speedLabel;
    [SerializeField] private Button _increaseButton;
    [SerializeField] private Button _decreaseButton;

    private void OnEnable()
    {
        if (_increaseButton != null)
            _increaseButton.onClick.AddListener(OnIncreaseClicked);

        if (_decreaseButton != null)
            _decreaseButton.onClick.AddListener(OnDecreaseClicked);

        TimeScaleController.OnSpeedChanged += HandleSpeedChanged;

        RefreshDisplay();
    }

    private void OnDisable()
    {
        if (_increaseButton != null)
            _increaseButton.onClick.RemoveListener(OnIncreaseClicked);

        if (_decreaseButton != null)
            _decreaseButton.onClick.RemoveListener(OnDecreaseClicked);

        TimeScaleController.OnSpeedChanged -= HandleSpeedChanged;
    }

    private void OnIncreaseClicked()
    {
        if (TimeScaleController.Instance != null)
            TimeScaleController.Instance.IncreaseSpeed();
    }

    private void OnDecreaseClicked()
    {
        if (TimeScaleController.Instance != null)
            TimeScaleController.Instance.DecreaseSpeed();
    }

    private void HandleSpeedChanged(float newSpeed)
    {
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (TimeScaleController.Instance == null)
            return;

        if (_speedLabel != null)
            _speedLabel.text = TimeScaleController.Instance.GetSpeedLabel();

        UpdateButtonInteractability();
    }

    private void UpdateButtonInteractability()
    {
        if (TimeScaleController.Instance == null)
            return;

        int currentIndex = TimeScaleController.Instance.CurrentStepIndex;
        int maxIndex = TimeScaleController.Instance.StepCount - 1;

        if (_decreaseButton != null)
            _decreaseButton.interactable = currentIndex > 0;

        if (_increaseButton != null)
            _increaseButton.interactable = currentIndex < maxIndex;
    }
}
