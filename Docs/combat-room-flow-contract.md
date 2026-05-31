# Combat room flow contract

This is the current contract for the first safe refactor stage. It documents existing authority boundaries without changing deployment flow.

## Authority

- `GameStateManager` is the source of truth for the global game mode: `ExploringDungeon`, `Deployment`, `InCombat`, `CombatResolved`, `GameOver`, and UI-related modes.
- `GameManager` translates room and encounter events into global game state transitions. It does not count units or decide victory/defeat.
- `CombatRoomController` is the source of truth for the local encounter state: `Deployment`, `Combat`, `Resolved`, plus the local `CombatRoomOutcome`.
- `CombatRoomController.CanUnitsAct` is the unit AI action gate. `UnitBrain` observes it and does not decide gameflow.
- `RoomDoor` blocks navigation from unresolved combat rooms by reading the local encounter state.
- `NecromancerInputController` blocks manual movement while the local combat room is unresolved, and separately blocks dialogue via global game state.

## Flow

- `FloorManager` enters a room and asks `RoomContext` to initialize grid, content, components, and units.
- `GameManager` receives `FloorManager.OnRoomEntered`.
- If the room is not a combat room, `GameManager` sets `ExploringDungeon`.
- If the room is a resolved combat room, `GameManager` also sets `ExploringDungeon`.
- If the room is an unresolved combat room, `GameManager` sets `Deployment` and listens to that room's `CombatStarted` event.
- `Necromancer` still starts combat from player input by calling `CombatRoomController.TryStartCombat()`.
- `CombatRoomController` emits `CombatStarted` only if the encounter remains active after the initial outcome check; `GameManager` translates it to `InCombat`.
- `CombatRoomController` decides victory, defeat, or mutual defeat from local unit state only.
- `CombatRoomController` emits `CombatResolved` and `AnyCombatResolved`; `GameManager` translates the current encounter result to global state.

## Death and outcome ordering

- `LifeController.OnUnitDied` is still the early global death signal used by existing observers.
- `UnitDeathHandler.AnyDeathResolved` is emitted after death runtime has been resolved into `Dead`, `Recruitable`, or `Removed`.
- `CombatRoomController` evaluates encounter outcome from `UnitDeathHandler.AnyDeathResolved`, so corpse/recruitable state is settled before victory or defeat is emitted.

## Cleanup policy

- `CombatRoomController` owns current runtime combat cleanup.
- Status effects are cleared on every encounter resolution.
- Summoned combat units are destroyed only on `PlayerVictory`.
- Projectile visuals are destroyed only on `PlayerVictory`.
- The victory-only summon/projectile cleanup is the current behavior. It is documented here, not changed.

## Observers

These systems should observe events and avoid deciding encounter gameflow:

- UI state and combat status UI.
- Selection cleanup.
- Party member cleanup and progression rewards.
- Party defeat return flow.
- Skill charge and kill log consumers of `LifeController.OnUnitDied`.
