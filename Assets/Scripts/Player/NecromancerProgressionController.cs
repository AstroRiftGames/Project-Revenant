using UnityEngine;

[DisallowMultipleComponent]
public class NecromancerProgressionController : MonoBehaviour
{
    [SerializeField] private NecromancerParty _party;
    [SerializeField] private ManaBank _manaBank;
    [SerializeField] private NecromancerProgressionContext _progressionContext;
    [SerializeField, Min(1)] private int _fallbackMaxPartyMembers = 3;
    [SerializeField, Min(0)] private int _fallbackMaxMana = 10;

    private NecromancerProgressionBank _progressionBank;

    private void Awake()
    {
        ResolveDependencies();
    }

    private void OnEnable()
    {
        ResolveDependencies();
        BindToProgression();
        ApplyProgressionState();
    }

    private void Start()
    {
        if (_progressionBank == null)
        {
            ResolveDependencies();
            BindToProgression();
        }

        ApplyProgressionState();
    }

    private void OnDisable()
    {
        UnbindFromProgression();
    }

    public void Configure(
        NecromancerParty party,
        ManaBank manaBank,
        NecromancerProgressionContext progressionContext,
        int fallbackMaxPartyMembers,
        int fallbackMaxMana)
    {
        _party = party;
        _manaBank = manaBank;
        _progressionContext = progressionContext;
        _fallbackMaxPartyMembers = Mathf.Max(1, fallbackMaxPartyMembers);
        _fallbackMaxMana = Mathf.Max(0, fallbackMaxMana);

        ResolveDependencies();
        BindToProgression();
        ApplyProgressionState();
    }

    private void HandleProgressionChanged(NecromancerProgressionSnapshot snapshot, int delta)
    {
        ApplyProgressionState();
    }

    private void ApplyProgressionState()
    {
        ResolveDependencies();

        NecromancerProgressionProfile profile = _progressionContext != null ? _progressionContext.Profile : null;
        int currentLevel = _progressionBank != null ? _progressionBank.CurrentLevel : 1;

        if (_party != null)
        {
            int maxPartyMembers = profile != null
                ? profile.GetMaxPartyMembersForLevel(currentLevel, _fallbackMaxPartyMembers)
                : _fallbackMaxPartyMembers;

            _party.SetMaxPartyMembers(maxPartyMembers);
        }

        if (_manaBank != null)
        {
            int maxMana = profile != null
                ? profile.GetMaxManaForLevel(currentLevel, _fallbackMaxMana)
                : _fallbackMaxMana;

            _manaBank.SetMaximumMana(maxMana);
        }
    }

    private void ResolveDependencies()
    {
        _party ??= GetComponent<NecromancerParty>();
        _manaBank ??= GetComponent<ManaBank>();
        _progressionContext ??= GetComponent<NecromancerProgressionContext>();
        _progressionContext ??= NecromancerProgressionContext.Current;
    }

    private void BindToProgression()
    {
        NecromancerProgressionBank nextBank = _progressionContext != null ? _progressionContext.ProgressionBank : null;
        if (ReferenceEquals(_progressionBank, nextBank))
            return;

        UnbindFromProgression();
        _progressionBank = nextBank;

        if (_progressionBank != null)
            _progressionBank.OnProgressionChanged += HandleProgressionChanged;
    }

    private void UnbindFromProgression()
    {
        if (_progressionBank != null)
        {
            _progressionBank.OnProgressionChanged -= HandleProgressionChanged;
            _progressionBank = null;
        }
    }
}
