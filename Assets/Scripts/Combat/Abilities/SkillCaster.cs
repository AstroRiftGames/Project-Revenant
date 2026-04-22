using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Unit))]
public class SkillCaster : MonoBehaviour
{
    [SerializeField] private SkillData _overrideSkill;
    [SerializeField] private bool _debugLogs;

    private Unit _unit;
    private SkillData _resolvedSkill;
    private readonly SkillState _state = new();

    public SkillData Skill => ResolveSkill();
    public bool HasSkill => Skill != null;
    public float CurrentCooldown => _state.RemainingCooldown;
    public float MaxCooldown => Skill != null ? Skill.Cooldown : 0f;
    public Sprite Icon => Skill != null ? Skill.Icon : null;

    private void Awake()
    {
        _unit = GetComponent<Unit>();
        ResolveSkill();
    }

    public bool TryUse(Unit combatTarget)
    {
        LogDebug($"[SkillCaster] {FormatOwnerIdentity()} attempting skill. Combat target: {FormatUnitName(combatTarget)}.");

        SkillData skill = ResolveSkill();
        if (_unit == null)
        {
            LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: owner unit was not resolved.");
            return false;
        }

        if (skill == null)
        {
            LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: no skill assigned.");
            return false;
        }

        if (!_state.IsReady)
        {
            LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: '{skill.DisplayName}' is on cooldown for {_state.RemainingCooldown:F2}s.");
            return false;
        }

        Unit primaryTarget = ResolvePrimaryTarget(skill, combatTarget);
        if (primaryTarget == null)
        {
            LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: '{skill.DisplayName}' resolved no target for mode {skill.TargetMode}.");
            return false;
        }

        if (!IsTargetValid(skill, primaryTarget))
        {
            LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: '{skill.DisplayName}' target {FormatUnitName(primaryTarget)} failed requirements.");
            return false;
        }

        if (!IsInRange(skill, primaryTarget))
        {
            LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: '{skill.DisplayName}' target {FormatUnitName(primaryTarget)} is out of range.");
            return false;
        }

        SkillCastContext context = new(_unit, skill, primaryTarget);
        Unit resolvedTarget = ResolveShapedTarget(context);
        if (resolvedTarget == null)
        {
            LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: '{skill.DisplayName}' shape '{skill.Shape}' produced no target.");
            return false;
        }

        if (!ApplyEffects(skill, context, resolvedTarget))
        {
            LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: '{skill.DisplayName}' applied no effects to {FormatUnitName(resolvedTarget)}.");
            return false;
        }

        _state.StartCooldown(skill.Cooldown);
        LogDebug($"[SkillCaster] {FormatOwnerIdentity()} used '{skill.DisplayName}' on {FormatUnitName(resolvedTarget)}.");
        LogDebug($"[SkillCaster] {FormatOwnerIdentity()} started cooldown for '{skill.DisplayName}': {skill.Cooldown:F2}s.");
        return true;
    }

    public void ResetState()
    {
        _state.Reset();
    }

    private SkillData ResolveSkill()
    {
        if (_overrideSkill != null)
            return _resolvedSkill = _overrideSkill;

        if (_resolvedSkill != null)
            return _resolvedSkill;

        UnitData unitData = _unit != null ? _unit.GetUnitData() : null;
        _resolvedSkill = unitData != null ? unitData.skill : null;
        return _resolvedSkill;
    }

    private Unit ResolvePrimaryTarget(SkillData skill, Unit combatTarget)
    {
        if (skill == null || _unit == null)
            return null;

        return skill.TargetMode switch
        {
            SkillTargetMode.Self => _unit,
            _ => combatTarget
        };
    }

    private bool IsTargetValid(SkillData skill, Unit primaryTarget)
    {
        if (skill == null)
            return false;

        SkillRequirements requirements = skill.Requirements;
        return requirements == null || requirements.AreMet(_unit, primaryTarget);
    }

    private bool IsInRange(SkillData skill, Unit primaryTarget)
    {
        if (_unit == null || skill == null)
            return false;

        if (primaryTarget == null)
            return !ResolveRequiresRangeCheck(skill);

        int rangeInCells = skill.RangeInCells;
        if (skill.TargetMode == SkillTargetMode.Self)
            return true;

        RoomGrid grid = _unit.RoomContext != null ? _unit.RoomContext.RoomGrid : null;
        if (grid == null)
        {
            float distance = Vector3.Distance(_unit.Position, primaryTarget.Position);
            return distance <= Mathf.Max(0f, rangeInCells);
        }

        Vector3Int selfCell = ResolveUnitCell(grid, _unit);
        Vector3Int targetCell = ResolveUnitCell(grid, primaryTarget);
        return GridNavigationUtility.IsWithinCellRange(selfCell, targetCell, rangeInCells);
    }

    private static bool ResolveRequiresRangeCheck(SkillData skill)
    {
        return skill != null && skill.Requirements != null && skill.Requirements.requiresTarget;
    }

    private static Unit ResolveShapedTarget(SkillCastContext context)
    {
        if (context == null || context.Skill == null)
            return null;

        return context.Skill.Shape switch
        {
            SkillShape.SingleTarget => context.PrimaryTarget,
            _ => null
        };
    }

    private static bool ApplyEffects(SkillData skill, SkillCastContext context, Unit resolvedTarget)
    {
        if (skill == null || context == null || resolvedTarget == null)
            return false;

        SkillEffect[] effects = skill.Effects;
        if (effects == null || effects.Length == 0)
            return false;

        bool anyApplied = false;

        for (int i = 0; i < effects.Length; i++)
        {
            SkillEffect effect = effects[i];
            if (effect == null)
                continue;

            anyApplied |= effect.Apply(context, resolvedTarget);
        }

        return anyApplied;
    }

    private static Vector3Int ResolveUnitCell(RoomGrid grid, Unit unit)
    {
        if (grid == null || unit == null)
            return Vector3Int.zero;

        UnitMovement movement = unit.GetComponent<UnitMovement>();
        if (movement != null && movement.TryGetLogicalCell(out Vector3Int cell))
            return cell;

        return grid.WorldToCell(unit.Position);
    }

    private void LogDebug(string message)
    {
        if (_debugLogs)
            Debug.Log(message, this);
    }

    private string FormatOwnerIdentity()
    {
        Unit owner = _unit;
        string ownerName = owner != null ? owner.name : name;
        int ownerInstanceId = owner != null ? owner.GetInstanceID() : GetInstanceID();
        string unitId = owner != null && !string.IsNullOrWhiteSpace(owner.Id) ? owner.Id : "NoUnitId";
        return $"[{ownerName}#{ownerInstanceId}|{unitId}]";
    }

    private static string FormatUnitName(Unit unit)
    {
        if (unit == null)
            return "[None]";

        string unitId = !string.IsNullOrWhiteSpace(unit.Id) ? unit.Id : "NoUnitId";
        return $"[{unit.name}#{unit.GetInstanceID()}|{unitId}]";
    }
}
