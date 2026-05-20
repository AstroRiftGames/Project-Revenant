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
    [FormerlySerializedAs("_targetMode")]
    [SerializeField] private ImpactCenterMode _impactCenterMode = ImpactCenterMode.PrimaryTarget;
    [SerializeField] private SkillShape _shape = SkillShape.SingleTarget;
    [SerializeField] private SkillRequirements _requirements = new();
    [SerializeField] private SkillTargetRequirement _impactTargetRequirement = SkillTargetRequirement.Any;

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

    public ImpactCenterMode ImpactCenterMode => _impactCenterMode;
    public SkillTargetRequirement TargetRequirement => _requirements != null
        ? _requirements.TargetRequirement
        : SkillTargetRequirement.Any;
    public SkillTargetRequirement ImpactTargetRequirement => NormalizeImpactTargetRequirement(_impactTargetRequirement);
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

    #region Legacy y deuda

    public float Cooldown => Mathf.Max(0f, _cooldown);

    #endregion

    #region Resolution Helpers

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
