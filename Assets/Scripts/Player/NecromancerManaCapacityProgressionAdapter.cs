using UnityEngine;

[DisallowMultipleComponent]
public class NecromancerManaCapacityProgressionAdapter : MonoBehaviour
{
    [SerializeField] private ManaBank _manaBank;
    [SerializeField] private NecromancerProgressionContext _progressionContext;
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
        RefreshMaxMana();
    }

    private void Start()
    {
        if (_progressionBank == null)
        {
            ResolveDependencies();
            BindToProgression();
        }

        RefreshMaxMana();
    }

    private void OnDisable()
    {
        UnbindFromProgression();
    }

    public void Configure(ManaBank manaBank, NecromancerProgressionContext progressionContext, int fallbackMaxMana)
    {
        _manaBank = manaBank;
        _progressionContext = progressionContext;
        _fallbackMaxMana = Mathf.Max(0, fallbackMaxMana);

        ResolveDependencies();
        BindToProgression();
        RefreshMaxMana();
    }

    private void HandleProgressionChanged(NecromancerProgressionSnapshot snapshot, int delta)
    {
        RefreshMaxMana();
    }

    private void RefreshMaxMana()
    {
        ResolveDependencies();
        if (_manaBank == null)
            return;

        NecromancerProgressionProfile profile = _progressionContext != null ? _progressionContext.Profile : null;
        int currentLevel = _progressionBank != null ? _progressionBank.CurrentLevel : 1;
        int maximumMana = profile != null
            ? profile.GetMaxManaForLevel(currentLevel, _fallbackMaxMana)
            : _fallbackMaxMana;

        _manaBank.SetMaximumMana(maximumMana);
    }

    private void ResolveDependencies()
    {
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
