using System;

[Obsolete("Use SkillData ImpactPattern/ImpactCenterMode and SkillModifier instead. Kept only for serialized legacy assets.")]
public enum SkillShape
{
    SingleTarget,
    // Deprecated legacy composite. Future: Direct + SplashSkillModifier.
    Splash,
    Area,
    // Deprecated legacy composite. Future: Line + PiercingSkillModifier.
    PiercingLine,
    Line,
    MultiTarget,
    // Deprecated legacy shape. Do not use in new assets.
    // SummonUnitSkillEffect must resolve summon anchors from SkillContext instead.
    SpawnMinions
}
