using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ChestInteractionController))]
[RequireComponent(typeof(ChestState))]
public class ChestInteractionPromptView : MonoBehaviour
{
    [SerializeField] private ChestInteractionController _interaction;
    [SerializeField] private ChestState _state;
    [SerializeField] private GameObject _promptRoot;
    [SerializeField] private TMP_Text _promptText;
    [SerializeField] private string _promptMessage = "Interact";

    private void Awake()
    {
        _interaction ??= GetComponent<ChestInteractionController>();
        _state ??= GetComponent<ChestState>();

        if (_promptRoot == null && _promptText != null)
            _promptRoot = _promptText.gameObject;

        ApplyPromptText();
        RefreshVisibility();
    }

    private void OnEnable()
    {
        if (_interaction != null)
            _interaction.OnInteractionAvailabilityChanged += HandleInteractionAvailabilityChanged;

        if (_state != null)
            _state.OnOpenedStateChanged += HandleOpenedStateChanged;

        RefreshVisibility();
    }

    private void OnDisable()
    {
        if (_interaction != null)
            _interaction.OnInteractionAvailabilityChanged -= HandleInteractionAvailabilityChanged;

        if (_state != null)
            _state.OnOpenedStateChanged -= HandleOpenedStateChanged;
    }

    private void HandleInteractionAvailabilityChanged(bool _)
    {
        RefreshVisibility();
    }

    private void HandleOpenedStateChanged(bool _)
    {
        RefreshVisibility();
    }

    private void ApplyPromptText()
    {
        if (_promptText != null)
            _promptText.text = _promptMessage;
    }

    private void RefreshVisibility()
    {
        if (_promptRoot == null)
            return;

        bool shouldShow = _interaction != null &&
                          _state != null &&
                          !_state.IsOpened &&
                          _interaction.IsInteractionAvailable;

        if (_promptRoot.activeSelf != shouldShow)
            _promptRoot.SetActive(shouldShow);
    }
}
