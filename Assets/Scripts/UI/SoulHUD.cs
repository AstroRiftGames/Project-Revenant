using TMPro;
using UnityEngine;

/// <summary>
/// Persistent HUD element showing the player's current Soul count.
/// Spawn point for SoulDeltaText popups when souls are gained or spent.
/// Requires a SoulBank reference via SoulContext.Current.SoulBank or direct assignment.
/// </summary>
public class SoulHUD : MonoBehaviour
{
    [Header("Counter")]
    [SerializeField] private TextMeshProUGUI _counterLabel;

    [Header("Delta Popup")]
    [SerializeField] private SoulDeltaText _deltaTextPrefab;
    /// <summary>World-space or Canvas-space anchor where delta popups spawn.</summary>
    [SerializeField] private RectTransform _deltaSpawnPoint;

    private SoulBank _bank;

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
