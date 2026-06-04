using UnityEngine;

[CreateAssetMenu(fileName = "ShieldSkillEffect", menuName = "Combat/Skills/Effects/Shield Skill Effect")]
public class ShieldSkillEffect : SkillEffect
{
    [SerializeField] private int _shieldAmount = 1;
    [SerializeField] private float _durationSeconds = 4f;

    public int ShieldAmount => Mathf.Max(0, _shieldAmount);
    public float DurationSeconds => Mathf.Max(0f, _durationSeconds);

    public override bool Apply(SkillContext context, SkillImpact impact)
    {
        Unit hitUnit = ResolveTargetUnit(impact);
        Unit caster = context != null ? context.Caster : null;
        if (caster == null || !IsCombatAliveUnit(hitUnit))
            return false;

        int shieldAmount = ShieldAmount;
        float durationSeconds = DurationSeconds;
        if (shieldAmount <= 0 || durationSeconds <= 0f)
            return false;

        ShieldController shieldController = hitUnit.GetComponent<ShieldController>();
        if (shieldController == null)
            shieldController = hitUnit.gameObject.AddComponent<ShieldController>();

        shieldController.ApplyShield(shieldAmount, durationSeconds);
        return true;
    }
}
