using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Base class for HUD elements that show a currency or resource (Souls, Mana, etc.).
/// Handles visibility fading and delta popup spawning.
/// </summary>
public abstract class BaseCurrencyCounter : MonoBehaviour
{
    [Header("Visibility")]
    [SerializeField] protected CanvasGroup _canvasGroup;
    [SerializeField] protected float _showDuration = 3f;
    [SerializeField] protected float _fadeDuration = 0.5f;

    [Header("Counter")]
    [SerializeField] protected TextMeshProUGUI _counterLabel;

    [Header("Delta Popup")]
    [SerializeField] protected BaseFeedbackText _deltaTextPrefab;
    [Tooltip("World-space or Canvas-space anchor where delta popups spawn.")]
    [SerializeField] protected RectTransform _deltaSpawnPoint;

    protected Coroutine _hideCoroutine;
    protected bool _isForcedVisible = false;

    protected virtual void Awake()
    {
        if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup != null) _canvasGroup.alpha = 0f;
    }

    public void SetForceVisible(bool force)
    {
        _isForcedVisible = force;
        if (_isForcedVisible)
        {
            if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;
        }
        else
        {
            ShowTemporarily();
        }
    }

    protected void ShowTemporarily()
    {
        if (_isForcedVisible || _canvasGroup == null) return;

        if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
        
        if (gameObject.activeInHierarchy)
        {
            _hideCoroutine = StartCoroutine(ShowAndFadeRoutine());
        }
    }

    private IEnumerator ShowAndFadeRoutine()
    {
        _canvasGroup.alpha = 1f;
        yield return new WaitForSeconds(_showDuration);

        float timer = 0f;
        while (timer < _fadeDuration)
        {
            timer += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / _fadeDuration);
            yield return null;
        }
        _canvasGroup.alpha = 0f;
    }

    protected void RefreshCounter(int total)
    {
        if (_counterLabel != null)
            _counterLabel.text = total.ToString();
    }

    protected void SpawnDelta(int delta)
    {
        if (_deltaTextPrefab == null || _deltaSpawnPoint == null)
        {
            return;
        }

        BaseFeedbackText popup = Instantiate(_deltaTextPrefab, _deltaSpawnPoint.parent);
        popup.transform.SetParent(_deltaSpawnPoint.transform, false);
        popup.Play(delta);
    }
}
