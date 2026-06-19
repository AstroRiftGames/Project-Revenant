# Prototype Readiness Report

- Generated At: 2026-06-19 18:36:05 -03:00
- Result: FAIL
- Errors: 2
- Warnings: 8
- Info: 4
- Failed Assets: 2
- Passed Assets: 75

## Build Settings

_No issues._

## Rooms

### Warning
- Severity: Warning
- Category: Room Prefab
- Path: Assets/Dungeon/Prefabs/Rooms/SafeZoneRoom.prefab
- Component: SpawnPoints
- Message: No explicit spawn points were found by name.
- Suggestion: If the room relies on grid-derived spawning only, document that; otherwise add explicit spawn markers.

### Error
- Severity: Error
- Category: Room Prefab
- Path: Assets/Dungeon/Prefabs/Rooms/MiniBossRoomPrefab.prefab
- Component: Doors
- Message: Door config #3 has a missing anchor transform.
- Suggestion: Assign the anchor transform for each procedural door.

### Error
- Severity: Error
- Category: Room Prefab
- Path: Assets/Dungeon/Prefabs/Rooms/SafeZoneRoom.prefab
- Component: RoomPrefabProfile
- Message: RoomPrefabProfile is missing and room type cannot be resolved.
- Suggestion: Add RoomPrefabProfile to the room root or parent.

## Combat Rooms

### Info
- Severity: Info
- Category: Combat Room
- Path: Assets/Dungeon/Prefabs/Rooms/BossRoomPrefab.prefab
- Component: PartySpawn
- Message: No explicit party/player spawn markers were found. Current runtime may rely on RoomPartySpawner grid fallback.
- Suggestion: Add explicit markers only if you want deterministic deployment anchors.

### Info
- Severity: Info
- Category: Combat Room
- Path: Assets/Dungeon/Prefabs/Rooms/CombatRoomPrefab.prefab
- Component: PartySpawn
- Message: No explicit party/player spawn markers were found. Current runtime may rely on RoomPartySpawner grid fallback.
- Suggestion: Add explicit markers only if you want deterministic deployment anchors.

### Info
- Severity: Info
- Category: Combat Room
- Path: Assets/Dungeon/Prefabs/Rooms/MiniBossRoomPrefab.prefab
- Component: PartySpawn
- Message: No explicit party/player spawn markers were found. Current runtime may rely on RoomPartySpawner grid fallback.
- Suggestion: Add explicit markers only if you want deterministic deployment anchors.

### Warning
- Severity: Warning
- Category: Combat Room
- Path: Assets/Dungeon/Prefabs/Rooms/BossRoomPrefab.prefab
- Component: EnemySpawn
- Message: No enemy units or explicit enemy spawn markers were found.
- Suggestion: Add pre-authored enemies, spawn markers, or confirm RoomContentGenerator handles this room.

### Warning
- Severity: Warning
- Category: Combat Room
- Path: Assets/Dungeon/Prefabs/Rooms/BossRoomPrefab.prefab
- Component: RoomContentGenerator
- Message: Combat room has no RoomContentGenerator and no pre-authored enemies.
- Suggestion: Add encounter content generation or author enemy units directly.

### Warning
- Severity: Warning
- Category: Combat Room
- Path: Assets/Dungeon/Prefabs/Rooms/CombatRoomPrefab.prefab
- Component: EnemySpawn
- Message: No enemy units or explicit enemy spawn markers were found.
- Suggestion: Add pre-authored enemies, spawn markers, or confirm RoomContentGenerator handles this room.

### Warning
- Severity: Warning
- Category: Combat Room
- Path: Assets/Dungeon/Prefabs/Rooms/MiniBossRoomPrefab.prefab
- Component: EnemySpawn
- Message: No enemy units or explicit enemy spawn markers were found.
- Suggestion: Add pre-authored enemies, spawn markers, or confirm RoomContentGenerator handles this room.

### Warning
- Severity: Warning
- Category: Combat Room
- Path: Assets/Dungeon/Prefabs/Rooms/MiniBossRoomPrefab.prefab
- Component: RoomContentGenerator
- Message: Combat room has no RoomContentGenerator and no pre-authored enemies.
- Suggestion: Add encounter content generation or author enemy units directly.

## SafeZone / Dungeon

### Warning
- Severity: Warning
- Category: Dungeon
- Path: Assets/Dungeon/Scenes/Dungeon.unity
- Component: GameSceneManager
- Message: No GameSceneManager was found while validating Dungeon scene.
- Suggestion: If the manager is expected to persist from SafeZone, verify scene load order manually.

## Party Manager

### Warning
- Severity: Warning
- Category: Manager Prefab
- Path: Assets/Core/Prefabs/[Manager_Party].prefab
- Component: RoomContext
- Message: NecromancerSpawner fallback RoomContext is not assigned.
- Suggestion: Leave it null only if all supported flows resolve the room procedurally at runtime.

## Creature Prefabs

_No issues._

## UnitData

### Info
- Severity: Info
- Category: UnitData
- Path: Multiple production UnitData assets
- Component: AudioSet
- Message: 54 production UnitData assets are missing AudioSet: Human_DPS_DirectDamage (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/DPS/Human_DPS_DirectDamage.asset); Human_DPS_DirectKnockback (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/DPS/Human_DPS_DirectKnockback.asset); Human_DPS_DirectPoison (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/DPS/Human_DPS_DirectPoison.asset); Human_DPS_DirectSplashDamage (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/DPS/Human_DPS_DirectSplashDamage.asset); Human_DPS_DirectStun (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/DPS/Human_DPS_DirectStun.asset); Human_DPS_LineDamage (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/DPS/Human_DPS_LineDamage.asset); Human_DPS_PiercingLineDamage (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/DPS/Human_DPS_PiercingLineDamage.asset); Human_Support_AllyBuffDamage (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Support/Human_Support_AllyBuffDamage.asset); Human_Support_AllyHeal (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Support/Human_Support_AllyHeal.asset); Human_Support_AreaBuffDamage (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Support/Human_Support_AreaBuffDamage.asset); Human_Support_AreaBuffSpeed (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Support/Human_Support_AreaBuffSpeed.asset); Human_Support_AreaHeal (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Support/Human_Support_AreaHeal.asset); Human_Support_AreaHealOverTime (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Support/Human_Support_AreaHealOverTime.asset); Human_Support_AreaShield (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Support/Human_Support_AreaShield.asset); Human_Support_AreaSlow (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Support/Human_Support_AreaSlow.asset); Human_Support_AreaStun (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Support/Human_Support_AreaStun.asset); Human_Support_DirectBuffSpeed (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Support/Human_Support_DirectBuffSpeed.asset); Human_Support_DirectHealOverTime (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Support/Human_Support_DirectHealOverTime.asset); Human_Support_DirectShield (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Support/Human_Support_DirectShield.asset); Human_Support_DirectSlow (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Support/Human_Support_DirectSlow.asset); Human_Support_DirectStun (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Support/Human_Support_DirectStun.asset); Human_Support_SelfHeal (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Support/Human_Support_SelfHeal.asset); Human_Tank_AreaDamage (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Tank/Human_Tank_AreaDamage.asset); Human_Tank_AreaKnockback (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Tank/Human_Tank_AreaKnockback.asset); Human_Tank_AreaShield (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Tank/Human_Tank_AreaShield.asset); Human_Tank_AreaSlow (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Tank/Human_Tank_AreaSlow.asset); Human_Tank_AreaStun (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Tank/Human_Tank_AreaStun.asset); Orc_DPS_DirectDamage (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/DPS/Orc_DPS_DirectDamage.asset); Orc_DPS_DirectKnockback (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/DPS/Orc_DPS_DirectKnockback.asset); Orc_DPS_DirectPoison (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/DPS/Orc_DPS_DirectPoison.asset); Orc_DPS_DirectSplashDamage (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/DPS/Orc_DPS_DirectSplashDamage.asset); Orc_DPS_DirectStun (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/DPS/Orc_DPS_DirectStun.asset); Orc_DPS_LineDamage (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/DPS/Orc_DPS_LineDamage.asset); Orc_DPS_PiercingLineDamage (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/DPS/Orc_DPS_PiercingLineDamage.asset); Orc_Support_AllyBuffDamage (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Support/Orc_Support_AllyBuffDamage.asset); Orc_Support_AllyHeal (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Support/Orc_Support_AllyHeal.asset); Orc_Support_AreaBuffDamage (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Support/Orc_Support_AreaBuffDamage.asset); Orc_Support_AreaBuffSpeed (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Support/Orc_Support_AreaBuffSpeed.asset); Orc_Support_AreaHeal (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Support/Orc_Support_AreaHeal.asset); Orc_Support_AreaHealOverTime (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Support/Orc_Support_AreaHealOverTime.asset); Orc_Support_AreaShield (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Support/Orc_Support_AreaShield.asset); Orc_Support_AreaSlow (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Support/Orc_Support_AreaSlow.asset); Orc_Support_AreaStun (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Support/Orc_Support_AreaStun.asset); Orc_Support_DirectBuffSpeed (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Support/Orc_Support_DirectBuffSpeed.asset); Orc_Support_DirectHealOverTime (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Support/Orc_Support_DirectHealOverTime.asset); Orc_Support_DirectShield (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Support/Orc_Support_DirectShield.asset); Orc_Support_DirectSlow (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Support/Orc_Support_DirectSlow.asset); Orc_Support_DirectStun (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Support/Orc_Support_DirectStun.asset); Orc_Support_SelfHeal (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Support/Orc_Support_SelfHeal.asset); Orc_Tank_AreaDamage (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Tank/Orc_Tank_AreaDamage.asset); Orc_Tank_AreaKnockback (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Tank/Orc_Tank_AreaKnockback.asset); Orc_Tank_AreaShield (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Tank/Orc_Tank_AreaShield.asset); Orc_Tank_AreaSlow (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Tank/Orc_Tank_AreaSlow.asset); Orc_Tank_AreaStun (Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Tank/Orc_Tank_AreaStun.asset)
- Suggestion: Assign AudioSet only for production units that need combat feedback.

## Skill References

_No issues._

## Failing Assets

- Assets/Dungeon/Prefabs/Rooms/MiniBossRoomPrefab.prefab
- Assets/Dungeon/Prefabs/Rooms/SafeZoneRoom.prefab

## Passing Assets

- Assets/Combat/Prefabs/Creatures/Playable/Human_DPS.prefab
- Assets/Combat/Prefabs/Creatures/Playable/Human_Support.prefab
- Assets/Combat/Prefabs/Creatures/Playable/Human_Tank.prefab
- Assets/Combat/Prefabs/Creatures/Playable/Orc_DPS.prefab
- Assets/Combat/Prefabs/Creatures/Playable/Orc_Support.prefab
- Assets/Combat/Prefabs/Creatures/Playable/Orc_Tank.prefab
- Assets/Core/Data/Scriptable Objects/Allies/HumanBruteData.asset
- Assets/Core/Data/Scriptable Objects/Allies/HumanShamanData.asset
- Assets/Core/Data/Scriptable Objects/Allies/HumanSlayerData.asset
- Assets/Core/Data/Scriptable Objects/Allies/OrcBruteData.asset
- Assets/Core/Data/Scriptable Objects/Allies/OrcShamanData.asset
- Assets/Core/Data/Scriptable Objects/Allies/OrcSlayerData.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/DPS/Human_DPS_DirectDamage.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/DPS/Human_DPS_DirectKnockback.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/DPS/Human_DPS_DirectPoison.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/DPS/Human_DPS_DirectSplashDamage.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/DPS/Human_DPS_DirectStun.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/DPS/Human_DPS_LineDamage.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/DPS/Human_DPS_PiercingLineDamage.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Support/Human_Support_AllyBuffDamage.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Support/Human_Support_AllyHeal.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Support/Human_Support_AreaBuffDamage.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Support/Human_Support_AreaBuffSpeed.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Support/Human_Support_AreaHeal.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Support/Human_Support_AreaHealOverTime.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Support/Human_Support_AreaShield.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Support/Human_Support_AreaSlow.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Support/Human_Support_AreaStun.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Support/Human_Support_DirectBuffSpeed.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Support/Human_Support_DirectHealOverTime.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Support/Human_Support_DirectShield.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Support/Human_Support_DirectSlow.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Support/Human_Support_DirectStun.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Support/Human_Support_SelfHeal.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Tank/Human_Tank_AreaDamage.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Tank/Human_Tank_AreaKnockback.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Tank/Human_Tank_AreaShield.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Tank/Human_Tank_AreaSlow.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Tank/Human_Tank_AreaStun.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/DPS/Orc_DPS_DirectDamage.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/DPS/Orc_DPS_DirectKnockback.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/DPS/Orc_DPS_DirectPoison.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/DPS/Orc_DPS_DirectSplashDamage.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/DPS/Orc_DPS_DirectStun.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/DPS/Orc_DPS_LineDamage.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/DPS/Orc_DPS_PiercingLineDamage.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Support/Orc_Support_AllyBuffDamage.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Support/Orc_Support_AllyHeal.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Support/Orc_Support_AreaBuffDamage.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Support/Orc_Support_AreaBuffSpeed.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Support/Orc_Support_AreaHeal.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Support/Orc_Support_AreaHealOverTime.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Support/Orc_Support_AreaShield.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Support/Orc_Support_AreaSlow.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Support/Orc_Support_AreaStun.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Support/Orc_Support_DirectBuffSpeed.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Support/Orc_Support_DirectHealOverTime.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Support/Orc_Support_DirectShield.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Support/Orc_Support_DirectSlow.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Support/Orc_Support_DirectStun.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Support/Orc_Support_SelfHeal.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Tank/Orc_Tank_AreaDamage.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Tank/Orc_Tank_AreaKnockback.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Tank/Orc_Tank_AreaShield.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Tank/Orc_Tank_AreaSlow.asset
- Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Tank/Orc_Tank_AreaStun.asset
- Assets/Core/Prefabs/[Manager_Party].prefab
- Assets/Core/Scenes/SafeZone.unity
- Assets/Dungeon/Prefabs/Rooms/AltarRoomPrefab.prefab
- Assets/Dungeon/Prefabs/Rooms/BossRoomPrefab.prefab
- Assets/Dungeon/Prefabs/Rooms/CombatRoomPrefab.prefab
- Assets/Dungeon/Prefabs/Rooms/LootRoomPrefab.prefab
- Assets/Dungeon/Prefabs/Rooms/ShopRoomPrefab.prefab
- Assets/Dungeon/Prefabs/Rooms/StartRoomPrefab.prefab
- Assets/Dungeon/Scenes/Dungeon.unity
