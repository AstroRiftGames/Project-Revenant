using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteOutlineHighlightView))]
public class InteractableHighlightController : MonoBehaviour
{
    [SerializeField] private MonoBehaviour _availabilitySourceComponent;
    [SerializeField] private Collider2D[] _targetColliders;
    [SerializeField] private bool _autoCollectChildColliders = true;
    [SerializeField] private bool _includeInactiveChildren = true;
    [SerializeField] private Camera _worldCamera;
    [SerializeField] private SpriteOutlineHighlightView _highlightView;

    private readonly List<MonoBehaviour> _componentBuffer = new();
    private IInteractionAvailabilitySource _availabilitySource;
    private bool _isHovered;

    private void Reset()
    {
        CollectColliders();
    }

    private void Awake()
    {
        _highlightView ??= GetComponent<SpriteOutlineHighlightView>();
        EnsureCamera();
        EnsureColliders();
        ResolveAvailabilitySource();
        RefreshHoverState();
        RefreshHighlight();
    }

    private void OnEnable()
    {
        if (_availabilitySource != null)
            _availabilitySource.OnInteractionAvailabilityChanged += HandleInteractionAvailabilityChanged;

        RefreshHoverState();
        RefreshHighlight();
    }

    private void Update()
    {
        bool previousHovered = _isHovered;
        RefreshHoverState();

        if (previousHovered != _isHovered)
            RefreshHighlight();
    }

    private void OnDisable()
    {
        if (_availabilitySource != null)
            _availabilitySource.OnInteractionAvailabilityChanged -= HandleInteractionAvailabilityChanged;

        _isHovered = false;

        if (_highlightView != null)
            _highlightView.SetHighlighted(false);
    }

    private void OnValidate()
    {
        if (_availabilitySourceComponent != null && _availabilitySourceComponent is not IInteractionAvailabilitySource)
            _availabilitySourceComponent = null;

        if (!Application.isPlaying)
            EnsureColliders();
    }

    private void HandleInteractionAvailabilityChanged(bool _)
    {
        RefreshHighlight();
    }

    private void EnsureCamera()
    {
        if (_worldCamera == null)
            _worldCamera = Camera.main;
    }

    private void EnsureColliders()
    {
        if (_targetColliders == null || _targetColliders.Length == 0 || _autoCollectChildColliders)
            CollectColliders();
    }

    private void CollectColliders()
    {
        _targetColliders = GetComponentsInChildren<Collider2D>(_includeInactiveChildren);
    }

    private void ResolveAvailabilitySource()
    {
        _availabilitySource = null;

        if (_availabilitySourceComponent != null)
        {
            _availabilitySource = _availabilitySourceComponent as IInteractionAvailabilitySource;
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

    private void RefreshHoverState()
    {
        EnsureCamera();
        _isHovered = IsPointerOverAnyCollider();
    }

    private bool IsPointerOverAnyCollider()
    {
        if (_worldCamera == null || _targetColliders == null || _targetColliders.Length == 0)
            return false;

        Vector3 mouseWorldPosition = _worldCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 point = new(mouseWorldPosition.x, mouseWorldPosition.y);

        for (int i = 0; i < _targetColliders.Length; i++)
        {
            Collider2D targetCollider = _targetColliders[i];
            if (targetCollider == null || !targetCollider.enabled || !targetCollider.gameObject.activeInHierarchy)
                continue;

            if (targetCollider.OverlapPoint(point))
                return true;
        }

        return false;
    }

    private void RefreshHighlight()
    {
        if (_highlightView == null)
            return;

        bool shouldHighlight =
            _availabilitySource != null &&
            _availabilitySource.IsInteractionAvailable &&
            _isHovered;

        _highlightView.SetHighlighted(shouldHighlight);
    }
}
