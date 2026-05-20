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
    #region Metadata

    [SerializeField] private string _skillId;
    [SerializeField] private string _displayName;
    [SerializeField] [TextArea(3, 10)] private string _description;
    [SerializeField] private Sprite _icon;

    #endregion

    #region Tipo

    // Documented skill type contract:
    // how the skill is applied, target selection, impact pattern, execution,
    // and valid target rules.
    [SerializeField] private SkillTargetMode _targetMode = SkillTargetMode.CurrentTarget;
    [SerializeField] private SkillShape _shape = SkillShape.SingleTarget;
    [SerializeField] private SkillRequirements _requirements = new();
    // Explicit impact target contract for shapes that do not rely on a
    // primary unit target. GroundCell remains technical support only.
    [SerializeField] private SkillTargetRequirement _impactTargetRequirement = SkillTargetRequirement.Legacy;

    #endregion

    #region Efectos

    // Effect layer: what the skill actually causes once a hit target is
    // resolved. Damage, healing, summon and knockback currently live here.
    [SerializeField] private SkillEffect[] _effects;
    [FormerlySerializedAs("_appliedStatusEffects")]
    [SerializeField] private SkillStatusEffect[] _statusEffects;

    #endregion

    #region Modificadores

    // Documented modifiers are not modeled as composable data yet.
    // Current runtime still expresses some modifier-like behavior through
    // Shape and parameter fields such as Splash/PiercingLine.

    #endregion

    #region Parametros

    [SerializeField] private float _castTime = 0f;
    [SerializeField] private int _rangeInCells = 1;
    [SerializeField] private int _splashRadiusInCells = 1;
    [SerializeField] private int _lineLengthInCells = 1;
    [SerializeField] private int _maxTargets = 1;

    #endregion

    #region Legacy y deuda

    // Legacy availability field kept because current SkillCaster cooldown
    // flow still consumes it directly. This is not part of the documented
    // type/effect/modifier split yet.
    [SerializeField] private float _cooldown = 5f;

    #endregion

    #region Metadata Properties

    public string SkillId => _skillId;
    public string DisplayName => _displayName;
    public string Description => _description;
    public Sprite Icon => _icon;

    #endregion

    #region Tipo Properties

    public SkillTargetMode TargetMode => _targetMode;
    public SkillTargetRequirement TargetRequirement => ResolveTargetRequirement();
    public SkillTargetRequirement ImpactTargetRequirement => ResolveImpactTargetRequirement();
    public SkillShape Shape => _shape;
    public SkillRequirements Requirements => _requirements;

    #endregion

    #region Efecto Properties

    public SkillEffect[] Effects => _effects;
    public SkillStatusEffect[] StatusEffects => _statusEffects;

    #endregion

    #region Parametro Properties

    public float CastTime => Mathf.Max(0f, _castTime);
    public int RangeInCells => Mathf.Max(0, _rangeInCells);
    public int SplashRadiusInCells => Mathf.Max(0, _splashRadiusInCells);
    public int LineLengthInCells => Mathf.Max(0, _lineLengthInCells);
    public int MaxTargets => Mathf.Max(1, _maxTargets);

    #endregion

    #region Legacy y compatibilidad

    public float Cooldown => Mathf.Max(0f, _cooldown);
    // Legacy alias kept because runtime collectors still read the older
    // impact radius name even though the documented parameter is splash radius.
    public int ImpactRadiusInCells => SplashRadiusInCells;
    // Compatibility helper kept for existing validators and callers that still
    // inspect the raw serialized requirements object.
    public bool RequiresTarget => Requirements == null || Requirements.requiresTarget;
    // Compatibility helper derived from documented target requirements.
    public bool ResolvesPrimaryTargetToCaster =>
        TargetRequirement == SkillTargetRequirement.Self;
    // Compatibility helper for current area/self-centered runtime paths.
    public bool UsesCasterAsImpactCenter =>
        TargetMode == SkillTargetMode.Self &&
        (Shape == SkillShape.Area || Shape == SkillShape.Splash || Shape == SkillShape.MultiTarget);
    // Compatibility helper for current visual popup anchoring rules.
    public bool UsesCasterAsPresentationAnchor =>
        Shape == SkillShape.SpawnMinions || TargetMode == SkillTargetMode.Self;

    #endregion

    #region Resolution Helpers

    private SkillTargetRequirement ResolveTargetRequirement()
    {
        return Requirements != null
            ? Requirements.TargetRequirement
            : SkillTargetRequirement.Any;
    }

    private SkillTargetRequirement ResolveImpactTargetRequirement()
    {
        if (_impactTargetRequirement != SkillTargetRequirement.Legacy)
            return NormalizeImpactTargetRequirement(_impactTargetRequirement);

        SkillTargetRequirement targetRequirement = TargetRequirement;

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

    #endregion
}
