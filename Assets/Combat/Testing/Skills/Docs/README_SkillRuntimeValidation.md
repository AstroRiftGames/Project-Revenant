# Skill Runtime Validation Scene

## Ruta

- Escena: `Assets/Combat/Testing/Skills/Scenes/SkillRuntimeValidationScene.unity`
- Casos V2: `Assets/Core/Data/Scriptable Objects/Combat/Skills/V2/TestPrefabs/`

## Uso rapido

1. Abre `SkillRuntimeValidationScene`.
2. Entra en Play Mode.
3. Presiona `Y` para cambiar el caso activo.
4. Presiona `T` para castear la skill activa.

## Que hace la escena

- Usa `RoomGrid` real.
- Usa `RoomContext` real.
- Usa `CombatRoomController` real en `Deployment`.
- Usa unidades reales con `SkillCaster`.
- Registra automaticamente las unidades de escena en el room context.
- Deja al caster y al aliado de heal con vida reducida.

## Casos incluidos

- `TestCase_DPS_DirectDamage`
- `TestCase_DPS_LineDamage`
- `TestCase_DPS_PiercingLineDamage`
- `TestCase_DPS_MultiTargetDamage`
- `TestCase_DPS_DirectStun`
- `TestCase_Tank_AreaTaunt`
- `TestCase_Tank_AreaDamage`
- `TestCase_Tank_MultiTargetDamage`
- `TestCase_Tank_SpawnMinions`
- `TestCase_Support_SelfHeal`
- `TestCase_Support_AllyHeal`
- `TestCase_Support_AreaHeal`
- `TestCase_Support_AllyBuffDamage`
- `TestCase_Support_EnemyDebuffDefense`
- `TestCase_Support_SpawnMinions`
- `TestCase_DPS_DirectSplashDamage`
- `TestCase_DPS_DirectExplosiveDamage`
- `TestCase_DPS_DirectBounceDamage`
- `TestCase_DPS_LinePiercingExplosiveDamage`

## Logs esperados

- El selector informa el caso activo.
- El harness informa si el cast fue aceptado.
- `Log Impacts` enumera los `SkillImpact` resueltos.
