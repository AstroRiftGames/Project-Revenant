using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public class SkillTextPopup : MonoBehaviour
{
    [SerializeField] private TMP_Text _label;
    [SerializeField] private float _lifetime = 0.7f;
    [SerializeField] private bool _debugLogs;

    private Color _baseColor;
    private Vector3 _startLocalPosition;
    private float _elapsed;
    private RectTransform _rectTransform;

    private void Awake()
    {
        _label ??= GetComponentInChildren<TMP_Text>(includeInactive: true);
        _rectTransform = transform as RectTransform;
        _startLocalPosition = transform.localPosition;
    }

    public void Initialize(string message, Color color)
    {
        _label ??= GetComponentInChildren<TMP_Text>(includeInactive: true);
        _rectTransform ??= transform as RectTransform;
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
            $"fontSize {(_label != null ? _label.fontSize : 0f):F2}.");
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;

        if (_rectTransform != null)
            _rectTransform.localPosition = _startLocalPosition;
        else
            transform.localPosition = _startLocalPosition;

        if (_elapsed >= _lifetime)
            Destroy(gameObject);
    }

    private void LogDebug(string message)
    {
        if (_debugLogs)
            Debug.Log(message, this);
    }
}
