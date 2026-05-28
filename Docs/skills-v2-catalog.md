# Skills V2 Catalog

Catalogo activo: `Assets/Core/Data/Scriptable Objects/Combat/Skills/V2/`

## Notas

- `Shield`, `AttackSpeed`, `Evasion` y `GroundCell` siguen pendientes por soporte runtime.
- `Persistent`, `Periodic` y `Accumulative` siguen modelados via `StatusEffectDefinition` cuando corresponde.
- `Expandible` no se implementa en esta etapa.
- Los valores numericos son placeholder de prototipo, no balance final.
- La validacion del catalogo V2 se hace manualmente desde `SkillRuntimeValidationScene`.
- Codex no debe ejecutar Unity, batch validation ni generar reportes automaticos para este catalogo.
- No hay `Logs/skill_v2_validation_report.txt` como salida obligatoria.

| SkillId | Rol | Tipo / ImpactPattern | TargetRequirement | ImpactTargetRequirement | TargetSelectionMode | Effects | Modifiers | Status aplicado | Prefab de test asociado | Estado |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `DPS_DirectDamage` | DPS | `Direct` | `Hostile` | `Hostile` | `RoleBasedOffensive` | `Effect_Damage_Medium` | `-` | `-` | `TestCase_DPS_DirectDamage` | Implementado |
| `DPS_LineDamage` | DPS | `Line` | `Hostile` | `Hostile` | `RoleBasedOffensive` | `Effect_Damage_Medium` | `-` | `-` | `TestCase_DPS_LineDamage` | Implementado |
| `DPS_PiercingLineDamage` | DPS | `Line` | `Hostile` | `Hostile` | `RoleBasedOffensive` | `Effect_Damage_Medium` | `Modifier_Piercing` | `-` | `TestCase_DPS_PiercingLineDamage` | Implementado |
| `DPS_MultiTargetDamage` | DPS | `MultiTarget` | `Hostile` | `Hostile` | `RoleBasedOffensive` | `Effect_Damage_Low` | `-` | `-` | `TestCase_DPS_MultiTargetDamage` | Implementado |
| `DPS_DirectStun` | DPS | `Direct` | `Hostile` | `Hostile` | `RoleBasedOffensive` | `Effect_Damage_Low`, `Effect_ApplyStatus_Stun` | `-` | `Status_Stun` | `TestCase_DPS_DirectStun` | Implementado |
| `Tank_AreaTaunt` | Tank | `Area` | `None` | `Hostile` | `None` | `Effect_ApplyStatus_Taunt` | `-` | `Status_Taunt` | `TestCase_Tank_AreaTaunt` | Implementado |
| `Tank_AreaDamage` | Tank | `Area` | `None` | `Hostile` | `None` | `Effect_Damage_Medium` | `-` | `-` | `TestCase_Tank_AreaDamage` | Implementado |
| `Tank_MultiTargetDamage` | Tank | `MultiTarget` | `Hostile` | `Hostile` | `RoleBasedOffensive` | `Effect_Damage_Low` | `-` | `-` | `TestCase_Tank_MultiTargetDamage` | Implementado |
| `Tank_SpawnMinions` | Tank | `Direct` | `Self` | `Any` | `None` | `Effect_Summon_MinorMinion` | `-` | `-` | `TestCase_Tank_SpawnMinions` | Implementado |
| `Support_SelfHeal` | Support | `Direct` | `Self` | `Self` | `None` | `Effect_Heal_Medium` | `-` | `-` | `TestCase_Support_SelfHeal` | Implementado |
| `Support_AllyHeal` | Support | `Direct` | `Ally` | `Ally` | `AllyLowestHealth` | `Effect_Heal_Medium` | `-` | `-` | `TestCase_Support_AllyHeal` | Implementado |
| `Support_AreaHeal` | Support | `Area` | `None` | `Ally` | `None` | `Effect_Heal_Low` | `-` | `-` | `TestCase_Support_AreaHeal` | Implementado |
| `Support_AllyBuffDamage` | Support | `Direct` | `Ally` | `Ally` | `AllyRolePriority` | `Effect_ApplyStatus_BuffAttackDamage` | `-` | `Status_Buff_AttackDamage` | `TestCase_Support_AllyBuffDamage` | Implementado |
| `Support_EnemyDebuffDefense` | Support | `Direct` | `Hostile` | `Hostile` | `RoleBasedOffensive` | `Effect_ApplyStatus_DebuffDefense` | `-` | `Status_Debuff_Defense` | `TestCase_Support_EnemyDebuffDefense` | Implementado |
| `Support_SpawnMinions` | Support | `Direct` | `Self` | `Any` | `None` | `Effect_Summon_MinorMinion` | `-` | `-` | `TestCase_Support_SpawnMinions` | Implementado |
| `DPS_DirectSplashDamage` | DPS | `Direct` | `Hostile` | `Hostile` | `RoleBasedOffensive` | `Effect_Damage_Medium` | `Modifier_Splash_Small` | `-` | `TestCase_DPS_DirectSplashDamage` | Implementado |
| `DPS_DirectExplosiveDamage` | DPS | `Direct` | `Hostile` | `Hostile` | `RoleBasedOffensive` | `Effect_Damage_Medium` | `Modifier_Explosive_Small` | `-` | `TestCase_DPS_DirectExplosiveDamage` | Implementado |
| `DPS_DirectBounceDamage` | DPS | `Direct` | `Hostile` | `Hostile` | `RoleBasedOffensive` | `Effect_Damage_Low` | `Modifier_Bounce_1` | `-` | `TestCase_DPS_DirectBounceDamage` | Implementado |
| `DPS_LinePiercingExplosiveDamage` | DPS | `Line` | `Hostile` | `Hostile` | `RoleBasedOffensive` | `Effect_Damage_High` | `Modifier_Piercing`, `Modifier_Explosive_Small` | `-` | `TestCase_DPS_LinePiercingExplosiveDamage` | Implementado |

## Status V2 disponibles

- `Status_Stun`
- `Status_Taunt`
- `Status_DamageOverTime_Poison`
- `Status_DamageOverTime_Burn`
- `Status_HealOverTime`
- `Status_Buff_AttackDamage`
- `Status_Buff_MoveSpeed`
- `Status_Debuff_Defense`
- `Status_Debuff_MoveSpeed`
- `Status_Debuff_Accuracy`

## Validacion manual

- Escena: `Assets/Combat/Testing/Skills/Scenes/SkillRuntimeValidationScene.unity`
- Selector manual: `Y`
- Cast manual: `T`
- El estado de validacion se registra manualmente si hace falta.
- La validacion automatizada fue retirada por friccion operativa.

## Casos criticos pendientes antes de abrir GroundCell

- `Tank_AreaDamage`
- `DPS_DirectExplosiveDamage`
- `DPS_DirectBounceDamage`
- `DPS_LinePiercingExplosiveDamage`
- `Tank_SpawnMinions`
- `Support_SpawnMinions`
