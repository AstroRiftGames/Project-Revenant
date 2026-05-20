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
    public static bool TryCreateForSkill(SkillRequirements requirements, out TargetingPolicy policy)
    {
        return TryCreateForPrimarySkillTarget(requirements, out policy);
    }

    #endregion

    #region Private Helpers

    private static bool TryCreateForPrimarySkillTarget(SkillRequirements requirements, out TargetingPolicy policy)
    {
        SkillTargetRequirement primaryTargetRequirement = requirements != null
            ? requirements.TargetRequirement
            : SkillTargetRequirement.Any;
        if (primaryTargetRequirement == SkillTargetRequirement.NoTarget ||
            primaryTargetRequirement == SkillTargetRequirement.GroundCell)
        {
            policy = default;
            return false;
        }

        policy = new TargetingPolicy(
            ResolveRelationship(primaryTargetRequirement),
            requiresTarget: true,
            allowSelf: primaryTargetRequirement == SkillTargetRequirement.Any || primaryTargetRequirement == SkillTargetRequirement.Self,
            requireSelf: primaryTargetRequirement == SkillTargetRequirement.Self,
            requireInjured: requirements != null && requirements.RequiresInjuredTarget);
        return true;
    }

    private static TargetRelation ResolveRelationship(SkillTargetRequirement targetRequirement)
    {
        return targetRequirement switch
        {
            SkillTargetRequirement.Hostile => TargetRelation.Hostile,
            SkillTargetRequirement.Ally => TargetRelation.Ally,
            _ => TargetRelation.Any
        };
    }

    #endregion
}
