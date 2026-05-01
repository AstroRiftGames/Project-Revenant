using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class StatusEffectPersistentPopup : MonoBehaviour
{
    [SerializeField] private TMP_Text _label;

    private RectTransform _rectTransform;
    private RectTransform _canvasRectTransform;
    private Canvas _parentCanvas;
    private Transform _target;
    private Vector3 _worldOffset;
    private Camera _worldCamera;

    private void Awake()
    {
        _rectTransform = transform as RectTransform;
        _label ??= GetComponentInChildren<TMP_Text>(includeInactive: true);
    }

    public void Initialize(Canvas parentCanvas, Transform target, Vector3 worldOffset)
    {
        _parentCanvas = parentCanvas;
        _canvasRectTransform = parentCanvas != null ? parentCanvas.transform as RectTransform : null;
        _target = target;
        _worldOffset = worldOffset;
        _worldCamera = ResolveWorldCamera(parentCanvas);
        _rectTransform ??= transform as RectTransform;
        _label ??= GetComponentInChildren<TMP_Text>(includeInactive: true);
        RefreshPosition();
    }

    public void SetText(string content)
    {
        if (_label != null)
        {
            _label.text = content;
            ForceOpaqueLabel();
        }
    }

    public void SetVisible(bool isVisible)
    {
        gameObject.SetActive(isVisible);
    }

    private void LateUpdate()
    {
        RefreshPosition();
    }

    private void RefreshPosition()
    {
        if (_target == null || _parentCanvas == null || _canvasRectTransform == null || _rectTransform == null)
            return;

        ForceOpaqueLabel();
        _worldCamera ??= ResolveWorldCamera(_parentCanvas);
        Vector3 worldPosition = _target.position + _worldOffset;
        Vector3 screenPoint = _worldCamera != null
            ? _worldCamera.WorldToScreenPoint(worldPosition)
            : RectTransformUtility.WorldToScreenPoint(null, worldPosition);

        bool isBehindCamera = _worldCamera != null && screenPoint.z < 0f;
        if (isBehindCamera) 
            return;

        Camera canvasCamera = _parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _parentCanvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRectTransform, screenPoint, canvasCamera, out Vector2 localPoint))
            _rectTransform.anchoredPosition = localPoint;
    }

    private void ForceOpaqueLabel()
    {
        if (_label == null)
            return;

        Color color = _label.color;
        if (color.a >= 0.999f)
            return;

        color.a = 1f;
        _label.color = color;
    }

    private static Camera ResolveWorldCamera(Canvas parentCanvas)
    {
        if (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceCamera && parentCanvas.worldCamera != null)
            return parentCanvas.worldCamera;

        return Camera.main;
    }
}
