using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ChestState))]
public class ChestOpenedVisualView : MonoBehaviour
{
    [SerializeField] private ChestState _state;
    [SerializeField] private SpriteRenderer _targetRenderer;
    [SerializeField] private Color _closedColor = Color.white;
    [SerializeField] private Color _openedColor = new(0.55f, 0.55f, 0.55f, 1f);

    private void Awake()
    {
        _state ??= GetComponent<ChestState>();
        _targetRenderer ??= GetComponentInChildren<SpriteRenderer>();
        RefreshVisual();
    }

    private void OnEnable()
    {
        if (_state != null)
            _state.OnOpenedStateChanged += HandleOpenedStateChanged;

        RefreshVisual();
    }

    private void OnDisable()
    {
        if (_state != null)
            _state.OnOpenedStateChanged -= HandleOpenedStateChanged;
    }

    private void HandleOpenedStateChanged(bool _)
    {
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (_targetRenderer == null || _state == null)
            return;

        _targetRenderer.color = _state.IsOpened ? _openedColor : _closedColor;
    }
}
