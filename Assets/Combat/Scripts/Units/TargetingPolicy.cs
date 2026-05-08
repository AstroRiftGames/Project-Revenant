public readonly struct TargetingPolicy
{
    public TargetingPolicy(
        RequiredTargetRelationship relationship,
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

    public RequiredTargetRelationship Relationship { get; }
    public bool RequiresTarget { get; }
    public bool AllowSelf { get; }
    public bool RequireSelf { get; }
    public bool AllowDead { get; }
    public bool AllowInvisible { get; }
    public bool RequireInjured { get; }
    public bool RequireSameRoom { get; }
    public bool RequireActive { get; }
    public bool RequireDetectable { get; }

    public static TargetingPolicy ForRelationship(RequiredTargetRelationship relationship, bool allowSelf = false)
    {
        return new TargetingPolicy(relationship, allowSelf: allowSelf);
    }

    public static TargetingPolicy ForBasicAction(IBasicAction action)
    {
        return action != null
            ? ForBasicAction(action.RequiredTargetRelationship, action.RequiresInjuredTarget)
            : ForRelationship(RequiredTargetRelationship.Hostile);
    }

    public static TargetingPolicy ForBasicAction(RequiredTargetRelationship relationship, bool requiresInjuredTarget = false)
    {
        return new TargetingPolicy(
            relationship,
            allowSelf: false,
            requireInjured: requiresInjuredTarget);
    }

    public static bool TryCreateForSkill(SkillRequirements requirements, out TargetingPolicy policy)
    {
        SkillTargetRequirement targetRequirement = requirements != null
            ? requirements.TargetRequirement
            : SkillTargetRequirement.Any;
        if (targetRequirement == SkillTargetRequirement.NoTarget || targetRequirement == SkillTargetRequirement.GroundCell)
        {
            policy = default;
            return false;
        }

        policy = new TargetingPolicy(
            ResolveRelationship(targetRequirement),
            requiresTarget: requirements == null || requirements.requiresTarget,
            allowSelf: targetRequirement == SkillTargetRequirement.Any || targetRequirement == SkillTargetRequirement.Self,
            requireSelf: targetRequirement == SkillTargetRequirement.Self,
            requireInjured: requirements != null && requirements.mustTargetInjured);
        return true;
    }

    public static bool TryCreateForImpact(SkillRequirements requirements, SkillTargetMode targetMode, out TargetingPolicy policy)
    {
        if (!TryCreateForSkill(requirements, out policy))
            return false;

        if (targetMode == SkillTargetMode.Self)
        {
            policy = new TargetingPolicy(
                policy.Relationship,
                policy.RequiresTarget,
                allowSelf: true,
                policy.RequireSelf,
                policy.AllowDead,
                policy.AllowInvisible,
                policy.RequireInjured,
                policy.RequireSameRoom,
                policy.RequireActive,
                policy.RequireDetectable);
        }

        return true;
    }

    private static RequiredTargetRelationship ResolveRelationship(SkillTargetRequirement targetRequirement)
    {
        return targetRequirement switch
        {
            SkillTargetRequirement.Hostile => RequiredTargetRelationship.Hostile,
            SkillTargetRequirement.Ally => RequiredTargetRelationship.Ally,
            _ => RequiredTargetRelationship.Any
        };
    }
}
