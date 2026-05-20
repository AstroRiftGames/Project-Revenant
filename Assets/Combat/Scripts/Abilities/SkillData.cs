using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public struct SkillStatusEffect
{
    [SerializeField] private StatusEffectDefinition _definition;
    [SerializeField] private TargetRelation _targetRelation;

    public StatusEffectDefinition Definition => _definition;
    public TargetRelation TargetRelation => _targetRelation;
}

[CreateAssetMenu(fileName = "SkillData", menuName = "Combat/Skills/Skill Data")]
public class SkillData : ScriptableObject
{
    [SerializeField] private string _skillId;
    [SerializeField] private string _displayName;
    [SerializeField] [TextArea(3, 10)] private string _description;
    [SerializeField] private Sprite _icon;
    [SerializeField] private float _cooldown = 5f;
    [SerializeField] private float _castTime = 0f;
    [SerializeField] private int _rangeInCells = 1;
    [SerializeField] private int _splashRadiusInCells = 1;
    [SerializeField] private int _lineLengthInCells = 1;
    [SerializeField] private int _maxTargets = 1;
    [SerializeField] private SkillTargetMode _targetMode = SkillTargetMode.CurrentTarget;
    [SerializeField] private SkillShape _shape = SkillShape.SingleTarget;
    [SerializeField] private SkillRequirements _requirements = new();
    // Defines which unit relationship the skill's shape may affect.
    // This is the explicit design-time contract for NoTarget/GroundCell skills.
    [SerializeField] private SkillTargetRequirement _impactTargetRequirement = SkillTargetRequirement.Legacy;
    [SerializeField] private SkillEffect[] _effects;
    [FormerlySerializedAs("_appliedStatusEffects")]
    [SerializeField] private SkillStatusEffect[] _statusEffects;

    public string SkillId => _skillId;
    public string DisplayName => _displayName;
    public string Description => _description;
    public Sprite Icon => _icon;
    public float Cooldown => Mathf.Max(0f, _cooldown);
    public float CastTime => Mathf.Max(0f, _castTime);
    public int RangeInCells => Mathf.Max(0, _rangeInCells);
    public int SplashRadiusInCells => Mathf.Max(0, _splashRadiusInCells);
    public int ImpactRadiusInCells => SplashRadiusInCells;
    public int LineLengthInCells => Mathf.Max(0, _lineLengthInCells);
    public int MaxTargets => Mathf.Max(1, _maxTargets);
    public SkillTargetMode TargetMode => _targetMode;
    public SkillShape Shape => _shape;
    public SkillRequirements Requirements => _requirements;
    public SkillTargetRequirement ImpactTargetRequirement => ResolveImpactTargetRequirement();
    public SkillEffect[] Effects => _effects;
    public SkillStatusEffect[] StatusEffects => _statusEffects;
    public bool ResolvesPrimaryTargetToCaster =>
        Requirements != null && Requirements.TargetRequirement == SkillTargetRequirement.Self;
    public bool UsesCasterAsImpactCenter =>
        TargetMode == SkillTargetMode.Self &&
        (Shape == SkillShape.Area || Shape == SkillShape.Splash || Shape == SkillShape.MultiTarget);
    public bool UsesCasterAsPresentationAnchor =>
        Shape == SkillShape.SpawnMinions || TargetMode == SkillTargetMode.Self;
    public bool RequiresTarget => Requirements == null || Requirements.requiresTarget;

    private SkillTargetRequirement ResolveImpactTargetRequirement()
    {
        if (_impactTargetRequirement != SkillTargetRequirement.Legacy)
            return NormalizeImpactTargetRequirement(_impactTargetRequirement);

        SkillTargetRequirement targetRequirement = Requirements != null
            ? Requirements.TargetRequirement
            : SkillTargetRequirement.Any;

        switch (targetRequirement)
        {
            case SkillTargetRequirement.Hostile:
            case SkillTargetRequirement.Ally:
            case SkillTargetRequirement.Self:
            case SkillTargetRequirement.Any:
                return targetRequirement;
            case SkillTargetRequirement.NoTarget:
            case SkillTargetRequirement.GroundCell:
                return ResolveNoPrimaryImpactTargetRequirement();
            default:
                return SkillTargetRequirement.Any;
        }
    }

    private SkillTargetRequirement ResolveNoPrimaryImpactTargetRequirement()
    {
        // Temporary compatibility fallback for older assets that still rely on
        // status configuration to imply impact relationship. New NoTarget and
        // GroundCell skills should set _impactTargetRequirement explicitly.
        if (_statusEffects == null || _statusEffects.Length == 0)
            return SkillTargetRequirement.Any;

        bool foundRelation = false;
        TargetRelation resolvedRelation = TargetRelation.Any;

        for (int i = 0; i < _statusEffects.Length; i++)
        {
            TargetRelation statusRelation = _statusEffects[i].TargetRelation;
            if (!foundRelation)
            {
                resolvedRelation = statusRelation;
                foundRelation = true;
                continue;
            }

            if (resolvedRelation != statusRelation)
                return SkillTargetRequirement.Any;
        }

        if (!foundRelation)
            return SkillTargetRequirement.Any;

        return resolvedRelation switch
        {
            TargetRelation.Hostile => SkillTargetRequirement.Hostile,
            TargetRelation.Ally => SkillTargetRequirement.Ally,
            _ => SkillTargetRequirement.Any
        };
    }

    private static SkillTargetRequirement NormalizeImpactTargetRequirement(SkillTargetRequirement targetRequirement)
    {
        return targetRequirement switch
        {
            SkillTargetRequirement.NoTarget => SkillTargetRequirement.Any,
            SkillTargetRequirement.GroundCell => SkillTargetRequirement.Any,
            _ => targetRequirement
        };
    }
}
