using UnityEngine;

[CreateAssetMenu(fileName = "DamageSkillEffect", menuName = "Combat/Skills/Effects/Damage Skill Effect")]
public class DamageSkillEffect : SkillEffect
{
    [SerializeField] private int _damage = 1;

    public override bool Apply(Unit caster, SkillData skill, Unit chosenTarget, Unit target)
    {
        if (caster == null || target == null || !target.IsAlive)
            return false;

        target.TakeDamage(Mathf.Max(0, _damage), caster);

        if (caster.StatusEffects != null && caster.StatusEffects.HasLifeSteal)
        {
            float healPercent = caster.StatusEffects.GetEffectStrength(StatusEffectType.LifeSteal);
            int healAmount = Mathf.RoundToInt(_damage * healPercent);
            LifeController casterLife = caster.GetComponent<LifeController>();
            if (healAmount > 0 && casterLife != null)
                casterLife.Heal(healAmount, target);
        }

        return true;
    }
}
