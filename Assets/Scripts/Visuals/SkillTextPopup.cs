using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public class SkillTextPopup : MonoBehaviour
{
    [SerializeField] private TMP_Text _label;
    [SerializeField] private float _lifetime = 0.7f;
    [SerializeField] private float _riseDistance = 0.45f;
    [SerializeField] private bool _debugLogs;

    private Color _baseColor;
    private Vector3 _startLocalPosition;
    private Camera _mainCamera;
    private float _elapsed;
    private RectTransform _rectTransform;

    private void Awake()
    {
        _label ??= GetComponentInChildren<TMP_Text>(includeInactive: true);
        _rectTransform = transform as RectTransform;
        _mainCamera = Camera.main;
        _startLocalPosition = transform.localPosition;
    }

    public void Initialize(string message, Color color)
    {
        _label ??= GetComponentInChildren<TMP_Text>(includeInactive: true);
        _rectTransform ??= transform as RectTransform;
        _mainCamera ??= Camera.main;
        _startLocalPosition = transform.localPosition;
        _elapsed = 0f;
        _baseColor = color;
        if (_label != null)
        {
            _label.text = message;
            _label.color = color;
        }

        LogDebug(
            $"[SkillTextPopup] '{name}' initialized at world {transform.position} local {_startLocalPosition} scale {transform.localScale} " +
            $"fontSize {(_label != null ? _label.fontSize : 0f):F2} camera {(_mainCamera != null ? _mainCamera.name : "None")}.");
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        float normalizedLifetime = _lifetime > 0f ? Mathf.Clamp01(_elapsed / _lifetime) : 1f;

        Vector3 localOffset = Vector3.up * (_riseDistance * normalizedLifetime);
        if (_rectTransform != null)
            _rectTransform.localPosition = _startLocalPosition + localOffset;
        else
            transform.localPosition = _startLocalPosition + localOffset;

        FaceCamera();
        UpdateAlpha(normalizedLifetime);

        if (_elapsed >= _lifetime)
            Destroy(gameObject);
    }

    private void FaceCamera()
    {
        if (_mainCamera == null)
            _mainCamera = Camera.main;

        if (_mainCamera == null)
            return;

        Vector3 directionToCamera = transform.position - _mainCamera.transform.position;
        if (directionToCamera.sqrMagnitude <= 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(directionToCamera.normalized, Vector3.up);
    }

    private void UpdateAlpha(float normalizedLifetime)
    {
        if (_label == null)
            return;

        Color color = _baseColor;
        color.a *= 1f - normalizedLifetime;
        _label.color = color;
    }

    private void LogDebug(string message)
    {
        if (_debugLogs)
            Debug.Log(message, this);
    }
}
