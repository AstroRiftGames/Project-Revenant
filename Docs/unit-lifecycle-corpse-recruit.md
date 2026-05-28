# Unit Lifecycle: Corpse, Recruit, and Soul Absorb

## State Diagram

```
  Alive ──(damage lethal)──► ResolveDeath()
                              │
                   ┌──────────┴──────────┐
                   │                     │
            IsEnemy=true           IsEnemy=false
                   │                     │
          LeaveRecruitableCorpse()  ResolveDefaultDeath()
                   │                     │
              Recruitable              Dead
              (active object)     (SetActive false)
                   │
          ┌───────┴────────┐
          │                │
    TryRecruit()    TryAbsorbSoul()
          │                │
      Alive           Removed
    (ally team)    (SetActive false)
```

## State Contract

| State | Can Act | Can Move | Can Be Targeted (Hostile) | Counts for Combat Outcome | Occupies Cell (regular) | Corpse Blocker Active | Interactable | in RoomContext.Units |
|---|---|---|---|---|---|---|---|---|
| Alive | Yes | Yes | Yes | Yes | Yes (Movement) | No | No | Yes |
| Dead | No | No | No | No | No (inactive) | No | No | No (unregistered) |
| Recruitable | No | No | No | No | No (released) | Yes | Yes | Yes |
| Removed | No | No | No | No | No (inactive) | No | No | No (unregistered) |

> **Regla explicita:** Recruitable **no** es una unidad viva. Es un corpse interactuable que bloquea la grilla solo mediante `PersistentGridOccupant` (persistent blocker). No ocupa celdas como `IGridOccupant` regular. No puede actuar, moverse, ni ser targeteado. No cuenta para el outcome del combate.

## Responsibilities by Class

### LifeController
- Manages health (CurrentHealth, MaxHealth)
- Guards death: `TakeDamage()` checks `IsAlive` and `HasInvincibility`
- On lethal damage → `ResolveDeath()` → interrupts movement, calls `HandleOwnerDeath()` on status effects, fires `OnUnitDied` static event
- `Revive()` restores living runtime state and sets health > 0
- Tracks aggressors and LastAttacker for TargetingStrategy Dynamic mode

### UnitDeathHandler
- Orchestrates death outcome: recruitable corpse vs default death vs soul absorb
- `ResolveDeath()`: entry point called by LifeController after lethal damage
- `LeaveRecruitableCorpse()`: disables combat behaviours, captures corpse occupancy as persistent blocker, sets state Recruitable
- `ResolveDefaultDeath()`: interrupts movement, clears corpse, sets Dead, deactivates object
- `FinishSoulAbsorb()`: clears corpse occupancy, sets Removed, deactivates object
- `ReviveUnit()`: delegates to LifeController
- `ResetDeathState()`: resets all death flags, releases corpse occupancy, restores behaviours, sets target lifecycle state

### RecruitableUnitState
- Single source of truth for lifecycle state
- Fires `OnStateChanged` and `OnRecruitableStateChanged` events
- Properties: `IsAlive`, `IsRecruitable`, `IsDead`, `IsRemoved`, `CanInteractWithCorpse`

### RecruitableCorpseHandler
- `TryRecruit()`: validates corpse is recruitable, spends mana, recruits into party, revives unit, tracks deployment
- `TryAbsorbSoul()`: validates corpse, awards souls, calls `FinishSoulAbsorb()`
- `ReviveRecruitedUnit()`: changes team/faction via SetAffiliation, sets party link, restores health

### RecruitableUnitInteraction
- Implements `IInteractable` for player interaction
- Shift+click → soul absorb; normal click → recruit
- Tracks interaction availability based on RecruitableUnitState

### Unit / Creature
- `IsAlive` → delegates to LifeController (health > 0)
- `LifecycleState` → delegates to RecruitableUnitState
- `IsHostileTo` → checks both units are Alive lifecycle AND alive health
- `CanDetect` → checks target is Alive lifecycle AND active in hierarchy
- OnDisable → unregisters from RoomContext
- `OccupiesCell` → only true when `LifecycleState == Alive` AND `activeInHierarchy` (defense-in-depth: Recruitable units never register as regular occupants, they use persistent blocker)

### CombatRoomController
- `IsValidCombatant()` → checks `activeInHierarchy && IsAlive && LifecycleState == Alive`
- `EvaluateEncounterOutcome()` → iterates `RoomContext.Units`, only counts combatants
- `HandleUnitDied()` → subscribed to `LifeController.OnUnitDied`
- `GetActiveEnemies()` → filters by `activeInHierarchy && IsAlive && Team == Enemy`

### UnitTargetValidator
- `IsValidSelectingUnit()` → blocks targeting from Dead/Recruitable/Removed units
- `IsValidTargetLifecycle()` → blocks targeting Recruitable and Removed units
- Dead units are blocked by `!policy.AllowDead && !target.IsAlive` (health check)

### GridOccupancyTracker
- `RegisterOccupant()` → checks `OccupiesCell` on the occupant; if false, releases instead of registering
- `IsOccupied()` → checks `OccupiesCell` per occupant; if false, skips that occupant
- `RegisterPersistentBlocker()` → creates a `PersistentGridOccupant` (always `OccupiesCell = true`) separate from the Unit

### UnitMovement
- `TryRegisterCurrentOccupancy()` → only registers if `LifecycleState == Alive`
- `CaptureCorpseOccupancy()` → releases regular occupant, registers persistent blocker
- `ClearCorpseOccupancy()` → releases persistent blocker
- `OnDisable()` → releases reservation AND current occupancy

## LeaveRecruitableCorpse() - Flow Verification

```
Step 1: PrepareMovementForDeath()
        → InterruptCast() on SkillCaster
        → InterruptMovement() on UnitMovement
          → StopMovementAndReleaseReservation()
          → reservation released, step data cleared

Step 2: CaptureCorpseOccupancy()
        → ReleaseCurrentOccupancy()
          → Unit removed from _occupantsByCell
          → _registeredGrid = null
        → RegisterPersistentBlocker(_unit, cell)
          → PersistentGridOccupant added to _occupantsByCell
          → _persistentOccupantsByOwner[_unit] = blocker

Step 3: DisableBehavioursForRecruitableDeath()
        → UnitMovement.OnDisable() fires
          → StopMovementAndReleaseReservation()  // no-op (already interrupted)
          → ReleaseCurrentOccupancy()            // no-op (_registeredGrid is null)

Step 4: SetLifeState(Recruitable)
        → _recruitableState.CurrentState = Recruitable
```

**No double registration:** regular occupant was released in step 2 before persistent blocker was registered. ✓
**No stray reservation:** cleared in step 1. ✓
**Recruitable unit is NOT in _occupantsByCell as IGridOccupant.** Only PersistentGridOccupant is there. ✓

## FinishSoulAbsorb() - Flow Verification

```
Step 1: PrepareMovementForDeath()
        → interrupt cast, interrupt movement, release reservation

Step 2: ClearCorpseOccupancy()
        → ReleasePersistentBlocker(_unit)
          → PersistentGridOccupant removed from _occupantsByCell
          → _persistentOccupantsByOwner cleared

Step 3: SetLifeState(Removed)
        → _recruitableState.CurrentState = Removed

Step 4: gameObject.SetActive(false)
        → Unit.OnDisable() → UnregisterUnit() from RoomContext
        → UnitMovement.OnDisable() → no-op (_registeredGrid is null)
```

**No leftover occupancy:** persistent blocker cleared in step 2. ✓
**No leftover reservation:** cleared in step 1. ✓
**No targeteable:** IsValidTargetLifecycle blocks Removed. ✓
**Does not count for outcome:** inactive + not alive. ✓

## ReviveUnit() - Flow Verification

```
Step 1: SetAffiliation(Ally team, Ally faction)
        → team changes to Ally BEFORE becoming Alive

Step 2: LifeController.Revive()
        → RestoreLivingRuntimeState()
          → DeathHandler.ResetDeathState(Alive)
            → ClearCorpseOccupancy()
              → PersistentGridOccupant removed from occupancy ✓
            → RestoreBehaviours()
              → UnitMovement.OnEnable()
                → TryRegisterCurrentOccupancy()
                  → LifecycleState is already Alive ✓
                  → Registers as regular occupant ✓
            → SetLifeState(Alive)
          → StatusEffectController.RestoreLivingRuntimeState()
        → SetCurrentHealth(>0)
```

**Corpse blocker cleared before regular occupant registered:** ResetDeathState calls ClearCorpseOccupancy, THEN RestoreBehaviours. ✓
**Team changes before becoming targetable:** SetAffiliation runs before Revive, so by the time the unit is Alive, it's already Ally. ✓
**Single occupancy registration:** TryRegisterCurrentOccupancy runs once via OnEnable. ✓
**No double registration:** ClearCorpseOccupancy removed the persistent blocker before any RegisterOccupant call. ✓

## Targeting Rules

- Units in Recruitable state: cannot be targeted (blocked by IsValidTargetLifecycle)
- Units in Removed state: cannot be targeted (blocked by IsValidTargetLifecycle)
- Units in Dead state: cannot be targeted (blocked by `!policy.AllowDead && !target.IsAlive`)
- Units in Alive state: targetable according to relationship policy
- Selecting unit must be Alive state (blocked by IsValidSelectingUnit)
- Recruited allies: team changes to Ally before becoming Alive, so never hostile after revive
- Summoned units: marked with CombatSummonedUnitRuntimeMarker, cleaned up on room resolve

## Occupancy Rules

- Alive units: registered as regular occupants via UnitMovement
- Recruitable corpses: regular occupant released, persistent blocker registered (blocks the cell)
- Dead units (SetActive false): occupant unregistered via OnDisable, no persistent blocker
- Removed units (SetActive false): occupant unregistered via OnDisable, persistent blocker cleared
- Soul absorbed: corpse occupancy cleared, unit deactivated
- Revived units: persistent blocker cleared, regular occupant re-registered

## CombatRoom Outcome Evaluation

Triggered by `LifeController.OnUnitDied` event.

Evaluation loops `RoomContext.Units`:
- Counts only units where `IsValidCombatant()` is true
- A recruited unit (Alive, Ally team) is counted as an ally
- A soul-absorbed corpse is Removed + inactive, so not counted
- A corpse in Recruitable state has health = 0 → IsAlive = false → not counted
- `activeInHierarchy` alone is never the criterion — always paired with `IsAlive` and `LifecycleState`

## Validation Cases (Playtest)

1. **Enemy takes lethal damage**
   - Health = 0, state = Recruitable
   - Unit movement disabled, behaviours disabled
   - Cell blocked by persistent blocker (OccupiesCell = true on PersistentGridOccupant)
   - Cannot be targeted
   - Interaction prompt shows (recruit or soul absorb)

2. **Ally takes lethal damage**
   - Health = 0, state = Dead
   - GameObject deactivated
   - Removed from RoomContext
   - Does not count for outcome

3. **Recruit a corpse (normal click)**
   - State changes: Recruitable → Alive
   - Team changes: Enemy → Ally
   - Persistent blocker cleared, regular occupant registered
   - Behaviours re-enabled
   - Can act on next brain tick

4. **Soul absorb a corpse (shift+click)**
   - State changes: Recruitable → Removed
   - Persistent blocker cleared
   - GameObject deactivated
   - Unit removed from RoomContext
   - Souls awarded, mana spent

5. **Recruited unit dies**
   - Follows ally death path (ResolveDefaultDeath)
   - State = Dead, GameObject deactivated
   - No recruitable corpse (only enemies leave corpses)

6. **Multiple corpses in same room**
   - Each corpse blocks its own cell via persistent blocker
   - Each can be interacted with independently
   - Absorbing one does not affect others

7. **Combat outcome with corpses remaining**
   - PlayerVictory if all enemies are dead/recruitable
   - Corpses do NOT count as alive enemies
   - Corpses remain in RoomContext.Units but filtered by IsValidCombatant

8. **Recruit with insufficient mana**
   - TryRecruit returns false
   - No state change, no party change
   - Corpse remains interactable

9. **Summoned unit dies**
   - Has CombatSummonedUnitRuntimeMarker
   - Death flow same as ally (ResolveDefaultDeath)
   - On PlayerVictory: all summoned units destroyed

## Deuda técnica conocida

- `Creature.GetVisibleUnits()` y `GetVisibleHostileUnits()` estan vacios — no hay sistema de vision implementado, todo targeting depende de `RoomContext.Units`
- `Accuracy` y `Evasion` existen como stats pero nunca se usan en el pipeline de dano
- `LifeController` tiene dos banderas `_hasResolvedDeath` (una propia y otra en `UnitDeathHandler`) — se mantienen en sync por el flujo pero no hay validacion de consistencia entre ellas. `FinishSoulAbsorb()` deja `_hasResolvedDeath = true` en ambos, pero la unidad queda Removed e inactiva, por lo que es irrelevante
- `ReviveUnit()` no valida que la celda esté libre antes de registrar occupant — en la practica es seguro porque el persistent blocker la mantuvo ocupada hasta el revive, pero es una invariante debil
