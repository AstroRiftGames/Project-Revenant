using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CreatureDuelTestController : MonoBehaviour
{
    private const string RoomValidationMessage = "This skill requires room/grid validation. Use SkillRuntimeValidationScene.";

    [SerializeField] private Unit _attacker;
    [SerializeField] private Unit _defender;
    [SerializeField] private Unit[] _secondaryDefenders;
    [SerializeField] private KeyCode _basicAttackKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode _skillKey = KeyCode.Alpha2;
    [SerializeField] private bool _forceSkillChargeForDebug = true;
    [SerializeField] private bool _logActions = true;

    private SkillCaster _subscribedSkillCaster;
    private readonly List<Unit> _registeredUnits = new();

    private void OnEnable()
    {
        ValidateAssignedUnits();
        RefreshRegisteredUnits();
        RefreshSubscriptions();
        LifeController.OnUnitDied += HandleUnitDied;
    }

    private void OnDisable()
    {
        LifeController.OnUnitDied -= HandleUnitDied;
        CreatureDuelDebugUnitRegistry.Unregister(this);
        RefreshSubscriptions(null);
    }

    private void Update()
    {
        if (!Application.isPlaying)
            return;

        RefreshRegisteredUnits();
        RefreshSubscriptions();

        if (_basicAttackKey != KeyCode.None && Input.GetKeyDown(_basicAttackKey))
            TriggerBasicAction();

        if (_skillKey != KeyCode.None && Input.GetKeyDown(_skillKey))
            TriggerSkill();
    }

    public void Configure(
        Unit attacker,
        Unit defender,
        KeyCode basicAttackKey,
        KeyCode skillKey,
        bool forceSkillChargeForDebug,
        bool logActions)
    {
        _attacker = attacker;
        _defender = defender;
        _basicAttackKey = basicAttackKey;
        _skillKey = skillKey;
        _forceSkillChargeForDebug = forceSkillChargeForDebug;
        _logActions = logActions;
        RefreshRegisteredUnits();
        RefreshSubscriptions();
    }

    public bool TriggerBasicAction()
    {
        if (!TryValidateBasicAction(out string rejectionReason))
        {
            Log($"[CreatureDuelTestController] Basic action rejected: {rejectionReason}");
            return false;
        }

        bool executed = _attacker.TryBasicAttackForDebug(_defender);
        Log(
            $"[CreatureDuelTestController] Basic action requested from {FormatUnit(_attacker)} " +
            $"towards {FormatUnit(_defender)}. Result: {(executed ? "executed" : "rejected by runtime state")}. " +
            $"Attacker charge: {FormatCharge(_attacker)}.");
        return executed;
    }

    public bool TriggerSkill()
    {
        if (!TryResolveSkillCaster(out SkillCaster skillCaster, out string skillCasterError))
        {
            Log($"[CreatureDuelTestController] Skill request rejected: {skillCasterError}");
            return false;
        }

        SkillData skill = skillCaster.Skill;
        if (!TryValidateSkillRequest(skillCaster, skill, out Unit requestedTarget, out string rejectionReason))
        {
            Log($"[CreatureDuelTestController] Skill request rejected: {rejectionReason}");
            return false;
        }

        if (_forceSkillChargeForDebug)
            skillCaster.AddAbilityCharge(skillCaster.MaxCharge);

        bool castStarted = skillCaster.TryUse(requestedTarget);
        Log(
            $"[CreatureDuelTestController] Skill '{ResolveSkillLabel(skill)}' requested from {FormatUnit(_attacker)} " +
            $"towards {FormatUnit(requestedTarget)}. Result: {(castStarted ? "cast started or completed" : "cast rejected")}. " +
            $"Charge: {skillCaster.CurrentCharge:F1}/{skillCaster.MaxCharge:F1}.");
        return castStarted;
    }

    private bool TryValidateBasicAction(out string rejectionReason)
    {
        rejectionReason = null;

        if (_attacker == null)
        {
            rejectionReason = "no attacker assigned.";
            return false;
        }

        if (_defender == null)
        {
            rejectionReason = "no defender assigned.";
            return false;
        }

        if (!_attacker.IsAlive)
        {
            rejectionReason = "attacker is dead.";
            return false;
        }

        if (!_defender.IsAlive)
        {
            rejectionReason = "defender is dead.";
            return false;
        }

        IBasicAction action = _attacker.Action;
        if (action == null)
        {
            rejectionReason = "attacker has no basic action.";
            return false;
        }

        if (!action.IsValidTarget(_attacker, _defender))
        {
            rejectionReason = BuildBasicActionTargetRejectionReason(action, _attacker, _defender);
            return false;
        }

        if (!action.IsInRange(_attacker, _defender))
        {
            rejectionReason = "defender is out of basic action range.";
            return false;
        }

        if (!action.CanExecute(_attacker, _defender))
        {
            rejectionReason = "basic action is blocked by cooldown or runtime state.";
            return false;
        }

        return true;
    }

    private bool TryResolveSkillCaster(out SkillCaster skillCaster, out string rejectionReason)
    {
        skillCaster = null;
        rejectionReason = null;

        if (_attacker == null)
        {
            rejectionReason = "no attacker assigned.";
            return false;
        }

        skillCaster = _attacker.GetComponent<SkillCaster>();
        if (skillCaster == null)
        {
            rejectionReason = "attacker has no SkillCaster.";
            return false;
        }

        return true;
    }

    private bool TryValidateSkillRequest(
        SkillCaster skillCaster,
        SkillData skill,
        out Unit requestedTarget,
        out string rejectionReason)
    {
        requestedTarget = null;
        rejectionReason = null;

        if (_attacker == null)
        {
            rejectionReason = "no attacker assigned.";
            return false;
        }

        if (!_attacker.IsAlive)
        {
            rejectionReason = "attacker is dead.";
            return false;
        }

        if (skillCaster == null)
        {
            rejectionReason = "attacker has no SkillCaster.";
            return false;
        }

        if (skill == null)
        {
            rejectionReason = "attacker has no skill assigned.";
            return false;
        }

        if (!IsSkillSupportedWithoutRoomGrid(skill, out string compatibilityReason))
        {
            rejectionReason = $"{RoomValidationMessage} Reason: {compatibilityReason}";
            return false;
        }

        requestedTarget = ResolveRequestedSkillTarget(skill);
        if (RequiresTargetUnit(skill) && requestedTarget == null)
        {
            rejectionReason = "the assigned skill needs a target unit but none was available.";
            return false;
        }

        if (requestedTarget != null && !UnitTargetValidator.IsSkillTargetSelectable(_attacker, requestedTarget, skill))
        {
            rejectionReason = BuildSkillTargetRejectionReason(skill, _attacker, requestedTarget);
            return false;
        }

        if (requestedTarget != null && !UnitTargetValidator.IsSkillTargetInRange(_attacker, requestedTarget, skill))
        {
            rejectionReason =
                $"target {FormatUnit(requestedTarget)} is out of skill range for '{ResolveSkillLabel(skill)}'. " +
                $"Range={skill.RangeInCells}, Distance={Vector3.Distance(_attacker.Position, requestedTarget.Position):F2}.";
            return false;
        }

        return true;
    }

    private Unit ResolveRequestedSkillTarget(SkillData skill)
    {
        if (skill == null || _attacker == null)
            return null;

        switch (skill.PrimaryTargetRequirement)
        {
            case PrimaryTargetRequirement.Self:
                return _attacker;

            case PrimaryTargetRequirement.Hostile:
            case PrimaryTargetRequirement.Ally:
                return _defender;

            default:
                return null;
        }
    }

    private static bool RequiresTargetUnit(SkillData skill)
    {
        if (skill == null)
            return false;

        return skill.PrimaryTargetRequirement == PrimaryTargetRequirement.Hostile ||
               skill.PrimaryTargetRequirement == PrimaryTargetRequirement.Ally ||
               skill.PrimaryTargetRequirement == PrimaryTargetRequirement.Self;
    }

    private static bool IsSkillSupportedWithoutRoomGrid(SkillData skill, out string compatibilityReason)
    {
        compatibilityReason = null;

        if (skill == null)
        {
            compatibilityReason = "no skill assigned.";
            return false;
        }

        if (skill.PrimaryTargetRequirement == PrimaryTargetRequirement.GroundCell)
        {
            compatibilityReason = "ground-targeted skills need room/grid targeting.";
            return false;
        }

        if (skill.PrimaryTargetRequirement == PrimaryTargetRequirement.None)
        {
            compatibilityReason = "targetless skills are not supported in this duel sandbox.";
            return false;
        }

        if (skill.ImpactCenterMode == ImpactCenterMode.TargetCell)
        {
            compatibilityReason = "target-cell impact centers need room/grid targeting.";
            return false;
        }

        if (skill.Modifiers != null)
        {
            for (int i = 0; i < skill.Modifiers.Length; i++)
            {
                SkillModifier modifier = skill.Modifiers[i];
                if (modifier == null)
                    continue;

                if (modifier is SplashSkillModifier)
                    continue;

                if (modifier is PiercingSkillModifier)
                    continue;

                if (modifier is BounceSkillModifier)
                    continue;

                if (modifier is ExplosiveSkillModifier)
                    continue;

                compatibilityReason = $"modifier '{modifier.name}' still requires full room/grid validation.";
                return false;
            }
        }

        if (skill.Effects == null || skill.Effects.Length == 0)
        {
            compatibilityReason = "the skill has no effects configured.";
            return false;
        }

        for (int i = 0; i < skill.Effects.Length; i++)
        {
            SkillEffect effect = skill.Effects[i];
            if (effect == null)
                continue;

            if (effect is DamageSkillEffect)
                continue;

            if (effect is HealSkillEffect)
                continue;

            if (effect is ApplyStatusSkillEffect)
                continue;

            compatibilityReason = $"effect '{effect.GetType().Name}' requires the dedicated room validation scene.";
            return false;
        }

        return true;
    }

    private void RefreshSubscriptions()
    {
        RefreshSubscriptions(_attacker != null ? _attacker.GetComponent<SkillCaster>() : null);
    }

    private void RefreshSubscriptions(SkillCaster nextCaster)
    {
        if (ReferenceEquals(_subscribedSkillCaster, nextCaster))
            return;

        if (_subscribedSkillCaster != null)
            _subscribedSkillCaster.SkillUsed -= HandleSkillUsed;

        _subscribedSkillCaster = nextCaster;

        if (_subscribedSkillCaster != null)
            _subscribedSkillCaster.SkillUsed += HandleSkillUsed;
    }

    private void HandleSkillUsed(Unit casterUnit, SkillData skill, Unit popupAnchor)
    {
        if (!_logActions)
            return;

        Log(
            $"[CreatureDuelTestController] Skill '{ResolveSkillLabel(skill)}' completed on {FormatUnit(casterUnit)} " +
            $"with popup anchor {FormatUnit(popupAnchor)}.");
    }

    private void HandleUnitDied(Unit deadUnit)
    {
        if (!_logActions || deadUnit == null)
            return;

        if (!ReferenceEquals(deadUnit, _attacker) && !ReferenceEquals(deadUnit, _defender))
        {
            bool isSecondaryDefender = false;
            if (_secondaryDefenders != null)
            {
                for (int i = 0; i < _secondaryDefenders.Length; i++)
                {
                    if (ReferenceEquals(_secondaryDefenders[i], deadUnit))
                    {
                        isSecondaryDefender = true;
                        break;
                    }
                }
            }

            if (!isSecondaryDefender)
                return;
        }

        Log($"[CreatureDuelTestController] Unit died: {FormatUnit(deadUnit)}.");
    }

    private void Log(string message)
    {
        if (_logActions)
            Debug.Log(message, this);
    }

    private void ValidateAssignedUnits()
    {
        ValidateAssignedUnit(_attacker, "attacker");
        ValidateAssignedUnit(_defender, "defender");

        if (_secondaryDefenders == null)
            return;

        for (int i = 0; i < _secondaryDefenders.Length; i++)
            ValidateAssignedUnit(_secondaryDefenders[i], $"secondaryDefender[{i}]");
    }

    private void ValidateAssignedUnit(Unit unit, string roleLabel)
    {
        if (unit == null)
            return;

        if (unit.GetComponent<LifeController>() == null)
            Debug.LogWarning($"[CreatureDuelTestController] Assigned {roleLabel} {FormatUnit(unit)} is missing LifeController.", this);

        if (unit.GetComponent<RecruitableUnitState>() == null)
            Debug.LogWarning($"[CreatureDuelTestController] Assigned {roleLabel} {FormatUnit(unit)} is missing RecruitableUnitState.", this);

        if (unit.GetComponent<UnitDeathHandler>() == null)
            Debug.LogWarning($"[CreatureDuelTestController] Assigned {roleLabel} {FormatUnit(unit)} is missing UnitDeathHandler.", this);

        if (unit.GetComponent<UnitMovement>() == null)
            Debug.LogWarning($"[CreatureDuelTestController] Assigned {roleLabel} {FormatUnit(unit)} is missing UnitMovement.", this);

        if (unit.GetComponent<UnitCombat>() == null)
            Debug.LogWarning($"[CreatureDuelTestController] Assigned {roleLabel} {FormatUnit(unit)} is missing UnitCombat.", this);

        if (unit.Action == null)
            Debug.LogWarning($"[CreatureDuelTestController] Assigned {roleLabel} {FormatUnit(unit)} resolved no basic action.", this);

        if (unit.GetUnitData() == null)
            Debug.LogWarning($"[CreatureDuelTestController] Assigned {roleLabel} {FormatUnit(unit)} has no UnitData assigned.", this);
    }

    private static string BuildBasicActionTargetRejectionReason(IBasicAction action, Unit attacker, Unit defender)
    {
        if (action == null)
            return "attacker has no basic action.";

        if (attacker == null || defender == null)
            return "attacker or defender is missing.";

        if (!defender.gameObject.activeInHierarchy)
            return $"defender {FormatUnit(defender)} is inactive.";

        if (!defender.IsAlive)
            return $"defender {FormatUnit(defender)} is dead.";

        if (action.TargetRelation == TargetRelation.Hostile && !attacker.IsHostileTo(defender))
        {
            return
                $"basic action expects a hostile target but attacker {FormatUnit(attacker)} and defender {FormatUnit(defender)} " +
                $"share team '{attacker.Team}'. Use an opposite-team target.";
        }

        if (action.TargetRelation == TargetRelation.Ally && attacker.IsHostileTo(defender))
        {
            return
                $"basic action expects an ally target but attacker {FormatUnit(attacker)} and defender {FormatUnit(defender)} " +
                "are hostile to each other. Use a same-team target.";
        }

        if (action.RequiresInjuredTarget && defender.CurrentHealth >= defender.MaxHealth)
        {
            return
                $"basic action expects an injured ally target but defender {FormatUnit(defender)} is at full health " +
                $"({defender.CurrentHealth}/{defender.MaxHealth}).";
        }

        return
            $"defender {FormatUnit(defender)} failed basic action target rules. " +
            $"Relation={action.TargetRelation}, RequiresInjured={action.RequiresInjuredTarget}, " +
            $"AttackerTeam={attacker.Team}, DefenderTeam={defender.Team}.";
    }

    private void RefreshRegisteredUnits()
    {
        _registeredUnits.Clear();
        AddRegisteredUnit(_attacker);
        AddRegisteredUnit(_defender);

        if (_secondaryDefenders != null)
        {
            for (int i = 0; i < _secondaryDefenders.Length; i++)
                AddRegisteredUnit(_secondaryDefenders[i]);
        }

        CreatureDuelDebugUnitRegistry.Register(this, _registeredUnits);
    }

    private void AddRegisteredUnit(Unit unit)
    {
        if (unit == null)
            return;

        for (int i = 0; i < _registeredUnits.Count; i++)
        {
            if (ReferenceEquals(_registeredUnits[i], unit))
                return;
        }

        _registeredUnits.Add(unit);
    }

    private static string BuildSkillTargetRejectionReason(SkillData skill, Unit attacker, Unit target)
    {
        if (skill == null || attacker == null || target == null)
            return "skill target validation failed because attacker, skill or target is missing.";

        if (!target.gameObject.activeInHierarchy)
            return $"target {FormatUnit(target)} is inactive for skill '{ResolveSkillLabel(skill)}'.";

        if (!target.IsAlive)
            return $"target {FormatUnit(target)} is dead for skill '{ResolveSkillLabel(skill)}'.";

        switch (skill.PrimaryTargetRequirement)
        {
            case PrimaryTargetRequirement.Hostile:
                if (!attacker.IsHostileTo(target))
                {
                    return
                        $"skill '{ResolveSkillLabel(skill)}' expects a hostile target but attacker {FormatUnit(attacker)} " +
                        $"and target {FormatUnit(target)} share team '{attacker.Team}'.";
                }
                break;

            case PrimaryTargetRequirement.Ally:
                if (attacker.IsHostileTo(target))
                {
                    return
                        $"skill '{ResolveSkillLabel(skill)}' expects an ally target but attacker {FormatUnit(attacker)} " +
                        $"and target {FormatUnit(target)} are hostile to each other.";
                }

                if (skill.Requirements != null && skill.Requirements.RequiresInjuredTarget && target.CurrentHealth >= target.MaxHealth)
                {
                    return
                        $"skill '{ResolveSkillLabel(skill)}' expects an injured ally target but {FormatUnit(target)} " +
                        $"is at full health ({target.CurrentHealth}/{target.MaxHealth}).";
                }
                break;

            case PrimaryTargetRequirement.Self:
                if (!ReferenceEquals(attacker, target))
                {
                    return
                        $"skill '{ResolveSkillLabel(skill)}' is self-targeted and must use the attacker as target.";
                }
                break;
        }

        return
            $"target {FormatUnit(target)} failed skill target rules for '{ResolveSkillLabel(skill)}'. " +
            $"PrimaryTargetRequirement={skill.PrimaryTargetRequirement}, AttackerTeam={attacker.Team}, TargetTeam={target.Team}.";
    }

    private static string ResolveSkillLabel(SkillData skill)
    {
        if (skill == null)
            return "None";

        return !string.IsNullOrWhiteSpace(skill.DisplayName)
            ? skill.DisplayName
            : skill.name;
    }

    private static string FormatCharge(Unit unit)
    {
        if (unit == null)
            return "NoUnit";

        SkillCaster skillCaster = unit.GetComponent<SkillCaster>();
        if (skillCaster == null)
            return "NoSkillCaster";

        return $"{skillCaster.CurrentCharge:F1}/{skillCaster.MaxCharge:F1}";
    }

    private static string FormatUnit(Unit unit)
    {
        if (unit == null)
            return "[None]";

        string unitId = !string.IsNullOrWhiteSpace(unit.Id) ? unit.Id : "NoUnitId";
        return $"[{unit.name}#{unit.GetInstanceID()}|{unitId}]";
    }
}
