using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(NecromancerProgressionBank))]
public class NecromancerProgressionContext : MonoBehaviour
{
    private const string DefaultProfileResourcePath = "DefaultNecromancerProgressionProfile";

    public static NecromancerProgressionContext Current { get; private set; }

    [SerializeField] private NecromancerProgressionProfile _profile;
    [SerializeField] private NecromancerProgressionBank _progressionBank;

    public NecromancerProgressionProfile Profile => _profile;
    public NecromancerProgressionBank ProgressionBank => _progressionBank;

    private void Awake()
    {
        EnsureConfigured();
    }

    private void OnEnable()
    {
        Current = this;
        EnsureConfigured();
    }

    private void OnDisable()
    {
        if (ReferenceEquals(Current, this))
            Current = null;
    }

    public void Configure(NecromancerProgressionProfile profile = null, NecromancerProgressionBank progressionBank = null)
    {
        if (profile != null)
            _profile = profile;

        if (progressionBank != null)
            _progressionBank = progressionBank;

        EnsureConfigured();
    }

    public int AwardExperience(int amount)
    {
        EnsureConfigured();
        if (_progressionBank == null)
            return 0;

        return _progressionBank.AwardExperience(amount, _profile);
    }

    public int AwardRoomVictoryExperience(int defeatedEnemyCount, int floorNumber)
    {
        EnsureConfigured();
        if (_profile == null)
            return 0;

        int experienceAward = _profile.CalculateRoomVictoryExperience(defeatedEnemyCount, floorNumber);
        return AwardExperience(experienceAward);
    }

    private void EnsureConfigured()
    {
        _progressionBank ??= GetComponent<NecromancerProgressionBank>();
        if (_progressionBank == null)
        {
            Debug.LogError(
                $"[{nameof(NecromancerProgressionContext)}] Missing {nameof(NecromancerProgressionBank)} on '{name}'. " +
                "Add the required scripts on the manager prefab instead of relying on runtime setup.",
                this);
            return;
        }

        _profile ??= Resources.Load<NecromancerProgressionProfile>(DefaultProfileResourcePath);
        if (_profile == null)
        {
            _profile = ScriptableObject.CreateInstance<NecromancerProgressionProfile>();
            _profile.name = "RuntimeNecromancerProgressionProfile";
        }

        _progressionBank.Initialize(_profile);
    }
}
