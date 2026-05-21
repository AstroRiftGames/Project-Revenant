public readonly struct TargetingPolicy
{
    #region Runtime Policy

    public TargetingPolicy(
        TargetRelation relationship,
        bool requiresTarget = true,
        bool allowSelf = false,
        bool requireSelf = false,
        bool allowDead = false,
        bool allowInvisible = false,
        bool requireInjured = false,
        bool requireSameRoom = true,
        bool requireActive = true,
        bool requireDetectable = true)
    {
        Relationship = relationship;
        RequiresTarget = requiresTarget;
        AllowSelf = allowSelf;
        RequireSelf = requireSelf;
        AllowDead = allowDead;
        AllowInvisible = allowInvisible;
        RequireInjured = requireInjured;
        RequireSameRoom = requireSameRoom;
        RequireActive = requireActive;
        RequireDetectable = requireDetectable;
    }

    public TargetRelation Relationship { get; }
    public bool RequiresTarget { get; }
    public bool AllowSelf { get; }
    public bool RequireSelf { get; }
    public bool AllowDead { get; }
    public bool AllowInvisible { get; }
    public bool RequireInjured { get; }
    public bool RequireSameRoom { get; }
    public bool RequireActive { get; }
    public bool RequireDetectable { get; }

    #endregion

    #region Generic Policies

    public static TargetingPolicy ForRelationship(TargetRelation relationship, bool allowSelf = false)
    {
        return new TargetingPolicy(relationship, allowSelf: allowSelf);
    }

    public static TargetingPolicy ForBasicAction(IBasicAction action)
    {
        return action != null
            ? ForBasicAction(action.TargetRelation, action.RequiresInjuredTarget)
            : ForRelationship(TargetRelation.Hostile);
    }

    public static TargetingPolicy ForBasicAction(TargetRelation relationship, bool requiresInjuredTarget = false)
    {
        return new TargetingPolicy(
            relationship,
            allowSelf: false,
            requireInjured: requiresInjuredTarget);
    }

    #endregion

    #region Skill-Derived Policies

    // Primary target policy derived from the documented skill type contract.
    public static bool TryCreateForSkill(SkillData skill, out TargetingPolicy policy)
    {
        return TryCreateForPrimarySkillTarget(skill, out policy);
    }

    #endregion

    #region Private Helpers

    private static bool TryCreateForPrimarySkillTarget(SkillData skill, out TargetingPolicy policy)
    {
        if (skill == null || !skill.TryValidateDeclarativeContract(out _))
        {
            policy = default;
            return false;
        }

        PrimaryTargetRequirement primaryTargetRequirement = skill.PrimaryTargetRequirement;
        if (primaryTargetRequirement == PrimaryTargetRequirement.None ||
            primaryTargetRequirement == PrimaryTargetRequirement.GroundCell)
        {
            policy = default;
            return false;
        }

        policy = new TargetingPolicy(
            ResolveRelationship(primaryTargetRequirement),
            requiresTarget: true,
            allowSelf: primaryTargetRequirement == PrimaryTargetRequirement.Self,
            requireSelf: primaryTargetRequirement == PrimaryTargetRequirement.Self,
            requireInjured: skill.Requirements != null && skill.Requirements.RequiresInjuredTarget);
        return true;
    }

    private static TargetRelation ResolveRelationship(PrimaryTargetRequirement targetRequirement)
    {
        return targetRequirement switch
        {
            PrimaryTargetRequirement.Hostile => TargetRelation.Hostile,
            PrimaryTargetRequirement.Ally => TargetRelation.Ally,
            _ => TargetRelation.Any
        };
    }

    #endregion
}
