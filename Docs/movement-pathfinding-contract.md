# Movement, Pathfinding, and Occupancy Contract

This document records the current movement/pathfinding/occupancy contract after the movement audit and minimal fixes. It describes current behavior, not a future navigation design.

## Responsibilities By Class

| Class | Responsibility |
|---|---|
| `UnitBrain` | Decides combat intent. Maintains `_basicActionTargetUnit` for basic attacks and skills. Resolves an independent `moveTargetUnit` for pursuit. Keeps the decision order: fear, skill, basic attack, movement, spacing. |
| `UnitMovement` | Executes step-based movement. Interpolates the unit visually. Reserves the next path cell for the active step. Cancels or interrupts movement and releases active reservations. |
| `UnitMovementPlanner` | Calculates `DesiredAttackCell` and `NextStepCell`. Owns path cache and repath timing. It does not execute movement, move transforms, or write occupancy. |
| `GridPathfinder` | Calculates paths through `RoomGrid` topology and traversal costs. It returns path cells; it does not reserve or move units. |
| `GridOccupancyTracker` | Owns real occupancy, step reservations, and persistent blockers. `ReleaseReservation()` is idempotent. |
| `RoomGrid` | Validates bounds, static walkability, avoidance blockers, dynamic occupancy, and step rules. |
| `CombatRoomController` | Cancels active unit movement when combat resolves before cleaning status effects, summons, and projectiles. |

## Targeting Vs Movement

- `_basicActionTargetUnit` is not the movement target.
- Basic attacks and skills use `_basicActionTargetUnit`.
- Movement uses a separate `moveTargetUnit`.
- If a unit cannot attack, it pursues the nearest visible hostile resolved for movement.
- Offensive priorities by role, health, or damage apply to attacking and casting, not to pursuit movement.
- Fear has priority over skill, attack, movement, and spacing. Fear uses its own nearest-threat resolution.

## Reservations

- The system reserves the next path cell, not the final path destination.
- `_reservedNextPathCell` represents the cell reserved for the active visual/logical step.
- A reservation is released when the step completes, movement is canceled or interrupted, the unit dies, the unit is disabled, the grid changes, the unit is attached to a new cell, or combat resolves.
- `GridOccupancyTracker.ReleaseReservation()` is idempotent, so repeated cleanup calls are safe.
- A path can be recalculated without reserving the whole path.

## DesiredAttackCell

- `DesiredAttackCell` is the cell where the unit wants to stand so it can be in basic attack range of its movement target.
- It is not the currently occupied cell.
- It is not necessarily the next path cell.
- It is not a reservation.
- `NextStepCell` is the actual candidate step that `UnitMovement` may reserve and execute.

## Cancellation And Interruption

- `InterruptMovement()` stops active movement.
- On interruption, the unit snaps visually back to `_currentCell` when a grid and logical current cell exist.
- `StopMovementAndReleaseReservation()` centralizes movement cleanup:
  - invalidates planner cache when requested,
  - releases `_reservedNextPathCell`,
  - clears active step cells,
  - resets visual movement state,
  - optionally snaps to the logical current cell.
- `CommitStepOccupancy()` must validate `CanContinueActiveMovement()` before confirming a step.
- If movement can no longer continue, the active step is canceled through the same cleanup path.

## States That Cancel Movement

Movement is canceled or prevented by:

- death,
- removed lifecycle state,
- disabled `UnitMovement`,
- stun or sleep,
- grid loss or grid change,
- combat resolution,
- manual attach to a grid cell.

Slow effects that modify move speed do not cancel movement by themselves. Stun and sleep are movement-restricting effects.

## Corpses And Recruitables

- The current project decision is that recruitable corpses can block cells as persistent blockers.
- Recruitable corpses are not valid combat targets.
- Persistent blockers affect movement/pathfinding, summon placement, and knockback validation because those systems route through grid enterability or step validation.
- Soul absorb/removal clears corpse occupancy before deactivating the object.

## Prototype Simplifications

These simplifications are accepted for the current prototype:

- Reservation is per next step, not for the full path.
- If a target dies during an active step, the step may finish; the next decision frame recalculates.
- Allies are not pushed.
- There is no advanced steering.
- There are no dynamic combat formations.
- There is no full path reservation.

## Known Debt

- `ClearDestination()` is ambiguous because it currently invalidates path cache; it does not cancel an active movement step.
- `RoomGrid.TryFindAttackPositionFromBlockedDesiredCell(...)` keeps older naming. It currently means "find an attack position when the desired attack cell is blocked."
- Movement logs can still be noisy in crowded or blocked scenarios.
- Crowding and deadlock behavior still need a separate audit.
- Dynamic diagonal movement against mobile blockers may need a future review; current diagonal validation primarily checks static corner cutting plus enterability of the destination cell.
- Older documents may still describe the pre-audit behavior where attack target and movement target were coupled.

## Manual Validation Checklist

- Unit dies during movement.
- Unit receives stun or sleep during movement.
- Combat resolves while several units are walking.
- Two units attempt to reserve or occupy the same next step.
- Target dies during pursuit.
- Corpse blocks path.
- Summon attempts to appear on an occupied or reserved cell.
- Knockback resolves against a unit, wall, corpse, and reservation.
- Support/ranged unit maintains preferred distance.
- Crowded room with many units pressing toward the same front.
