# Grid Occupancy & Reservations

## System overview

Three systems interact to determine which cells are blocked:

1. **Static tilemap blocking** (`RoomGridTopology`): walkable/blocked tilemaps + optional physics overlap check
2. **Occupancy** (`GridOccupancyTracker`): live units and persistent blockers registered per cell
3. **Reservations** (`GridOccupancyTracker`): cells reserved by units in the process of moving into them

`IsCellEnterable` = not hard-blocked AND not movement-blocked.
`DoesCellBlockMovement` = any occupant at cell with `BlocksMovement == true`, OR cell is reserved.

## Core invariants

### IGridOccupant contract

| Property | Unit (alive) | Unit (dead/recruitable/removed) | PersistentGridOccupant |
|---|---|---|---|
| `OccupiesCell` | true | false | true |
| `BlocksMovement` | true | N/A (OccupiesCell=false) | true |

- `OccupiesCell` requires `LifecycleState == UnitLifecycleState.Alive && gameObject.activeInHierarchy`
- `PersistentGridOccupant` always occupies and always blocks (used for corpses, obstacles)

### Registration invariants

```
Unit has UnitMovement    → registered in GridOccupancyTracker when alive+active
Unit no UnitMovement     → NEVER registered in GridOccupancyTracker
PersistentGridOccupant   → registered via RegisterPersistentBlocker (corpses)
```

- Registration happens at: `OnEnable`, `SnapToCurrentCell`, `RelocateToCellInternal`, `AttachToGridAtCell`, `CommitStepOccupancy`
- De-registration happens at: `OnDisable`, `ReleaseOccupancyFrom`, `ReleaseCurrentOccupancy`

## Movement step lifecycle

```
SetTarget/SetDestinationCell
  → Planner.PlanTowards → TryCommitMovementStep
    → IsCellBlockedFor? (occupied or reserved → retry/deadlock)
    → SetDestinationCell → TryReserveCell → BeginVisualStep
      → UpdateStepPresentation (lerp over time)
        → CommitStepOccupancy
          → MoveOccupant (updates cell, releases reservation)
```

### Reservation protocol

- Reservation is an exclusive intent-to-occupy, preventing other units from targeting the same cell
- `TryReserveCell` fails if cell is occupied or reserved by another unit
- Reservations are released on: step commit, interruption, death, grid change, `ReleaseOccupancyFrom`
- `IsCellBlockedFor` considers both occupancy and reservations as blockers

## Flow analysis

### Death flow (recruitable enemy)

```
LifeController.ResolveDeath()
→ UnitDeathHandler.HandleOwnerDeath()
  → InterruptMovement()              → StopMovementAndReleaseReservation (releases reservation, snaps back)
  → LeaveRecruitableCorpse()
    → CaptureCorpseOccupancy()       → ReleaseCurrentOccupancy + RegisterPersistentBlocker
    → DisableBehavioursForRecruitable → UnitMovement.OnDisable → StopMovementAndReleaseReservation (no-op, already stopped)
    → state = Recruitable
```

Invariant: regular occupancy released BEFORE persistent blocker registered → no double-block.

### Death flow (ally)

```
LifeController.ResolveDeath()
→ UnitDeathHandler.HandleOwnerDeath()
  → InterruptMovement()              → StopMovementAndReleaseReservation
  → ResolveDefaultDeath()
    → ClearCorpseOccupancy()         → no-op (recruitable was never set)
    → state = Dead
    → SetActive(false)
      → OnDisable → ReleaseCurrentOccupancy()
```

Invariant: occupancy released via `OnDisable` → `ReleaseCurrentOccupancy`.

### Revive flow

```
RecruitableCorpseHandler.ReviveRecruitedUnit()
→ SetAffiliation(Ally)
→ DeathHandler.ReviveUnit()
  → LifeController.Revive()
    → RestoreLivingRuntimeState()
    → DeathHandler.ResetDeathState(Alive)
      → ClearCorpseOccupancy()       → ReleasePersistentBlocker
      → RestoreBehaviours()          → OnEnable → TryRegisterCurrentOccupancy
```

Invariant: persistent blocker released BEFORE re-registering as alive occupant.

### Summon flow

```
SummonUnitSkillEffect.Apply()
→ TryFindWalkableCellInRange(desiredCell, casterCell, range, movingUnit: null)
  → null movingUnit = exclude no one → 100% empty cell required
→ Instantiate prefab
→ movement.AttachToGridAtCell(grid, spawnCell)
  → IsCellEnterable(check, _unit)     → excludes summoned self (not yet registered, no-op)
  → ReleaseOccupancyFrom(oldGrid)     → clean slate
  → RegisterOccupant(_unit, cell)     → fresh registration
```

Invariant: spawn cell verified empty (null movingUnit), no double-occupancy risk.

### Knockback flow

```
KnockbackSkillEffect.ApplyKnockback()
→ Resolve cell iterating: IsCellEnterable(intermediateCell, target)
  → excludes target from blocking check
→ If has UnitMovement → ForceRelocateToCell(resolvedCell)
  → StopMovementAndReleaseReservation
  → RegisterOccupant(new cell)
→ If no UnitMovement → transform.position = CellToWorld(resolvedCell)
  → No occupancy update needed (unit was never registered)
```

Invariant: knockback on non-UnitMovement entities is correct (they're not tracked in occupancy).

### Room change flow

```
RoomContext.EnterRoom()
→ CacheUnits()                        → _units.Clear() + GetComponentsInChildren
→ InjectContextIntoUnits()
  → Unit.IntegrateIntoRoom(context)
    → UnitMovement.IntegrateWithRoom(context)
      → SetGrid(roomContext.RoomGrid)
        → ReleaseOccupancyFrom(oldGrid) → ReleaseOccupant + ReleaseReservation + ReleasePersistentBlocker
        → SnapToCurrentCell()           → TryRegisterCurrentOccupancy in new grid
```

### Combat room enemy arrangement

```
CombatRoomController.ArrangeEnemiesForRoomEntry(enteredDoor)
→ ReleaseEnemyOccupancy(enemies)      → SetGrid(null) on each → ReleaseOccupancyFrom + clear
→ For each enemy: AttachToGridAtCell(grid, candidateCell)
  → StopMovementAndReleaseReservation + ReleaseOccupancyFrom (no-op, already null)
  → RegisterOccupant(new cell)
```

Invariant: old occupancy fully released before new registration. No double-occupancy risk.

## GridCellMovementValidator

```csharp
CanEnter(grid, query):
  if grid.IsCellHardBlocked(query.Cell) → false
  return !grid.DoesCellBlockMovement(query.Cell, query.MovingOccupant)
```

13 lines, single responsibility. Delegates hard blocking to `RoomGrid.IsCellHardBlocked` (static tilemap + avoidance obstacles) and movement blocking to `GridOccupancyTracker.DoesCellBlockMovement` (occupants + reservations).

## GridPathfinder

Standard A* using:
- `grid.IsCellEnterable(goal, movingUnit)` for goal validation
- `grid.GetNeighbors(current, movingUnit)` for neighbor iteration
- `grid.TryGetTraversalCost(neighbor)` for cost lookups (rejects hard-blocked cells)

No explicit occupancy check in pathfinding itself — delegates entirely to `RoomGrid`.

## Edge cases considered and verified

| Case | Status |
|---|---|
| Knockback on unit without UnitMovement | No fix needed — unit was never in occupancy tracker |
| Summon on occupied cell | Not possible — `TryFindWalkableCellInRange` with `null` requires completely empty cell |
| Double-revive guard | Added in round 1 (`LifeController.Revive` early-return) |
| Death during movement with reservation | Reservation released via `InterruptMovement` → `StopMovementAndReleaseReservation` |
| Reservation not released on death | Released in `InterruptMovement` (called before death resolution) |
| Occupancy after `SetActive(false)` | Released via `OnDisable` → `ReleaseCurrentOccupancy` |
| Unit disabled while alive (not death) | Same path — `OnDisable` releases occupancy |
| Inactive unit in `InjectContextIntoUnits` | `TryRegisterCurrentOccupancy` guarded by `isActiveAndEnabled` |
| Room re-entry | `SetGrid` with same grid is no-op (path cache invalidated only) |
| `PersistentGridOccupant.WorldPosition` for parameterless `RegisterOccupant` | Not used — `RegisterPersistentBlocker` calls the cell-parameter overload directly |
