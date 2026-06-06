# Skills V2 Overview

Este documento es la fuente de verdad principal para el estado actual del sistema Skills V2. Describe lo que existe hoy en runtime, assets, VFX placeholder y herramientas de prueba. No describe el diseno ideal ni reemplaza la matriz externa de compatibilidad.

## 1. Estado Actual

Skills V2 soporta como base habilidades `Hitscan` con ejecucion `Instant`, `CastTime` y `Channel` a nivel de datos/runtime. La cobertura validada esta centrada en `Hitscan` + `Instant`.

Implementado:
- Resolucion de impactos por `Direct`, `Area`, `Line` y `MultiTarget`.
- Effects runtime: Damage, Heal, ApplyStatus, Knockback, Shield y Summon.
- DoT/HoT mediante status con ticks periodicos.
- Modifiers runtime: Splash, Piercing, Bounce y Explosive.
- Placeholder VFX/debug para impacts, status, shield, knockback, DoT/HoT y projectile visual-only.
- Prefabs reales activos de criaturas: `Human_DPS`, `Human_Tank`, `Human_Support`, `Orc_DPS`, `Orc_Tank` y `Orc_Support`.
- Catalogo SkillData V2 normalizado en carpetas `Role_DPS`, `Role_Tank` y `Role_Support`.

Provisional:
- `Tank_AreaTaunt` esta activo, pero Taunt debe formalizarse en la matriz.
- `Projectile` existe como dato, pero no como gameplay real.

Debug/deprecated:
- MultiTarget y multi-modifier existen, pero son advanced/debug hasta definir contrato.
- Summon existe en runtime, pero no queda ningun `SkillData` V2 activo para Summon hasta redisenarlo como Area/Zone.
- Assets legacy `SE_*` fueron eliminados y quedan fuera de Skills V2.
- Prefabs genericos/legacy `HumanUnit`, `OrcUnit`, `Enemy*` y `Ally*` no forman parte del flujo real.

Falta:
- Projectile gameplay real.
- UI final de shield.
- VFX de CastTime/Channel.
- UI real para `GroundCell`.
- Runtime para Persistent, Periodic, Expandable y Accumulative como modifiers avanzados.

## 2. Catalogo Activo

### DPS

| Skill | Pattern | Target | Effect | Modifier | Estado |
| --- | --- | --- | --- | --- | --- |
| DPS_DirectDamage | Direct | Hostile | Damage | - | Activa |
| DPS_DirectSplashDamage | Direct | Hostile | Damage | Splash | Activa |
| DPS_DirectExplosiveDamage | Direct | Hostile | Damage | Explosive | Activa |
| DPS_DirectBounceDamage | Direct | Hostile | Damage | Bounce | Activa |
| DPS_DirectStun | Direct | Hostile | Damage + Stun | - | Activa |
| DPS_LineDamage | Line | Hostile | Damage | - | Activa |
| DPS_PiercingLineDamage | Line | Hostile | Damage | Piercing | Activa |
| DPS_AreaDamage | Area | Hostile | Damage | - | Activa |
| DPS_AreaStun | Area | Hostile | Damage + Stun | - | Activa |
| DPS_AreaSlow | Area | Hostile | Slow | - | Activa |
| DPS_DirectKnockback | Direct | Hostile | Knockback | - | Activa |
| DPS_DirectPoison | Direct | Hostile | Poison DoT | - | Activa |
| DPS_AreaBurn | Area | Hostile | Burn DoT | - | Activa |
| DPS_LinePiercingExplosiveDamage | Line | Hostile | Damage | Piercing + Explosive | Debug/advanced |
| DPS_MultiTargetDamage | MultiTarget | Hostile | Damage | - | Debug/advanced |

### Tank

| Skill | Pattern | Target | Effect | Modifier | Estado |
| --- | --- | --- | --- | --- | --- |
| Tank_DirectDamage | Direct | Hostile | Damage | - | Activa |
| Tank_AreaDamage | Area | Hostile | Damage | - | Activa |
| Tank_DirectStun | Direct | Hostile | Stun | - | Activa |
| Tank_AreaStun | Area | Hostile | Stun | - | Activa |
| Tank_DirectSlow | Direct | Hostile | Slow | - | Activa |
| Tank_AreaSlow | Area | Hostile | Slow | - | Activa |
| Tank_DirectKnockback | Direct | Hostile | Knockback | - | Activa |
| Tank_AreaKnockback | Area | Hostile | Knockback | - | Activa |
| Tank_DirectShield | Direct | Ally | Shield | - | Activa |
| Tank_AreaShield | Area | Ally | Shield | - | Activa |
| Tank_AreaTaunt | Area | Hostile | Taunt | - | Provisional activa |
| Tank_MultiTargetDamage | MultiTarget | Hostile | Damage | - | Debug/advanced |

### Support

| Skill | Pattern | Target | Effect | Modifier | Estado |
| --- | --- | --- | --- | --- | --- |
| Support_AllyHeal | Direct | Ally | Heal | - | Activa |
| Support_AreaHeal | Area | Ally | Heal | - | Activa |
| Support_SelfHeal | Direct | Self | Heal | - | Activa |
| Support_AllyBuffDamage | Direct | Ally | BuffDamage | - | Activa |
| Support_AreaBuffDamage | Area | Ally | BuffDamage | - | Activa |
| Support_DirectBuffSpeed | Direct | Ally | BuffMoveSpeed | - | Activa |
| Support_AreaBuffSpeed | Area | Ally | BuffMoveSpeed | - | Activa |
| Support_DirectSlow | Direct | Hostile | Slow | - | Activa |
| Support_AreaSlow | Area | Hostile | Slow | - | Activa |
| Support_DirectStun | Direct | Hostile | Stun | - | Activa |
| Support_AreaStun | Area | Hostile | Stun | - | Activa |
| Support_DirectShield | Direct | Ally | Shield | - | Activa |
| Support_AreaShield | Area | Ally | Shield | - | Activa |
| Support_DirectHealOverTime | Direct | Ally | HealOverTime | - | Activa |
| Support_AreaHealOverTime | Area | Ally | HealOverTime | - | Activa |
| Support_EnemyDebuffDefense | Direct | Hostile | DebuffDefense | - | Debug/advanced |

## 3. Runtime

- `SkillCaster`: valida carga y contexto, resuelve impactos, emite eventos visuales y aplica effects. `AnySkillImpactsResolvedForVisuals` se emite antes de aplicar effects.
- `SkillHitCollector`: construye impactos por pattern y aplica modifiers.
- `SkillEffect`: contrato base para aplicar comportamiento por impacto.
- `SkillModifier`: transforma o agrega impactos antes de los effects.
- `StatusEffectController`: mantiene status activos, expiracion y ticks periodicos.
- `LifeController`: centraliza dano, curacion, muerte y consumo de shield antes de HP.
- `ShieldController`: mantiene escudo temporal, absorbe dano entrante y expira por tiempo.

## 4. Effects Implementados

- `DamageSkillEffect`: aplica dano via `LifeController.TakeDamage`.
- `HealSkillEffect`: aplica curacion via `LifeController.Heal`.
- `ApplyStatusSkillEffect`: aplica status, incluidos buff, debuff, taunt, stun, slow, DoT y HoT.
- `KnockbackSkillEffect`: empuja unidades en gameplay despues del evento visual.
- `ShieldSkillEffect`: aplica shield temporal.
- `SummonUnitSkillEffect`: existe en runtime, pero no hay assets V2 activos de Summon; requiere rediseno de skill valida Area/Zone antes de uso real.
- DoT/HoT: se modelan como status con ticks periodicos.

## 5. Modifiers Implementados

- `SplashSkillModifier`: agrega impactos secundarios alrededor del impacto primario.
- `PiercingSkillModifier`: agrega impactos sobre una linea.
- `BounceSkillModifier`: encadena impactos y usa `ChainIndex` para orden visual.
- `ExplosiveSkillModifier`: agrega impactos alrededor de centros de explosion. El caso Direct + Explosive simple se representa como una sola explosion en el impacto primario.

## 6. Placeholder VFX

`SkillDebugVfxPresenter` es debug/placeholder, no VFX final. Escucha eventos de skill y ticks de status.

- Direct: `VFX_TargetPoint` sobre el impacto primario, color por efecto dominante.
- Area: `VFX_AreaCircle` en el centro resuelto, color por efecto dominante. No fabrica primary artificial.
- Line: linea hasta impacto real; Line + Piercing se extiende a `LineLengthInCells`.
- Splash: target primario, circulo en primary y markers secundarios.
- Explosive simple: un circulo en primary y markers secundarios.
- Bounce: primary marker, secundarios y `VFX_BounceLink` entre impactos por `ChainIndex`.
- Knockback: color cyan, `UnitVisualBumpView` como feedback principal y `VFX_Knockback` como fallback.
- Shield: color celeste, `VFX_Shield` sobre objetivos.
- Status/Buff/Debuff: `VFX_Status` con color buff o debuff.
- DoT/HoT: pulso pequeno por `StatusEffectController.EffectTickResolved`.
- Projectile: visual-only usando `CombatProjectileVisual`; no retrasa effects ni confirma arrival.

## 7. Tool de Prueba

- `TestMapScene` contiene `CombatDebugVisuals` con `SkillDebugVfxPresenter` y prefabs placeholder asignados.
- `CreatureCombatDebugTool` permite spawnear criaturas, asignar team, attach al grid, forzar carga, castear skill sobre target/celda, ejecutar basic attack, matar y limpiar.
- Esta tool permite probar skills y VFX sin flujo completo de combate.
- Las pruebas de criaturas deben usar los prefabs activos `Human_*` y `Orc_*` por rol. Las variantes futuras deben crearse a partir de esos prefabs, no desde prefabs genericos legacy.

## 8. Provisional / Debug / Deprecated

- `Tank_AreaTaunt`: provisional activa hasta formalizar Taunt en matriz.
- Summon: runtime existente, pero los assets invalidos `Support_SpawnMinions`, `Tank_SpawnMinions` y `Effect_Summon_MinorMinion` fueron eliminados; debe redisenarse como Area/Zone antes de volver al catalogo.
- MultiTarget: runtime parcial, pero no formalizado para criatura real.
- Multi-modifier: permitido tecnicamente, pero advanced/debug hasta tener semantica y metadata visual.
- Legacy `SE_*` y `Skills/Deprecated`: eliminados del proyecto tras confirmar que no tenian referencias reales externas.
- Documentacion vieja fragmentada: reemplazada por este overview cuando este presente en el repo.

## 9. Limitaciones Conocidas

- `Projectile` no es gameplay real.
- El evento VFX de skill ocurre antes de aplicar effects.
- `SkillImpact` no expone origen de modifier, por lo que multi-modifier no puede representarse con precision.
- `Area` ignora `MaxTargets`; `MaxTargets = 1` en Area es confuso.
- Shield no tiene UI final.
- CastTime/Channel no tienen VFX dedicado.
- GroundCell no tiene UI real de seleccion.
- Persistent, Periodic, Expandable y Accumulative no tienen runtime.

## 10. Proximos Pasos

1. Formalizar Summon como Area/Zone.
2. Formalizar MultiTarget.
3. Agregar metadata de origen de impactos para modifiers.
4. Implementar modifiers avanzados solo despues de definir semantica.
