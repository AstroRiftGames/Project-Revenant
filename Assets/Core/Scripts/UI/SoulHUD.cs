using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Persistent HUD element showing the player's current Soul count.
/// Spawn point for SoulDeltaText popups when souls are gained or spent.
/// Requires a SoulBank reference via SoulContext.Current.SoulBank or direct assignment.
/// </summary>
public class SoulHUD : MonoBehaviour
{
    public static SoulHUD Instance { get; private set; }

    [Header("Visibility")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _showDuration = 3f;
    [SerializeField] private float _fadeDuration = 0.5f;

    [Header("Counter")]
    [SerializeField] private TextMeshProUGUI _counterLabel;

    [Header("Delta Popup")]
    [SerializeField] private SoulDeltaText _deltaTextPrefab;
    /// <summary>World-space or Canvas-space anchor where delta popups spawn.</summary>
    [SerializeField] private RectTransform _deltaSpawnPoint;

    private SoulBank _bank;
    private Coroutine _hideCoroutine;
    private bool _isForcedVisible = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(this);

        if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup != null) _canvasGroup.alpha = 0f;
    }

    private void OnEnable()
    {
        if (SoulContext.Current != null)
            Bind(SoulContext.Current.SoulBank);
    }

    private void Start()
    {
        if (_bank != null) return; // Already bound from OnEnable.

        if (SoulContext.Current != null)
            Bind(SoulContext.Current.SoulBank);
    }

    private void OnDisable()
    {
        Unbind();
    }

    /// <summary>Late-bind to a bank (e.g. after a scene transition sets up SoulContext).</summary>
    public void Bind(SoulBank bank)
    {
        Unbind();
        _bank = bank;
        if (_bank == null)
        {
            return;
        }

        _bank.OnSoulsChanged += HandleSoulsChanged;
        RefreshCounter(_bank.StoredSouls);
    }

    private void Unbind()
    {
        if (_bank != null)
        {
            _bank.OnSoulsChanged -= HandleSoulsChanged;
            _bank = null;
        }
    }

    private void HandleSoulsChanged(int newTotal, int delta)
    {
        RefreshCounter(newTotal);
        SpawnDelta(delta);
        ShowTemporarily();
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

    private void ShowTemporarily()
    {
        if (_isForcedVisible || _canvasGroup == null) return;

        if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
        _hideCoroutine = StartCoroutine(ShowAndFadeRoutine());
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

    private void RefreshCounter(int total)
    {
        if (_counterLabel != null)
            _counterLabel.text = total.ToString();
    }

    private void SpawnDelta(int delta)
    {
        if (_deltaTextPrefab == null)
        {
            return;
        }
        if (_deltaSpawnPoint == null)
        {
            return;
        }

        SoulDeltaText popup = Instantiate(_deltaTextPrefab, _deltaSpawnPoint.parent);
        popup.transform.SetParent(_deltaSpawnPoint.transform, false);
        popup.Play(delta);
    }
}
