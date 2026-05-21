# Skills Runtime Validation Harness

`SkillRuntimeTestHarness` is the manual runtime driver for skill validation. It always goes through the real `SkillCaster` flow, so `ImpactPattern`, `SkillModifier[]`, `SkillImpact` and `SkillEffect[]` are resolved exactly as they are in gameplay.

## Setup

1. Open a combat scene with a live `RoomContext`.
2. Add `SkillRuntimeTestHarness` to any GameObject.
3. Assign:
   - `Caster`
   - `Primary Target` when the skill requires one
   - `Skill To Test`
   - `Force Full Charge`
   - `Cast Key`
   - `Log Impacts`

If you also want internal cast pipeline logs, enable `_debugLogs` on the same `SkillCaster`.

## Usage

Press `T` in Play Mode. The harness:

- optionally refills charge,
- asks `SkillCaster` to cast the selected `SkillData`,
- keeps the normal cast rules and timing,
- logs whether the cast was accepted,
- logs the resolved impacts when the cast succeeds.

The harness does not call `SkillEffect` directly and does not build a parallel runtime path.

## Impact Logs

When `Log Impacts` is enabled, the harness prints:

- skill used,
- caster,
- primary target,
- impact count,
- one line per impact with `Kind`, `TargetUnit`, `Cell`, `IsPrimaryImpact` and `ChainIndex`.

## Escena de validacion

- Escena: `Assets/Combat/Testing/Skills/Scenes/SkillRuntimeValidationScene.unity`
- Prefabs de caso: `Assets/Core/Data/Scriptable Objects/Combat/Skills/V2/TestPrefabs/`
- Controller runtime: `Assets/Combat/Testing/Skills/SkillRuntimeValidationSceneController.cs`
- Selector runtime: `Assets/Combat/Testing/Skills/SkillRuntimeTestCaseSelector.cs`

### Como abrirla

1. Abre `SkillRuntimeValidationScene.unity`.
2. Entra en Play Mode.
3. `SkillRuntimeValidationRoot` configura `RoomContext`, `RoomGrid` y deja la sala en `Deployment` para bloquear la IA.

### Como seleccionar y disparar casos

- `Y`: avanza al siguiente caso activo.
- `T`: ejecuta el caso activo.
- El selector loguea el nombre del caso activo.
- Todos los casos V2 usan `Force Full Charge = true` y `Log Impacts = true`.

### Que logs mirar

- `SkillRuntimeTestCaseSelector`: caso activo.
- `SkillRuntimeTestHarness`: skill solicitada, caster, target, resultado del cast e impactos.
- `SkillCaster`: logs internos opcionales si `_debugLogs` esta activo.

### Casos V2 disponibles

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

### Layout cubierto por la escena

- `Single target`: `Enemy_SingleTarget`
- `Area / Splash / Explosive`: cluster alrededor de `Enemy_AreaPrimary`
- `Line / Piercing`: `Enemy_LinePrimary` y followers alineados, con un enemigo fuera de eje
- `Bounce`: cadena de rebote con un objetivo fuera de rango
- `MultiTarget`: mas enemigos que `MaxTargets`
- `SelfHeal`: caster aliado con vida reducida
- `Ally heal / buff`: `Ally_HealTarget` con vida reducida

### Resultados esperados

- `DirectDamage`: un impacto hostil directo.
- `LineDamage`: solo impactos alineados.
- `PiercingLineDamage`: varios impactos alineados.
- `MultiTargetDamage`: no supera `MaxTargets`.
- `DirectStun`: aplica dano y stun al target valido.
- `AreaTaunt`: aplica taunt alrededor del caster.
- `AreaHeal`: cura aliados validos en area.
- `SpawnMinions`: usa `SummonUnitSkillEffect`, no `SkillShape`.
- `Splash`, `Explosive` y `Bounce`: alteran el set final de `SkillImpact` sin salirse del contrato declarativo.
