using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SplashSkillModifier", menuName = "Combat/Skills/Modifiers/Splash Skill Modifier")]
public class SplashSkillModifier : SkillModifier
{
    public override void ModifyImpacts(SkillContext context, SkillData skill, List<SkillImpact> impacts)
    {
        SkillHitCollector.ApplySplashModifierToImpacts(context, skill, impacts);
    }
}
