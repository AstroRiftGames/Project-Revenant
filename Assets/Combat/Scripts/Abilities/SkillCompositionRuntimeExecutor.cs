using System.Collections.Generic;
using UnityEngine;

public static class SkillCompositionRuntimeExecutor
{
    private const string LogTag = "[SkillCompositionRuntimeExecutor]";

    // ============================================================
    // PUBLIC ENTRY POINTS
    // ============================================================

    public static bool CanExecuteComposition(SkillData skill, out string gapMessage)
    {
        if (skill == null)
        {
            gapMessage = "Skill is null";
            return false;
        }

        int compEffectCount = skill.CompositionEffects != null ? skill.CompositionEffects.Length : 0;
        if (compEffectCount == 0)
        {
            gapMessage = "No composition effects defined.";
            return false;
        }

        if (skill.CompositionEffects != null)
        {
            foreach (var effect in skill.CompositionEffects)
            {
                if (effect.EffectKind == SkillEffectKind.Summon)
                {
                    if (effect.SummonedUnit == null)
                    {
                        gapMessage = "Summon effect is missing SummonedUnit reference.";
                        return false;
                    }
                }
                else if (effect.EffectKind == SkillEffectKind.Shield)
                {
                    if (effect.Duration <= 0f)
                    {
                        gapMessage = "Shield effect duration is <= 0.";
                        return false;
                    }
                }
                else if (effect.EffectKind == SkillEffectKind.Haste ||
                         effect.EffectKind == SkillEffectKind.StrengthBuff ||
                         effect.EffectKind == SkillEffectKind.Slow ||
                         effect.EffectKind == SkillEffectKind.Stun ||
                         effect.EffectKind == SkillEffectKind.PoisonBurn ||
                         effect.EffectKind == SkillEffectKind.Buff ||
                         effect.EffectKind == SkillEffectKind.Debuff ||
                         effect.EffectKind == SkillEffectKind.StatModifierDebuff ||
                         effect.EffectKind == SkillEffectKind.Taunt)
                {
                    if (effect.StatusDefinition == null)
                    {
                        gapMessage = $"Status effect '{effect.EffectKind}' is missing StatusEffectDefinition reference.";
                        return false;
                    }
                }
            }
        }

        // Build lookup for modifier data by kind
        var modifierDataByKind = new Dictionary<SkillModifierKind, SkillCompositionModifierData>();
        if (skill.CompositionModifierData != null)
        {
            foreach (var md in skill.CompositionModifierData)
            {
                if (!modifierDataByKind.ContainsKey(md.ModifierKind))
                    modifierDataByKind[md.ModifierKind] = md;
            }
        }

        if (skill.CompositionModifierKinds != null)
        {
            foreach (var modKind in skill.CompositionModifierKinds)
            {
                switch (modKind)
                {
                    case SkillModifierKind.Bounce:
                        if (!modifierDataByKind.TryGetValue(SkillModifierKind.Bounce, out var bounceData) || bounceData.BounceMaxBounces <= 0)
                        {
                            gapMessage = "Bounce modifier is missing BounceMaxBounces data.";
                            return false;
                        }
                        break;

                    case SkillModifierKind.Explosive:
                        if (!modifierDataByKind.TryGetValue(SkillModifierKind.Explosive, out var explosiveData) || explosiveData.ExplosiveRadiusInCells <= 0)
                        {
                            gapMessage = "Explosive modifier is missing ExplosiveRadiusInCells data.";
                            return false;
                        }
                        break;

                    case SkillModifierKind.Persistent:
                    case SkillModifierKind.Cumulative:
                    case SkillModifierKind.Periodic:
                    case SkillModifierKind.Expandable:
                        gapMessage = $"Modifier '{modKind}' has no runtime implementation.";
                        return false;
                }
            }
        }

        gapMessage = null;
        return true;
    }

    public static bool ExecuteEffects(SkillData skill, SkillContext context, SkillImpact impact)
    {
        if (skill == null || skill.CompositionEffects == null || skill.CompositionEffects.Length == 0)
            return false;

        bool anyApplied = false;
        for (int i = 0; i < skill.CompositionEffects.Length; i++)
        {
            SkillCompositionEffect effect = skill.CompositionEffects[i];
            anyApplied |= ExecuteSingleEffect(effect, skill, context, impact);
        }
        return anyApplied;
    }

    public static void ExecuteModifiers(SkillContext context, SkillData skill, List<SkillImpact> impacts)
    {
        if (skill == null || skill.CompositionModifierKinds == null || skill.CompositionModifierKinds.Length == 0)
            return;

        for (int i = 0; i < skill.CompositionModifierKinds.Length; i++)
        {
            SkillModifierKind kind = skill.CompositionModifierKinds[i];
            SkillCompositionModifierData modifierData = FindModifierData(skill, kind);
            ExecuteSingleModifier(kind, modifierData, context, skill, impacts);
        }
    }

    private static SkillCompositionModifierData FindModifierData(SkillData skill, SkillModifierKind kind)
    {
        if (skill.CompositionModifierData == null)
            return default;

        for (int i = 0; i < skill.CompositionModifierData.Length; i++)
        {
            if (skill.CompositionModifierData[i].ModifierKind == kind)
                return skill.CompositionModifierData[i];
        }

        return default;
    }

    // ============================================================
    // EFFECT EXECUTION
    // ============================================================

    private static bool ExecuteSingleEffect(SkillCompositionEffect effect, SkillData skill, SkillContext context, SkillImpact impact)
    {
        if (effect.EffectKind == SkillEffectKind.Heal && effect.StatusDefinition != null)
            return ExecuteStatusEffect(effect, skill, context, impact);

        switch (effect.EffectKind)
        {
            case SkillEffectKind.Damage:
                return ExecuteDamage(effect, context, impact);
            case SkillEffectKind.Heal:
                return ExecuteHeal(effect, context, impact);
            case SkillEffectKind.Shield:
                return ExecuteShield(effect, context, impact);
            case SkillEffectKind.Knockback:
                return ExecuteKnockback(effect, context, impact);
            case SkillEffectKind.Summon:
                return ExecuteSummon(effect, skill, context, impact);
            case SkillEffectKind.Haste:
            case SkillEffectKind.StrengthBuff:
            case SkillEffectKind.Slow:
            case SkillEffectKind.Stun:
            case SkillEffectKind.PoisonBurn:
            case SkillEffectKind.Buff:
            case SkillEffectKind.Debuff:
            case SkillEffectKind.StatModifierDebuff:
            case SkillEffectKind.Taunt:
                return ExecuteStatusEffect(effect, skill, context, impact);
            default:
                Debug.LogWarning($"{LogTag} Unhandled SkillEffectKind: {effect.EffectKind}");
                return false;
        }
    }

    private static bool ExecuteDamage(SkillCompositionEffect effect, SkillContext context, SkillImpact impact)
    {
        Unit hitUnit = ResolveTargetUnit(impact);
        Unit caster = context != null ? context.Caster : null;
        if (caster == null || !IsCombatAliveUnit(hitUnit))
            return false;

        int damageAmount = Mathf.Max(0, effect.Value);
        hitUnit.TakeDamage(damageAmount, caster);

        return true;
    }

    private static bool ExecuteHeal(SkillCompositionEffect effect, SkillContext context, SkillImpact impact)
    {
        Unit hitUnit = ResolveTargetUnit(impact);
        Unit caster = context != null ? context.Caster : null;
        if (caster == null || !IsCombatAliveUnit(hitUnit))
            return false;

        if (hitUnit.CurrentHealth >= hitUnit.MaxHealth)
            return false;

        hitUnit.Heal(Mathf.Max(0, effect.Value), caster);
        return true;
    }

    private static bool ExecuteShield(SkillCompositionEffect effect, SkillContext context, SkillImpact impact)
    {
        Unit hitUnit = ResolveTargetUnit(impact);
        Unit caster = context != null ? context.Caster : null;
        if (caster == null || !IsCombatAliveUnit(hitUnit))
            return false;

        int shieldAmount = Mathf.Max(0, effect.Value);
        float durationSeconds = Mathf.Max(0f, effect.Duration);
        if (shieldAmount <= 0 || durationSeconds <= 0f)
            return false;

        ShieldController shieldController = hitUnit.GetComponent<ShieldController>();
        if (shieldController == null)
            shieldController = hitUnit.gameObject.AddComponent<ShieldController>();

        shieldController.ApplyShield(shieldAmount, durationSeconds);
        return true;
    }

    private static bool ExecuteKnockback(SkillCompositionEffect effect, SkillContext context, SkillImpact impact)
    {
        Unit hitUnit = ResolveTargetUnit(impact);
        Unit caster = context != null ? context.Caster : null;
        if (caster == null || !IsCombatAliveUnit(hitUnit))
            return false;

        int knockbackCells = Mathf.Max(0, effect.Value);
        if (knockbackCells <= 0)
            return false;

        // Interrupt active cast when knocked back
        hitUnit.GetComponent<SkillCaster>()?.InterruptCast();

        RoomGrid grid = ResolveRoomGrid(context, hitUnit, caster);
        UnitMovement movement = hitUnit.GetComponent<UnitMovement>();
        Unit referenceUnit = ResolveReferenceUnit(context, caster);

        Vector3 knockbackDirection = ResolveKnockbackDirection(hitUnit, referenceUnit);
        knockbackDirection.z = 0f;

        if (knockbackDirection.sqrMagnitude < Mathf.Epsilon)
            return false;

        if (grid != null)
        {
            Vector3Int currentCell = GridUnitCellUtility.ResolveUnitCell(grid, hitUnit);
            int stepX = Mathf.RoundToInt(knockbackDirection.x);
            int stepY = Mathf.RoundToInt(knockbackDirection.y);
            Vector3Int resolvedCell = currentCell;
            Vector3Int previousCell = currentCell;

            for (int i = 1; i <= knockbackCells; i++)
            {
                Vector3Int intermediateCell = currentCell + new Vector3Int(stepX * i, stepY * i, 0);
                if (!grid.IsStepAllowed(previousCell, intermediateCell, hitUnit))
                    break;
                resolvedCell = intermediateCell;
                previousCell = intermediateCell;
            }

            if (movement != null)
            {
                if (!movement.ForceRelocateToCell(resolvedCell))
                    return false;
            }
            else
            {
                hitUnit.transform.position = grid.CellToWorld(resolvedCell);
            }
            return true;
        }

        float worldDistance = knockbackCells;
        Vector3 newPosition = hitUnit.Position + knockbackDirection * worldDistance;
        if (movement != null)
            movement.ForceSyncToWorldPosition(newPosition);
        else
            hitUnit.transform.position = newPosition;

        return true;
    }

    private static bool ExecuteSummon(SkillCompositionEffect effect, SkillData skill, SkillContext context, SkillImpact impact)
    {
        Unit caster = context != null ? context.Caster : null;
        if (caster == null || skill == null)
            return false;

        UnitData summonedUnitData = effect.SummonedUnit;
        if (summonedUnitData == null || summonedUnitData.unitPrefab == null)
            return false;

        RoomContext roomContext = ResolveRoomContext(context, caster);
        RoomGrid grid = roomContext != null ? roomContext.RoomGrid : null;
        if (roomContext == null || grid == null)
            return false;

        Vector3Int casterCell = GridUnitCellUtility.ResolveUnitCell(grid, caster);
        Vector3Int desiredCell = ResolveSummonDesiredCell(context, grid, caster, impact, effect.SummonAnchorMode);
        int spawnRangeInCells = Mathf.Max(0, effect.Value);

        if (!grid.TryFindWalkableCellInRange(desiredCell, casterCell, spawnRangeInCells, null, out Vector3Int spawnCell))
            return false;

        Vector3 spawnPosition = grid.CellToWorld(spawnCell);
        GameObject instance = Object.Instantiate(summonedUnitData.unitPrefab, spawnPosition, Quaternion.identity, roomContext.transform);
        if (!instance.TryGetComponent(out Unit summonedUnit))
        {
            Object.Destroy(instance);
            return false;
        }

        CombatSummonedUnitRuntimeMarker marker = instance.GetComponent<CombatSummonedUnitRuntimeMarker>();
        if (marker == null)
            marker = instance.AddComponent<CombatSummonedUnitRuntimeMarker>();

        TemporaryCombatUnit temporaryUnit = instance.GetComponent<TemporaryCombatUnit>();
        if (temporaryUnit == null)
            temporaryUnit = instance.AddComponent<TemporaryCombatUnit>();

        temporaryUnit.Initialize(caster, skill);
        summonedUnit.SetAffiliation(caster.Team, caster.Faction);

        if (instance.TryGetComponent(out UnitMovement movement))
        {
            if (!movement.AttachToGridAtCell(grid, spawnCell))
            {
                movement.SetGrid(grid);
                if (!movement.ForceSyncToCell(spawnCell))
                {
                    Object.Destroy(instance);
                    return false;
                }
            }
        }
        else
        {
            summonedUnit.SnapToGrid();
        }

        return true;
    }

    private static bool ExecuteStatusEffect(SkillCompositionEffect effect, SkillData skill, SkillContext context, SkillImpact impact)
    {
        Unit hitUnit = ResolveTargetUnit(impact);
        Unit caster = context != null ? context.Caster : null;
        if (caster == null || hitUnit == null)
            return false;

        StatusEffectDefinition statusDef = effect.StatusDefinition;
        if (statusDef != null)
        {
            StatusEffectController statusController = hitUnit.StatusEffects;
            if (statusController == null)
                return false;

            var application = new StatusEffectApplication(hitUnit, caster, skill, statusDef);
            return statusController.TryApply(application);
        }

        Debug.LogError($"{LogTag} Status effect '{effect.EffectKind}' on skill '{skill?.name}' has no StatusEffectDefinition reference in composition.");
        return false;
    }

    // ============================================================
    // MODIFIER EXECUTION
    // ============================================================

    private static void ExecuteSingleModifier(SkillModifierKind kind, SkillCompositionModifierData modifierData, SkillContext context, SkillData skill, List<SkillImpact> impacts)
    {
        switch (kind)
        {
            case SkillModifierKind.Splash:
                SkillHitCollector.ApplySplashModifierToImpacts(context, skill, impacts);
                break;

            case SkillModifierKind.Penetrating:
                if (skill.ImpactPattern == ImpactPattern.Line)
                    SkillHitCollector.ApplyPiercingModifierToImpacts(context, skill, impacts);
                break;

            case SkillModifierKind.Bounce:
                if (modifierData.ModifierKind == SkillModifierKind.Bounce && modifierData.BounceMaxBounces > 0)
                {
                    ApplyBounceWithParams(context, skill, impacts,
                        modifierData.BounceMaxBounces,
                        modifierData.BounceRangeInCells,
                        modifierData.BounceCanBounceToPrimaryTargetAgain);
                }
                break;

            case SkillModifierKind.Explosive:
                if (modifierData.ModifierKind == SkillModifierKind.Explosive && modifierData.ExplosiveRadiusInCells > 0)
                {
                    ApplyExplosiveWithParams(context, skill, impacts,
                        modifierData.ExplosiveRadiusInCells,
                        modifierData.ExplosiveIncludePrimaryImpactTarget);
                }
                break;

            case SkillModifierKind.Persistent:
            case SkillModifierKind.Cumulative:
            case SkillModifierKind.Periodic:
            case SkillModifierKind.Expandable:
                Debug.LogWarning($"{LogTag} Modifier kind '{kind}' is declared in CompositionModifierKinds but no runtime implementation exists yet.");
                break;

            default:
                Debug.LogWarning($"{LogTag} Unhandled SkillModifierKind: {kind}");
                break;
        }
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private static Unit ResolveTargetUnit(SkillImpact impact)
    {
        return impact != null && impact.HasTargetUnit
            ? impact.TargetUnit
            : null;
    }

    private static bool IsCombatAliveUnit(Unit unit)
    {
        return unit != null &&
               unit.IsAlive &&
               unit.LifecycleState == UnitLifecycleState.Alive;
    }

    // ============================================================
    // HELPERS (mirror KnockbackSkillEffect)
    // ============================================================

    private static RoomGrid ResolveRoomGrid(SkillContext context, Unit target, Unit sourceUnit)
    {
        if (context != null && context.RoomGrid != null)
            return context.RoomGrid;

        RoomGrid targetGrid = target != null ? target.RoomContext?.RoomGrid : null;
        if (targetGrid != null)
            return targetGrid;

        return sourceUnit != null ? sourceUnit.RoomContext?.RoomGrid : null;
    }

    private static Unit ResolveReferenceUnit(SkillContext context, Unit sourceUnit)
    {
        if (context == null)
            return sourceUnit;

        if (context.HasPrimaryTarget)
            return sourceUnit;

        if (context.HasImpactCenterUnit)
            return context.ImpactCenterUnit;

        return sourceUnit;
    }

    private static Vector3 ResolveKnockbackDirection(Unit target, Unit referenceUnit)
    {
        if (target == null || referenceUnit == null)
            return Vector3.zero;

        Vector3 direction = target.Position - referenceUnit.Position;
        direction.z = 0f;

        if (direction.sqrMagnitude < Mathf.Epsilon)
            return Vector3.zero;

        return direction.normalized;
    }

    // ============================================================
    // HELPERS (mirror SummonUnitSkillEffect)
    // ============================================================

    private static RoomContext ResolveRoomContext(SkillContext context, Unit caster)
    {
        if (context != null && context.RoomContext != null)
            return context.RoomContext;

        return caster != null ? caster.RoomContext : null;
    }

    private static Vector3Int ResolveSummonDesiredCell(SkillContext context, RoomGrid grid, Unit caster, SkillImpact impact, SummonAnchorMode anchorMode = SummonAnchorMode.AroundImpactCenter)
    {
        if (grid == null)
            return Vector3Int.zero;

        if (impact != null)
        {
            switch (anchorMode)
            {
                case SummonAnchorMode.AroundCaster:
                    if (caster != null)
                        return GridUnitCellUtility.ResolveUnitCell(grid, caster);
                    break;

                case SummonAnchorMode.AroundPrimaryTarget:
                case SummonAnchorMode.AroundImpactCenter:
                    if (impact.HasTargetUnit)
                        return GridUnitCellUtility.ResolveUnitCell(grid, impact.TargetUnit);
                    if (impact.Kind == SkillImpactKind.AreaPoint)
                        return grid.WorldToCell(impact.WorldPosition);
                    if (impact.HasCell)
                        return new Vector3Int(impact.Cell.x, impact.Cell.y, 0);
                    break;

                case SummonAnchorMode.AtTargetCell:
                    if (impact.HasCell)
                        return new Vector3Int(impact.Cell.x, impact.Cell.y, 0);
                    if (impact.HasTargetUnit)
                        return GridUnitCellUtility.ResolveUnitCell(grid, impact.TargetUnit);
                    if (impact.Kind == SkillImpactKind.AreaPoint)
                        return grid.WorldToCell(impact.WorldPosition);
                    break;
            }
        }

        if (context == null || caster == null)
            return caster != null ? GridUnitCellUtility.ResolveUnitCell(grid, caster) : Vector3Int.zero;

        switch (anchorMode)
        {
            case SummonAnchorMode.AroundCaster:
                return GridUnitCellUtility.ResolveUnitCell(grid, caster);

            case SummonAnchorMode.AroundPrimaryTarget:
                if (context.HasPrimaryTarget)
                    return GridUnitCellUtility.ResolveUnitCell(grid, context.PrimaryTarget);
                return GridUnitCellUtility.ResolveUnitCell(grid, caster);

            case SummonAnchorMode.AroundImpactCenter:
                if (context.HasImpactCenterUnit)
                    return GridUnitCellUtility.ResolveUnitCell(grid, context.ImpactCenterUnit);
                if (context.ImpactCenterWorld != Vector3.zero)
                    return grid.WorldToCell(context.ImpactCenterWorld);
                return GridUnitCellUtility.ResolveUnitCell(grid, caster);

            case SummonAnchorMode.AtTargetCell:
                if (context.HasTargetCell)
                    return new Vector3Int(context.TargetCell.x, context.TargetCell.y, 0);
                return GridUnitCellUtility.ResolveUnitCell(grid, caster);

            default:
                return GridUnitCellUtility.ResolveUnitCell(grid, caster);
        }
    }

    // ============================================================
    // MODIFIER LOGIC (migrated from legacy backend)
    // ============================================================

    private static void ApplyBounceWithParams(SkillContext context, SkillData skill, List<SkillImpact> impacts,
        int maxBounces, int rangeInCells, bool canBounceToPrimaryAgain)
    {
        if (context == null || skill == null || impacts == null || impacts.Count == 0)
            return;

        maxBounces = Mathf.Max(0, maxBounces);
        int bounceRangeInCells = Mathf.Max(0, rangeInCells);
        if (maxBounces <= 0 || bounceRangeInCells <= 0)
            return;

        Unit caster = context.Caster;
        RoomGrid roomGrid = ResolveRoomGridForModifier(context, caster);
        if (caster == null)
        {
            Debug.LogWarning($"[SkillCompositionRuntimeExecutor] Skill '{skill.DisplayName}' could not resolve a caster for bounce impacts.", skill);
            return;
        }

        IReadOnlyList<Unit> roomUnits = caster.GetRoomUnits();
        if (roomUnits == null || roomUnits.Count == 0)
            return;

        int sourceImpactCount = impacts.Count;
        for (int sourceImpactIndex = 0; sourceImpactIndex < sourceImpactCount; sourceImpactIndex++)
        {
            SkillImpact sourceImpact = impacts[sourceImpactIndex];
            if (sourceImpact == null || !sourceImpact.HasTargetUnit || !IsCombatAliveUnit(sourceImpact.TargetUnit))
                continue;

            Unit chainPrimaryUnit = sourceImpact.TargetUnit;
            Unit currentUnit = chainPrimaryUnit;
            int nextChainIndex = sourceImpact.ChainIndex + 1;
            bool primaryTargetRepeated = false;
            var visitedUnits = new List<Unit> { chainPrimaryUnit };

            for (int bounceIndex = 0; bounceIndex < maxBounces; bounceIndex++)
            {
                if (!TrySelectNextBounceTarget(
                        context,
                        roomGrid,
                        roomUnits,
                        impacts,
                        currentUnit,
                        chainPrimaryUnit,
                        bounceRangeInCells,
                        canBounceToPrimaryAgain,
                        visitedUnits,
                        primaryTargetRepeated,
                        out Unit nextUnit,
                        out Vector3Int nextUnitCell))
                {
                    break;
                }

                if (ReferenceEquals(nextUnit, chainPrimaryUnit))
                    primaryTargetRepeated = true;

                SkillImpact bounceImpact = CreateBounceImpact(roomGrid, nextUnit, nextUnitCell, nextChainIndex);
                if (bounceImpact == null)
                    break;

                impacts.Add(bounceImpact);

                if (!ContainsVisitedUnit(visitedUnits, nextUnit))
                    visitedUnits.Add(nextUnit);

                currentUnit = nextUnit;
                nextChainIndex++;
            }
        }
    }

    private static bool TrySelectNextBounceTarget(
        SkillContext context,
        RoomGrid roomGrid,
        IReadOnlyList<Unit> roomUnits,
        List<SkillImpact> impacts,
        Unit currentUnit,
        Unit chainPrimaryUnit,
        int bounceRangeInCells,
        bool canBounceToPrimaryAgain,
        List<Unit> visitedUnits,
        bool primaryTargetRepeated,
        out Unit nextUnit,
        out Vector3Int nextUnitCell)
    {
        nextUnit = null;
        nextUnitCell = default;

        if (context == null || roomUnits == null || currentUnit == null)
            return false;

        Vector3Int currentUnitCell = roomGrid != null
            ? GridUnitCellUtility.ResolveUnitCell(roomGrid, currentUnit)
            : default;
        float bestDistance = float.MaxValue;
        int bestInstanceId = int.MaxValue;

        for (int unitIndex = 0; unitIndex < roomUnits.Count; unitIndex++)
        {
            Unit candidate = roomUnits[unitIndex];
            if (!IsCombatAliveUnit(candidate))
                continue;

            if (ReferenceEquals(candidate, currentUnit))
                continue;

            if (!SkillHitCollector.CanSkillHitUnit(context, candidate))
                continue;

            bool isPrimaryTargetCandidate = ReferenceEquals(candidate, chainPrimaryUnit);
            bool alreadyVisited = ContainsVisitedUnit(visitedUnits, candidate);
            bool canReusePrimaryTarget =
                isPrimaryTargetCandidate &&
                canBounceToPrimaryAgain &&
                !primaryTargetRepeated;

            if (alreadyVisited && !canReusePrimaryTarget)
                continue;

            if (ContainsUnitImpact(impacts, candidate) && !canReusePrimaryTarget)
                continue;

            Vector3Int candidateCell = roomGrid != null
                ? GridUnitCellUtility.ResolveUnitCell(roomGrid, candidate)
                : default;
            if (!IsCandidateWithinBounceRange(roomGrid, currentUnit, currentUnitCell, candidate, candidateCell, bounceRangeInCells))
                continue;

            float distance = ResolveBounceDistance(roomGrid, currentUnit, currentUnitCell, candidate, candidateCell);
            int candidateInstanceId = candidate.GetInstanceID();
            if (distance < bestDistance ||
                (Mathf.Approximately(distance, bestDistance) && candidateInstanceId < bestInstanceId))
            {
                bestDistance = distance;
                bestInstanceId = candidateInstanceId;
                nextUnit = candidate;
                nextUnitCell = candidateCell;
            }
        }

        return nextUnit != null;
    }

    private static bool IsCandidateWithinBounceRange(
        RoomGrid roomGrid,
        Unit currentUnit,
        Vector3Int currentUnitCell,
        Unit candidate,
        Vector3Int candidateCell,
        int bounceRangeInCells)
    {
        if (currentUnit == null || candidate == null)
            return false;

        if (roomGrid != null)
            return GridNavigationUtility.IsWithinCellRange(currentUnitCell, candidateCell, bounceRangeInCells);

        float distance = Vector3.Distance(currentUnit.Position, candidate.Position);
        return distance <= Mathf.Max(0f, bounceRangeInCells);
    }

    private static float ResolveBounceDistance(
        RoomGrid roomGrid,
        Unit currentUnit,
        Vector3Int currentUnitCell,
        Unit candidate,
        Vector3Int candidateCell)
    {
        if (currentUnit == null || candidate == null)
            return float.MaxValue;

        if (roomGrid != null)
            return GridNavigationUtility.GetCellDistance(currentUnitCell, candidateCell);

        return Vector3.Distance(currentUnit.Position, candidate.Position);
    }

    private static SkillImpact CreateBounceImpact(RoomGrid roomGrid, Unit nextUnit, Vector3Int nextUnitCell, int nextChainIndex)
    {
        if (nextUnit == null)
            return null;

        if (roomGrid != null)
        {
            Vector2Int nextCell2D = new(nextUnitCell.x, nextUnitCell.y);
            return SkillImpact.CreateUnit(nextUnit, nextCell2D, nextChainIndex, false);
        }

        return SkillImpact.CreateUnit(nextUnit, nextChainIndex, false);
    }

    private static void ApplyExplosiveWithParams(SkillContext context, SkillData skill, List<SkillImpact> impacts,
        int radiusInCells, bool includePrimaryImpactTarget)
    {
        if (context == null || skill == null || impacts == null || impacts.Count == 0)
            return;

        int explosionRadiusInCells = Mathf.Max(0, radiusInCells);
        if (explosionRadiusInCells <= 0)
            return;

        Unit caster = context.Caster;
        RoomGrid roomGrid = ResolveRoomGridForModifier(context, caster);
        if (caster == null)
        {
            Debug.LogWarning($"[SkillCompositionRuntimeExecutor] Skill '{skill.DisplayName}' could not resolve a caster for bounce impacts.", skill);
            return;
        }

        IReadOnlyList<Unit> roomUnits = caster.GetRoomUnits();
        if (roomUnits == null || roomUnits.Count == 0)
            return;

        int sourceImpactCount = impacts.Count;
        for (int sourceImpactIndex = 0; sourceImpactIndex < sourceImpactCount; sourceImpactIndex++)
        {
            SkillImpact sourceImpact = impacts[sourceImpactIndex];
            if (sourceImpact == null)
                continue;

            if (!TryResolveExplosionCenter(roomGrid, sourceImpact, out Vector3Int explosionCenterCell, out Vector3 explosionCenterWorld))
            {
                Debug.LogWarning($"[SkillCompositionRuntimeExecutor] Skill '{skill.DisplayName}' ignored an impact because no explosion center could be resolved for kind '{sourceImpact.Kind}'.", skill);
                continue;
            }

            for (int unitIndex = 0; unitIndex < roomUnits.Count; unitIndex++)
            {
                Unit candidate = roomUnits[unitIndex];
                if (!IsCombatAliveUnit(candidate))
                    continue;

                if (!SkillHitCollector.CanSkillHitUnit(context, candidate))
                    continue;

                if (!IsCandidateInsideExplosion(roomGrid, explosionCenterCell, explosionCenterWorld, candidate, explosionRadiusInCells))
                    continue;

                bool isPrimaryTargetUnit =
                    sourceImpact.HasTargetUnit &&
                    ReferenceEquals(sourceImpact.TargetUnit, candidate);
                if (isPrimaryTargetUnit && !includePrimaryImpactTarget)
                    continue;

                if (ContainsUnitImpact(impacts, candidate))
                    continue;

                SkillImpact explosiveImpact = CreateExplosiveImpact(roomGrid, candidate, sourceImpact.ChainIndex + 1);
                if (explosiveImpact == null)
                    continue;

                impacts.Add(explosiveImpact);
            }
        }
    }

    private static bool TryResolveExplosionCenter(
        RoomGrid roomGrid,
        SkillImpact impact,
        out Vector3Int explosionCenterCell,
        out Vector3 explosionCenterWorld)
    {
        explosionCenterCell = default;
        explosionCenterWorld = default;
        if (impact == null)
            return false;

        if (impact.HasTargetUnit)
        {
            explosionCenterWorld = impact.TargetUnit.Position;
            if (roomGrid != null)
                explosionCenterCell = GridUnitCellUtility.ResolveUnitCell(roomGrid, impact.TargetUnit);
            return true;
        }

        if (impact.Kind == SkillImpactKind.AreaPoint)
        {
            explosionCenterWorld = impact.WorldPosition;
            if (roomGrid != null)
                explosionCenterCell = roomGrid.WorldToCell(impact.WorldPosition);
            return true;
        }

        if (impact.HasCell && roomGrid != null)
        {
            explosionCenterCell = new Vector3Int(impact.Cell.x, impact.Cell.y, 0);
            explosionCenterWorld = roomGrid.CellToWorld(explosionCenterCell);
            return true;
        }

        return false;
    }

    private static bool IsCandidateInsideExplosion(
        RoomGrid roomGrid,
        Vector3Int explosionCenterCell,
        Vector3 explosionCenterWorld,
        Unit candidate,
        int explosionRadiusInCells)
    {
        if (candidate == null)
            return false;

        if (roomGrid != null)
        {
            Vector3Int candidateCell = GridUnitCellUtility.ResolveUnitCell(roomGrid, candidate);
            return GridNavigationUtility.IsWithinCellRange(explosionCenterCell, candidateCell, explosionRadiusInCells);
        }

        float distance = Vector3.Distance(explosionCenterWorld, candidate.Position);
        return distance <= Mathf.Max(0f, (float)explosionRadiusInCells);
    }

    private static SkillImpact CreateExplosiveImpact(RoomGrid roomGrid, Unit candidate, int chainIndex)
    {
        if (candidate == null)
            return null;

        if (roomGrid != null)
        {
            Vector3Int candidateCell = GridUnitCellUtility.ResolveUnitCell(roomGrid, candidate);
            Vector2Int candidateCell2D = new(candidateCell.x, candidateCell.y);
            return SkillImpact.CreateUnit(candidate, candidateCell2D, chainIndex, false);
        }

        return SkillImpact.CreateUnit(candidate, chainIndex, false);
    }

    private static RoomGrid ResolveRoomGridForModifier(SkillContext context, Unit caster)
    {
        if (context != null && context.RoomGrid != null)
            return context.RoomGrid;

        return caster != null && caster.RoomContext != null
            ? caster.RoomContext.RoomGrid
            : null;
    }

    private static bool ContainsVisitedUnit(List<Unit> visitedUnits, Unit candidate)
    {
        if (visitedUnits == null || candidate == null)
            return false;

        for (int i = 0; i < visitedUnits.Count; i++)
        {
            if (ReferenceEquals(visitedUnits[i], candidate))
                return true;
        }

        return false;
    }

    private static bool ContainsUnitImpact(List<SkillImpact> impacts, Unit candidate)
    {
        if (impacts == null || candidate == null)
            return false;

        for (int i = 0; i < impacts.Count; i++)
        {
            SkillImpact impact = impacts[i];
            if (impact == null || !impact.HasTargetUnit)
                continue;

            if (ReferenceEquals(impact.TargetUnit, candidate))
                return true;
        }

        return false;
    }
}
