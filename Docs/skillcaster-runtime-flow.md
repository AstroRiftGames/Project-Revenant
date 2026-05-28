# SkillCaster Runtime Flow

## Entry point

```
UnitBrain.Update()
  → CanUpdateBrain()              unit alive, movement/action/caster resolved
  → CanActInCurrentEncounter()    room state allows actions
  → CanActFromOperationalState()  not dead/CC/moving/attacking/casting
  → UpdateDecisionState()         TargetingStrategy.SelectBasicActionTarget()
  → ExecuteDecision()
    → ExecuteImmediateIntent()    fear → spacing, consumed = return
    → ExecuteCombatIntent()       
      → TryUseSkillIntent()       SkillCaster.TryUse → consumed? return
      → ExecuteBasicActionIntent() fallback basic action
```

## SkillCaster.TryUse → TryUseInternal

```
TryUseInternal(combatTarget, targetCell, hasTargetCell, skillOverride)

  1. ResolveSkill()                           ← override or UnitData.skill
  2. CanStartCast(skill)                      ← pre-flight checks
  3. TryBuildSkillContext(skill, ...)         ← primary target + context
  4. TryValidateSkillContext(skillContext)    ← target contract + impact center
  5. IsSkillContextInRange(skillContext)      ← range check
  6. BeginCast(skillContext)                  ← set casting state, interrupt movement
  7. ShouldCompleteCastImmediately(skill)     ← Instant or castTime ≤ 0?
     → CompleteCast()                         ← yes: execute now
     → return true                            ← no: wait for UpdateCasting()
```

### Guard breakdown (CanStartCast)

1. `_unit == null` → abort
2. `skill == null` → abort
3. `!skill.TryValidateDeclarativeContract()` → error
4. `IsCasting` → abort (already casting)
5. `!_unit.IsAlive || _unit.LifecycleState != UnitLifecycleState.Alive` → abort
6. `StatusEffects != null && (!CanAct || !CanUseSkills)` → abort
7. `!ResolveSkillReadiness(skill)` → abort (not charged)

### Target selection (ChoosePrimaryTarget)

```
ChoosePrimaryTarget(skill, requestedPrimaryTarget)

  PrimaryTargetRequirement:
    Self           → _unit (if alive)
    None/GroundCell → null
    Hostile/Ally   → 
      requestedPrimaryTarget valid? → use it
      SelectPrimaryTargetByMode()   → auto-select
      AllowsNullPrimaryTarget()?   → return null
      FindFallbackPrimaryTarget()  → last resort
```

`SelectPrimaryTargetByMode` delegates to `TargetingStrategy` static methods:
- `Closest` → `SelectClosestTarget`
- `LowestHealth` → `SelectLowestHealthRatioTarget`
- `HighestBasicDamage` → `SelectHighestBasicDamageTarget`
- `AllyLowestHealth` → `SelectBestHealingAllyTarget`
- `AllyRolePriority` → `SelectBestBuffAllyTarget`
- `RoleBasedOffensive` → `SelectBestOffensiveTarget`

### Context building (BuildSkillContext)

```
SkillContext(
  Caster            = _unit
  Skill             = skill
  PrimaryTarget     = ChoosePrimaryTarget(skill, combatTarget)
  TargetCell        = resolvedTargetCell
  HasTargetCell     = useTargetCell
  ImpactCenterWorld = ResolveImpactCenterWorld(grid, impactCenterUnit, hasTargetCell, resolvedTargetCell)
  ImpactCenterUnit  = ResolveImpactCenterUnit(skill, primaryTarget, useTargetCell)
  RoomContext       = _unit.RoomContext
  RoomGrid          = roomContext.RoomGrid
)
```

`ImpactCenterMode` mapping:
- `Caster` → impact center = caster
- `PrimaryTarget` → impact center = primary target
- `TargetCell` → impact center = cell (GroundCell), no impact center unit

## Cast time / Channel / Instant

`ResolveExecutionDuration(skill)`:
- `Instant` → 0f → completes immediately in TryUseInternal
- `CastTime` → `skill.CastTime` → UpdateCasting counts down each frame
- `Channel` → `skill.CastTime` → UpdateCasting counts down each frame

### UpdateCasting (per frame while casting)

```
UpdateCasting()
  → ShouldInterruptCurrentCast()?  → InterruptCast (unit died / CC'd)
  → _castRemainingTime -= deltaTime
  → _castRemainingTime ≤ 0?        → CompleteCast()
```

### ShouldInterruptCurrentCast (during cast)

1. `_unit == null || !_unit.IsAlive` → interrupt
2. `StatusEffects == null` → no interrupt
3. `!CanAct || !CanUseSkills` → interrupt

### Retarget during cast (TryResolveCastContext)

Called in `CompleteCast()` before impact collection. Rebuilds context if the cached context became stale (e.g., primary target died).

```
TryResolveCastContext(skill, out resolvedContext)

  CanCompleteCastWithContext(_castingContext)?     → use as-is (still valid)
  
  TargetFallbackMode:
    ContinueFromCurrentContext  → use stale context (impacts may be empty → skill fails)
    Cancel                      → return false (skill fails)
    Retarget (default)         → TryBuildSkillContext with previous target
```

Retarget rebuilds the context and re-validates (target valid + in range). If rebuilding fails, the cast is canceled without consuming charge.

## Impact collection (TryCollectImpacts)

```
TryCollectImpacts(skillContext)
  → SkillHitCollector.TryCollectImpacts(context, _impactsHit, logFn)
    → TryCollectBaseImpacts(request, results)
      → ImpactPattern switch:
        Direct       → TryCollectSingleTarget    (primary target only)
        Area         → TryCollectAreaTargets     (radius, all units)
        Line         → TryCollectLineTargets     (projection along line)
        MultiTarget  → TryCollectMultiTarget     (radius, sorted by distance, capped by MaxTargets)
    → ApplyModifiersToImpacts(skillContext, results)
      → SkillModifier.ModifyImpacts(context, skill, impacts)
```

If no impacts collected BUT `CanResolveWithoutImpactTargets` (all effects are SummonUnitSkillEffect):
- Creates a fallback impact (cell or area point) for visual anchoring
- Returns true

If no impacts and skill requires unit targets → returns false → cast fails.

## Effect application (ApplySkillToImpacts)

```
ApplySkillToImpacts(skillContext)
  → for each SkillImpact in _impactsHit
    → ApplySkillToImpact → ApplySkillEffectsToImpact
      → for each SkillEffect in skill.Effects
        → effect.Apply(skillContext, impact)
```

## Charge consumption

```
OnSkillCastSucceeded(skill, primaryTarget)
  → ConsumeChargeOnSuccess(skill)       → ResetAbilityCharge() (full reset)
  → BreakInvisibilityAfterSkillUse()    → remove invis if present
  → NotifySkillUsed(skill, popupAnchor) → SkillUsed + AnySkillUsed events
```

**Charge is consumed ONLY when the skill fully succeeds** (target valid, context built, impacts collected, effects applied). If the cast is interrupted, canceled, or fails at any point, charge is NOT consumed.

## Visual events

```
CompleteCast → NotifySkillImpactsResolvedForVisuals(skill, resolvedContext)
  → instance event: SkillImpactsResolvedForVisuals
  → static event:  AnySkillImpactsResolvedForVisuals
```

Creates an immutable snapshot of impacts (cloned `SkillImpact` objects) so visual listeners can iterate without seeing mid-frame mutations.

### Popup anchor resolution

```
ResolvePopupAnchorUnit(skill, primaryTarget)
  → ResolvePopupAnchorImpact(skill, primaryTarget)
    1. Find PrimaryImpact in _impactsHit → use its TargetUnit
    2. No impacts? Use primaryTarget → SkillImpact.CreateUnit(primaryTarget)
    3. No-impact skill (summon)? Use context target cell / impact center world
    4. Last resort → caster position
  → Return TargetUnit from resolved impact, or _unit as fallback
```

## Cancelation rules

| Scenario | Charge consumed? | Casting state cleared? | Events emitted? |
|---|---|---|---|
| Skill not charged | No | N/A | No |
| Target died during cast | No | Yes (CancelCurrentCast) | No |
| Caster died during cast | No | Yes (InterruptCast) | No |
| Caster CC'd during cast | No | Yes (InterruptCast) | No |
| Context rebuild failed | No | Yes (CancelCurrentCast) | No |
| No impacts collected | No | Yes (CancelCurrentCast) | No |
| No effects applied | No | Yes (CancelCurrentCast) | No |
| Skill succeeds | Yes (full reset) | Yes (ClearCastingState) | SkillUsed + AnySkillUsed + VisualImpacts |

## Charge system

### Sources

| Source | Role gate | Amount |
|---|---|---|
| BasicAttack / BasicHeal | Always | `_chargePerSuccessfulBasicAttack` / `_chargePerSuccessfulBasicHeal` |
| Kill | DPS only | `_chargeFromKill` |
| DamageTaken | Tank only | `_chargeFromDamageTaken` |
| NearbyAllySkillUsed | Support only | `_chargeFromNearbyAllySkillUsed` (radius: `_nearbyAllySkillChargeRadius`) |

### Basic action → charge contract

La carga por basic action se otorga exclusivamente a través de `GrantChargeFromBasicAction(TargetRelation)`.

**Caller único:** `UnitCombat.NotifySuccessfulBasicAction()` — llamado después de aplicar el efecto y verificar que efectivamente cambió el health del target.

```
UnitCombat.TryUseBasicActionOn():
  1. CanUseBasicActionOn(...)         ← validaciones (rango, cooldown, status)
  2. ApplyBasicActionToTarget(...)    ← damage/heal hitscan inmediato
  3. DidBasicActionApplyEffect(...)   ← target.CurrentHealth changed?
  4. NotifySuccessfulBasicAction()    ← solo si effect applied:
       → _skillCaster.GrantChargeFromBasicAction(targetRelation)
  5. ConsumeBasicActionCooldown()
  6. ShowBasicActionPresentation()    ← projectile VISUAL (puramente estético)
```

Reglas:
- `TargetRelation.Hostile` → `AbilityChargeSource.BasicAttack` (ataque)
- `TargetRelation.Ally` → `AbilityChargeSource.BasicHeal` (curación)
- Si `targetRelation == Ally`, valida además que la acción del unit tenga `RequiresInjuredTarget == true`
- **Proyectiles:** son puramente visuales (`ShowBasicActionPresentation`). El daño/curación siempre se aplica hitscan. No hay doble charge ni charge diferida.
- **Melee:** no muestra projectile. La carga se otorga igual, hitscan inmediato.
- **Sin efecto:** si `target.CurrentHealth == targetHealthBefore` (target inmune, full HP, etc.), no se otorga carga.

**Fuera del contrato (eliminado):** `NotifyBasicActionHit()` no tenía callers. Era dead code que habría duplicado carga si alguien lo invocaba junto con `GrantChargeFromBasicAction`. Se eliminó.

### Gating

1. `CanReceiveChargeFromSource`: role gate + `CanApplyAbilityCharge`
2. `CanApplyAbilityCharge`: lifecycle must be `Alive`, unit must be `IsAlive` (except DamageTaken bypass)
3. `CanGainAbilityCharge`: status effects don't prevent charge (no `PreventsSkillCooldownCharge`)

### Consumption

`ConsumeChargeOnSuccess` → `ResetAbilityCharge()` → sets charge to 0 (full reset, not decrement-by-cost).

## Relationship with UnitTargetValidator

- `IsSkillTargetSelectable`: entry point for skill primary target validation
- Creates `TargetingPolicy` from `SkillData` (relation, self/allowed, etc.)
- `IsValidTargetLifecycle`: excludes `Removed` and `Recruitable` targets
- `CanUseUnitAsPrimaryTarget` also checks `skill.Requirements.AreSkillSpecificRequirementsMet`

## Relationship with SkillHitCollector

- `CanSkillHitUnit`: validates impact targets (separate contract from primary target)
- `ImpactTargetRequirement` vs `PrimaryTargetRequirement`: independent enums
- `CanResolveWithoutImpactTargets`: true only if ALL effects are `SummonUnitSkillEffect` — allows summon skills to succeed without hitting any unit

## Legado (non-functional, kept for migration)

- `_legacyShape` field in `SkillData`: `Splash`, `PiercingLine`, `SpawnMinions` → migration warnings emitted in `OnValidate()`
- `SkillShape.Splash` → should become `ImpactPattern.Direct` + `SplashSkillModifier`
- `SkillShape.PiercingLine` → should become `ImpactPattern.Line` + `PiercingSkillModifier`
- `SkillShape.SpawnMinions` → should become `SummonUnitSkillEffect` + `SummonAnchorMode`

## Cambios realizados en esta auditoría

| Cambio | Archivo | Razón |
|---|---|---|
| Add lifecycle guard `LifecycleState != Alive` in `CanStartCast` | SkillCaster.cs:353 | Avoid recruitable/removed units casting |
| Move `ClearCastingState()` after `OnSkillCastSucceeded()` | SkillCaster.cs:428 | Fix popup anchor using stale `_castingContext` |
| Remove dead `SkillContextsMatch()` | SkillCaster.cs | Private, never called |
| Remove dead `TryBuildSkillContext(3-param)` overload | SkillCaster.cs | Private, never called |
| Remove dead `BuildSkillContext(2-param)` overload | SkillCaster.cs | Private, never called |
| Remove `_unitsHit` field + population + replace with `_impactsHit` | SkillCaster.cs | Redundant data source, same information available in `_impactsHit` |
| Remove dead `NotifyBasicActionHit()` | SkillCaster.cs | Zero callers, would duplicate charge |
| Rename `NotifyBasicActionSucceeded` → `GrantChargeFromBasicAction` | SkillCaster.cs, UnitCombat.cs | Clarifies actual purpose (grants charge, not "notification") |

## Deuda pendiente

- `_castingTarget` field duplicates `_castingContext.PrimaryTarget`. After retarget in `TryResolveCastContext`, both are kept in sync. Could be removed if `CastTarget` property uses `_castingContext` directly.
- Debug formatting methods (`FormatOwnerIdentity`, `FormatUnitName`, `FormatImpacts`, `FormatSkillContext`) — ~100 lines at end of file. Duplicated in `SkillHitCollector` (~75 lines). Extraction to a shared `SkillDebugFormatter` static class would reduce duplication but is cosmetic.
- `GroundCell` targeting (`TryUseGroundCell`) is signed off as incomplete — only `TryUseInternal` path exists, no UI-driven ground target selection flow is wired.
- `SkillTrajectory.Projectile` has no runtime implementation. All skills use `Hitscan`.
- Several `static` debugging `Format*` methods in `SkillHitCollector` duplicate the same pattern in `SkillCaster`.
