using UnityEngine;

[CreateAssetMenu(fileName = "ApplyStatusSkillEffect", menuName = "Combat/Skills/Effects/Apply Status Skill Effect")]
public class ApplyStatusSkillEffect : SkillEffect
{
    [SerializeField] private StatusEffectDefinition[] _statusDefinitions;

    public override bool Apply(SkillContext context, Unit hitUnit)
    {
        Unit caster = context != null ? context.Caster : null;
        SkillData skill = context != null ? context.Skill : null;
        StatusEffectController statusController = hitUnit != null ? hitUnit.StatusEffects : null;

        if (caster == null || skill == null || hitUnit == null || statusController == null)
            return false;

        if (_statusDefinitions == null || _statusDefinitions.Length == 0)
            return false;

        bool anyApplied = false;
        for (int i = 0; i < _statusDefinitions.Length; i++)
        {
            StatusEffectDefinition definition = _statusDefinitions[i];
            if (definition == null)
                continue;

            StatusEffectApplication application = new(
                hitUnit,
                caster,
                skill,
                definition);

            anyApplied |= statusController.TryApply(application);
        }

        return anyApplied;
    }
}
