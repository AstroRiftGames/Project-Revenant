using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD element that displays the player's profile information:
/// Level, XP progress, Mana, Souls, and Team Size.
/// </summary>
public class ProfileUI : MonoBehaviour
{
    [Header("Level")]
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private Slider _levelProgressBar;

    [Header("Currency: Mana")]
    [SerializeField] private Slider _manaBar;

    [Header("Currency: Souls")]
    [SerializeField] private TextMeshProUGUI _soulsText;

    [Header("Team")]
    [SerializeField] private TextMeshProUGUI _teamSizeText;

    private void OnEnable()
    {
        SubscribeEvents();
        RefreshAll();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void SubscribeEvents()
    {
        if (ManaContext.Current != null && ManaContext.Current.ManaBank != null)
        {
            ManaContext.Current.ManaBank.OnManaChanged += HandleManaChanged;
            ManaContext.Current.ManaBank.OnMaximumManaChanged += HandleMaxManaChanged;
        }

        if (SoulContext.Current != null && SoulContext.Current.SoulBank != null)
            SoulContext.Current.SoulBank.OnSoulsChanged += HandleSoulsChanged;

        if (NecromancerProgressionContext.Current != null && NecromancerProgressionContext.Current.ProgressionBank != null)
            NecromancerProgressionContext.Current.ProgressionBank.OnProgressionChanged += HandleProgressionChanged;

        NecromancerParty.OnPartyUpdated += RefreshTeamSize;
    }

    private void UnsubscribeEvents()
    {
        if (ManaContext.Current != null && ManaContext.Current.ManaBank != null)
        {
            ManaContext.Current.ManaBank.OnManaChanged -= HandleManaChanged;
            ManaContext.Current.ManaBank.OnMaximumManaChanged -= HandleMaxManaChanged;
        }

        if (SoulContext.Current != null && SoulContext.Current.SoulBank != null)
            SoulContext.Current.SoulBank.OnSoulsChanged -= HandleSoulsChanged;

        if (NecromancerProgressionContext.Current != null && NecromancerProgressionContext.Current.ProgressionBank != null)
            NecromancerProgressionContext.Current.ProgressionBank.OnProgressionChanged -= HandleProgressionChanged;

        NecromancerParty.OnPartyUpdated -= RefreshTeamSize;
    }

    private void RefreshAll()
    {
        RefreshMana();
        RefreshSouls();
        RefreshProgression();
        RefreshTeamSize();
    }

    private void RefreshMana()
    {
        if (ManaContext.Current == null || ManaContext.Current.ManaBank == null || _manaBar == null) return;
        _manaBar.maxValue = ManaContext.Current.ManaBank.MaximumMana;
        _manaBar.value = ManaContext.Current.ManaBank.StoredMana;
    }

    private void HandleManaChanged(int newTotal, int delta)
    {
        if (_manaBar != null) _manaBar.value = newTotal;
    }

    private void HandleMaxManaChanged(int newMax, int delta)
    {
        if (_manaBar != null) _manaBar.maxValue = newMax;
    }

    private void RefreshSouls()
    {
        if (SoulContext.Current == null || SoulContext.Current.SoulBank == null || _soulsText == null) return;
        _soulsText.text = SoulContext.Current.SoulBank.StoredSouls.ToString();
    }

    private void HandleSoulsChanged(int newTotal, int delta)
    {
        if (_soulsText != null) _soulsText.text = newTotal.ToString();
    }

    private void RefreshProgression()
    {
        if (NecromancerProgressionContext.Current == null || NecromancerProgressionContext.Current.ProgressionBank == null) return;
        
        var bank = NecromancerProgressionContext.Current.ProgressionBank;
        var profile = NecromancerProgressionContext.Current.Profile;
        var snapshot = bank.GetSnapshot(profile);
        
        UpdateProgressionUI(snapshot);
    }

    private void HandleProgressionChanged(NecromancerProgressionSnapshot snapshot, int deltaXP)
    {
        UpdateProgressionUI(snapshot);
    }

    private void UpdateProgressionUI(NecromancerProgressionSnapshot snapshot)
    {
        if (_levelText != null) _levelText.text = snapshot.CurrentLevel.ToString("00");
        if (_levelProgressBar != null)
        {
            _levelProgressBar.maxValue = snapshot.HasReachedMaxLevel ? 1 : snapshot.ExperienceRequiredForNextLevel;
            _levelProgressBar.value = snapshot.ExperienceIntoCurrentLevel;
        }
    }

    private void RefreshTeamSize()
    {
        if (NecromancerParty.Instance == null || _teamSizeText == null) return;
        _teamSizeText.text = $"{NecromancerParty.Instance.SlotsUsed}/{NecromancerParty.Instance.MaxPartyMembers}";
    }
}
