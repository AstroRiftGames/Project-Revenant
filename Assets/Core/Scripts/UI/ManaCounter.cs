using UnityEngine;

/// <summary>
/// Persistent HUD element showing the player's current Mana count.
/// Spawn point for ManaDeltaText popups when mana is gained or spent.
/// Requires a ManaBank reference via ManaContext.Current.ManaBank or direct assignment.
/// </summary>
public class ManaCounter : BaseCurrencyCounter
{
    public static ManaCounter Instance { get; private set; }

    private ManaBank _bank;

    protected override void Awake()
    {
        base.Awake();
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(this);
    }

    private void OnEnable()
    {
        if (ManaContext.Current != null)
            Bind(ManaContext.Current.ManaBank);
    }

    private void Start()
    {
        if (_bank != null) return; // Already bound from OnEnable.

        if (ManaContext.Current != null)
            Bind(ManaContext.Current.ManaBank);
    }

    private void OnDisable()
    {
        Unbind();
    }

    /// <summary>Late-bind to a bank (e.g. after a scene transition sets up ManaContext).</summary>
    public void Bind(ManaBank bank)
    {
        Unbind();
        _bank = bank;
        if (_bank == null)
        {
            return;
        }

        _bank.OnManaChanged += HandleManaChanged;
        RefreshCounter(_bank.StoredMana);
    }

    private void Unbind()
    {
        if (_bank != null)
        {
            _bank.OnManaChanged -= HandleManaChanged;
            _bank = null;
        }
    }

    private void HandleManaChanged(int newTotal, int delta)
    {
        RefreshCounter(newTotal);
        SpawnDelta(delta);
        ShowTemporarily();
    }
}
