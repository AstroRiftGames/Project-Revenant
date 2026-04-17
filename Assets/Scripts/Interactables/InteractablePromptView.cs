using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class InteractablePromptView : MonoBehaviour
{
    [SerializeField] private MonoBehaviour _availabilitySourceComponent;
    [SerializeField] private GameObject _promptRoot;
    [SerializeField] private TMP_Text _promptText;
    [SerializeField] private string _promptMessage = "Interact";

    private readonly List<MonoBehaviour> _componentBuffer = new();
    private IInteractionAvailabilitySource _availabilitySource;

    private void Awake()
    {
        if (_promptRoot == null && _promptText != null)
            _promptRoot = _promptText.gameObject;

        ResolveAvailabilitySource();
        ApplyPromptText();
        RefreshVisibility();
    }

    private void OnEnable()
    {
        if (_availabilitySource == null)
            ResolveAvailabilitySource();

        if (_availabilitySource != null)
            _availabilitySource.OnInteractionAvailabilityChanged += HandleInteractionAvailabilityChanged;

        RefreshVisibility();
    }

    private void OnDisable()
    {
        if (_availabilitySource != null)
            _availabilitySource.OnInteractionAvailabilityChanged -= HandleInteractionAvailabilityChanged;
    }

    private void OnValidate()
    {
        if (_availabilitySourceComponent != null &&
            _availabilitySourceComponent is not IInteractionAvailabilitySource)
        {
            _availabilitySourceComponent = null;
        }

        if (_promptRoot == null && _promptText != null)
            _promptRoot = _promptText.gameObject;

        ApplyPromptText();
    }

    private void HandleInteractionAvailabilityChanged(bool _)
    {
        RefreshVisibility();
    }

    private void ApplyPromptText()
    {
        if (_promptText != null)
            _promptText.text = _promptMessage;
    }

    private void ResolveAvailabilitySource()
    {
        _availabilitySource = null;

        if (_availabilitySourceComponent != null)
        {
            _availabilitySource = _availabilitySourceComponent as IInteractionAvailabilitySource;
            if (_availabilitySource != null)
                return;
        }

        _componentBuffer.Clear();
        GetComponents(_componentBuffer);

        for (int i = 0; i < _componentBuffer.Count; i++)
        {
            if (_componentBuffer[i] is IInteractionAvailabilitySource availabilitySource)
            {
                _availabilitySource = availabilitySource;
                _availabilitySourceComponent = _componentBuffer[i];
                return;
            }
        }
    }

    private void RefreshVisibility()
    {
        if (_promptRoot == null)
            return;

        bool shouldShow = _availabilitySource != null && _availabilitySource.IsInteractionAvailable;
        if (_promptRoot.activeSelf != shouldShow)
            _promptRoot.SetActive(shouldShow);
    }
}
