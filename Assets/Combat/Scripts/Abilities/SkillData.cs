using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "SkillData", menuName = "Combat/Skills/Skill Data")]
public class SkillData : ScriptableObject
{
    #region Metadata

    [SerializeField] private string _skillId;
    [SerializeField] private string _displayName;
    [SerializeField] [TextArea(3, 10)] private string _description;
    [SerializeField] private Sprite _icon;

    #endregion

    #region Targeting

    [SerializeField] private PrimaryTargetRequirement _primaryTargetRequirement = PrimaryTargetRequirement.Hostile;
    [SerializeField] private ImpactTargetRequirement _impactTargetRequirement = ImpactTargetRequirement.Hostile;
    [SerializeField] private TargetSelectionMode _targetSelectionMode = TargetSelectionMode.RoleBasedOffensive;
    [SerializeField] private TargetFallbackMode _targetFallbackMode = TargetFallbackMode.Retarget;
    #endregion

    #region Delivery

    [SerializeField] private SkillTrajectory _trajectory = SkillTrajectory.Hitscan;
    [SerializeField] private ImpactPattern _impactPattern = ImpactPattern.Direct;
    [SerializeField] private ImpactCenterMode _impactCenterMode = ImpactCenterMode.PrimaryTarget;
    [SerializeField] private SkillExecutionMode _skillExecutionMode = SkillExecutionMode.Instant;

    #endregion

    #region Effects

    [Header("Composition")]
    [SerializeField] private SkillCompositionEffect[] _compositionEffects;
    [SerializeField] private SkillModifierKind[] _compositionModifierKinds;
    [SerializeField] private SkillCompositionModifierData[] _compositionModifierData;
    [SerializeField] private SkillCompositionState _compositionState = SkillCompositionState.Official;

    #endregion

    #region Parameters

    [SerializeField] private float _castTime = 0f;
    [SerializeField] private int _rangeInCells = 1;
    [FormerlySerializedAs("_splashRadiusInCells")]
    [SerializeField] private int _radiusInCells = 1;
    [SerializeField] private int _lineLengthInCells = 1;
    [SerializeField] private int _maxTargets = 1;

    #endregion

    #region Metadata Properties

    public string SkillId => _skillId;
    public string DisplayName => _displayName;
    public string Description => _description;
    public Sprite Icon => _icon;

    #endregion

    #region Targeting Properties

    public PrimaryTargetRequirement PrimaryTargetRequirement => _primaryTargetRequirement;
    public ImpactTargetRequirement ImpactTargetRequirement => _impactTargetRequirement;
    public TargetSelectionMode TargetSelectionMode => _targetSelectionMode;
    public TargetFallbackMode TargetFallbackMode => _targetFallbackMode;
    #endregion

    #region Delivery Properties

    public SkillTrajectory Trajectory => _trajectory;
    public ImpactPattern ImpactPattern => _impactPattern;
    public ImpactCenterMode ImpactCenterMode => _impactCenterMode;
    public SkillExecutionMode ExecutionMode => _skillExecutionMode;

    #endregion

    #region Effect Properties

    public SkillCompositionEffect[] CompositionEffects => _compositionEffects;
    public SkillModifierKind[] CompositionModifierKinds => _compositionModifierKinds;
    public SkillCompositionModifierData[] CompositionModifierData => _compositionModifierData;
    public SkillCompositionState CompositionState => _compositionState;

    #endregion

    #region Parameter Properties

    public float CastTime => Mathf.Max(0f, _castTime);
    public int RangeInCells => Mathf.Max(0, _rangeInCells);
    public int RadiusInCells => Mathf.Max(0, _radiusInCells);
    public int LineLengthInCells => Mathf.Max(0, _lineLengthInCells);
    public int MaxTargets => Mathf.Max(1, _maxTargets);

    #endregion

    #region Validation

    public bool TryValidateDeclarativeContract(out string validationError)
    {
        if (!IsValidPrimaryTargetRequirement(_primaryTargetRequirement))
        {
            validationError = $"Skill '{name}' has invalid PrimaryTargetRequirement value '{(int)_primaryTargetRequirement}'.";
            return false;
        }

        if (!IsValidImpactTargetRequirement(_impactTargetRequirement))
        {
            validationError = $"Skill '{name}' has invalid ImpactTargetRequirement value '{(int)_impactTargetRequirement}'.";
            return false;
        }

        if (!IsValidTargetSelectionMode(_targetSelectionMode))
        {
            validationError = $"Skill '{name}' has invalid TargetSelectionMode value '{(int)_targetSelectionMode}'.";
            return false;
        }

        if (!IsValidTargetFallbackMode(_targetFallbackMode))
        {
            validationError = $"Skill '{name}' has invalid TargetFallbackMode value '{(int)_targetFallbackMode}'.";
            return false;
        }

        if (!IsValidSkillTrajectory(_trajectory))
        {
            validationError = $"Skill '{name}' has invalid SkillTrajectory value '{(int)_trajectory}'.";
            return false;
        }

        if (!IsValidImpactPattern(_impactPattern))
        {
            validationError = $"Skill '{name}' has invalid ImpactPattern value '{(int)_impactPattern}'.";
            return false;
        }

        if (!IsValidImpactCenterMode(_impactCenterMode))
        {
            validationError = $"Skill '{name}' has invalid ImpactCenterMode value '{(int)_impactCenterMode}'.";
            return false;
        }

        if (!IsValidSkillExecutionMode(_skillExecutionMode))
        {
            validationError = $"Skill '{name}' has invalid SkillExecutionMode value '{(int)_skillExecutionMode}'.";
            return false;
        }

        validationError = null;
        return true;
    }

    private static bool IsValidPrimaryTargetRequirement(PrimaryTargetRequirement value)
    {
        switch (value)
        {
            case PrimaryTargetRequirement.None:
            case PrimaryTargetRequirement.Hostile:
            case PrimaryTargetRequirement.Ally:
            case PrimaryTargetRequirement.Self:
            case PrimaryTargetRequirement.GroundCell:
                return true;
            default:
                return false;
        }
    }

    private static bool IsValidImpactTargetRequirement(ImpactTargetRequirement value)
    {
        switch (value)
        {
            case ImpactTargetRequirement.Hostile:
            case ImpactTargetRequirement.Ally:
            case ImpactTargetRequirement.Self:
            case ImpactTargetRequirement.Any:
                return true;
            default:
                return false;
        }
    }

    private static bool IsValidTargetSelectionMode(TargetSelectionMode value)
    {
        switch (value)
        {
            case TargetSelectionMode.None:
            case TargetSelectionMode.Closest:
            case TargetSelectionMode.LowestHealth:
            case TargetSelectionMode.HighestBasicDamage:
            case TargetSelectionMode.AllyLowestHealth:
            case TargetSelectionMode.AllyRolePriority:
            case TargetSelectionMode.RoleBasedOffensive:
                return true;
            default:
                return false;
        }
    }

    private static bool IsValidTargetFallbackMode(TargetFallbackMode value)
    {
        switch (value)
        {
            case TargetFallbackMode.Cancel:
            case TargetFallbackMode.Retarget:
            case TargetFallbackMode.ContinueFromCurrentContext:
                return true;
            default:
                return false;
        }
    }

    private static bool IsValidSkillTrajectory(SkillTrajectory value)
    {
        switch (value)
        {
            case SkillTrajectory.Hitscan:
            case SkillTrajectory.Projectile:
                return true;
            default:
                return false;
        }
    }

    private static bool IsValidImpactPattern(ImpactPattern value)
    {
        switch (value)
        {
            case ImpactPattern.Direct:
            case ImpactPattern.Area:
            case ImpactPattern.Line:
            case ImpactPattern.MultiTarget:
                return true;
            default:
                return false;
        }
    }

    private static bool IsValidImpactCenterMode(ImpactCenterMode value)
    {
        switch (value)
        {
            case ImpactCenterMode.PrimaryTarget:
            case ImpactCenterMode.Caster:
            case ImpactCenterMode.TargetCell:
                return true;
            default:
                return false;
        }
    }

    private static bool IsValidSkillExecutionMode(SkillExecutionMode value)
    {
        switch (value)
        {
            case SkillExecutionMode.Instant:
            case SkillExecutionMode.CastTime:
            case SkillExecutionMode.Channel:
                return true;
            default:
                return false;
        }
    }

    #endregion
}
