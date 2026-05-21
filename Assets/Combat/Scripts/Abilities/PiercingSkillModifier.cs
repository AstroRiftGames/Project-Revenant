using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PiercingSkillModifier", menuName = "Combat/Skills/Modifiers/Piercing Skill Modifier")]
public class PiercingSkillModifier : SkillModifier
{
    public override void ModifyImpacts(SkillContext context, SkillData skill, List<SkillImpact> impacts)
    {
        SkillHitCollector.ApplyPiercingModifierToImpacts(context, skill, impacts);
    }
}
