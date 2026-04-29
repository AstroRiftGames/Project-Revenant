using UnityEngine;

[DisallowMultipleComponent]
public class NecromancerPartyCapacityProgressionAdapter : MonoBehaviour
{
    [SerializeField] private NecromancerParty _party;
    [SerializeField] private NecromancerProgressionContext _progressionContext;
    [SerializeField, Min(1)] private int _fallbackMaxPartyMembers = 3;

    private NecromancerProgressionBank _progressionBank;

    private void Awake()
    {
        ResolveDependencies();
    }

    private void OnEnable()
    {
        ResolveDependencies();
        BindToProgression();
        RefreshPartyCapacity();
    }

    private void Start()
    {
        if (_progressionBank == null)
        {
            ResolveDependencies();
            BindToProgression();
        }

        RefreshPartyCapacity();
    }

    private void OnDisable()
    {
        UnbindFromProgression();
    }

    public void Configure(NecromancerParty party, NecromancerProgressionContext progressionContext, int fallbackMaxPartyMembers)
    {
        _party = party;
        _progressionContext = progressionContext;
        _fallbackMaxPartyMembers = Mathf.Max(1, fallbackMaxPartyMembers);

        ResolveDependencies();
        BindToProgression();
        RefreshPartyCapacity();
    }

    private void HandleProgressionChanged(NecromancerProgressionSnapshot snapshot, int delta)
    {
        RefreshPartyCapacity();
    }

    private void RefreshPartyCapacity()
    {
        ResolveDependencies();
        if (_party == null)
            return;

        NecromancerProgressionProfile profile = _progressionContext != null ? _progressionContext.Profile : null;
        int currentLevel = _progressionBank != null ? _progressionBank.CurrentLevel : 1;
        int maxPartyMembers = profile != null
            ? profile.GetMaxPartyMembersForLevel(currentLevel, _fallbackMaxPartyMembers)
            : _fallbackMaxPartyMembers;

        _party.SetMaxPartyMembers(maxPartyMembers);
    }

    private void ResolveDependencies()
    {
        _party ??= GetComponent<NecromancerParty>();
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
