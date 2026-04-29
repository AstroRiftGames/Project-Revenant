using UnityEngine;

/// <summary>
/// UI popup for health changes, inheriting base behaviour but adding a random rotation.
/// </summary>
public class HealthDeltaText : BaseFeedbackText
{
    private RectTransform _rectTransform;
    [Header("Health Feedback Settings")]
    [SerializeField] private float _minRotationZ = -15f;
    [SerializeField] private float _maxRotationZ = 15f;

    protected override void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        base.Awake();
    }

    public override void Play(int delta)
    {
        ApplyRandomRotation();
        _rectTransform.anchoredPosition = Vector2.zero;
        base.Play(delta);
    }

    private void ApplyRandomRotation()
    {
        float randomZ = Random.Range(_minRotationZ, _maxRotationZ);
        transform.localEulerAngles = new Vector3(0, 0, randomZ);
    }
}
