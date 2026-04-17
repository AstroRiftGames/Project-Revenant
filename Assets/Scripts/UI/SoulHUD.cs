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
        Debug.Log($"[SoulHUD] OnEnable — SoulContext.Current: {(SoulContext.Current != null ? "found" : "NULL")}");
        if (SoulContext.Current != null)
            Bind(SoulContext.Current.SoulBank);
        // else: Start() will retry once all OnEnable calls have finished.
    }

    private void Start()
    {
        if (_bank != null) return; // Already bound from OnEnable.

        Debug.Log($"[SoulHUD] Start fallback — SoulContext.Current: {(SoulContext.Current != null ? "found" : "NULL")}");
        if (SoulContext.Current != null)
            Bind(SoulContext.Current.SoulBank);
        else
            Debug.LogWarning("[SoulHUD] Start: SoulContext.Current is still NULL — is SoulContext present in the scene?");
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
            Debug.LogWarning("[SoulHUD] Bind called with NULL SoulBank.");
            return;
        }

        Debug.Log($"[SoulHUD] Bind — subscribed to SoulBank '{_bank.name}'. Current souls: {_bank.StoredSouls}");
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
        Debug.Log($"[SoulHUD] HandleSoulsChanged — delta: {delta:+#;-#;0}, newTotal: {newTotal}");
        RefreshCounter(newTotal);
        SpawnDelta(delta);
    }

    private void RefreshCounter(int total)
    {
        if (_counterLabel != null)
            _counterLabel.text = total.ToString();
        else
            Debug.LogWarning("[SoulHUD] _counterLabel is NULL — assign it in the Inspector.");
    }

    private void SpawnDelta(int delta)
    {
        if (_deltaTextPrefab == null)
        {
            Debug.LogWarning("[SoulHUD] _deltaTextPrefab is NULL — assign the SoulDeltaText prefab in the Inspector.");
            return;
        }
        if (_deltaSpawnPoint == null)
        {
            Debug.LogWarning("[SoulHUD] _deltaSpawnPoint is NULL — assign a RectTransform spawn point in the Inspector.");
            return;
        }

        Debug.Log($"[SoulHUD] Spawning SoulDeltaText with delta {delta:+#;-#;0}");
        SoulDeltaText popup = Instantiate(_deltaTextPrefab, _deltaSpawnPoint.parent);
        popup.transform.SetParent(_deltaSpawnPoint.transform, false);
        popup.Play(delta);
    }
}
