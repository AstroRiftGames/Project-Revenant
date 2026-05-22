# Skills Runtime Validation Harness

`SkillRuntimeTestHarness` queda como herramienta de validacion manual. Siempre pasa por el flujo real de `SkillCaster`, asi que `ImpactPattern`, `SkillModifier[]`, `SkillImpact` y `SkillEffect[]` se resuelven igual que en gameplay.

## Politica de validacion

- La validacion V2 se hace manualmente desde Unity.
- Codex no debe abrir Unity, entrar en Play Mode ni ejecutar batch validation para skills.
- No hay reporte automatico obligatorio.
- Si hace falta dejar constancia del estado de un caso, se registra manualmente fuera de este tooling.
- La escena ahora tambien muestra feedback visual temporal mediante `SkillDebugVfxPresenter`.

## Setup

1. Abre Unity manualmente.
2. Abre `Assets/Combat/Testing/Skills/Scenes/SkillRuntimeValidationScene.unity`.
3. Entra en Play Mode manualmente.
4. Verifica que `SkillRuntimeValidationRoot` deje la sala en `Deployment`.

Si quieres usar el harness en otra escena con `RoomContext`, puedes agregar `SkillRuntimeTestHarness` a cualquier GameObject y asignar:

- `Caster`
- `Primary Target` cuando la skill lo requiera
- `Skill To Test`
- `Force Full Charge`
- `Cast Key`
- `Log Impacts`

Si tambien quieres logs internos del pipeline de cast, habilita `_debugLogs` en el mismo `SkillCaster`.

## Uso manual

- `Y`: avanza al siguiente caso activo.
- `T`: ejecuta el caso activo.
- El selector loguea el nombre del caso activo.
- Todos los casos V2 usan `Force Full Charge = true` y `Log Impacts = true`.
- `SkillDebugVfxPresenter` muestra lineas, areas, rebotes, summon y markers temporales sobre impactos resueltos.

El harness:

- puede recargar charge antes del cast,
- pide a `SkillCaster` que use la `SkillData` configurada,
- mantiene las reglas normales de cast y timing,
- loguea si el cast fue aceptado,
- loguea los `SkillImpact` resueltos cuando la skill completa.

El harness no llama `SkillEffect` directamente y no construye un runtime paralelo.

## Impact Logs

Cuando `Log Impacts` esta activo, el harness imprime:

- skill usada,
- caster,
- primary target,
- cantidad de impactos,
- una linea por impacto con `Kind`, `TargetUnit`, `Cell`, `IsPrimaryImpact` y `ChainIndex`.

## Escena de validacion

- Escena: `Assets/Combat/Testing/Skills/Scenes/SkillRuntimeValidationScene.unity`
- Prefabs de caso: `Assets/Core/Data/Scriptable Objects/Combat/Skills/V2/TestPrefabs/`
- Controller runtime: `Assets/Combat/Testing/Skills/SkillRuntimeValidationSceneController.cs`
- Selector runtime: `Assets/Combat/Testing/Skills/SkillRuntimeTestCaseSelector.cs`
- Feedback visual temporal: `Assets/Combat/Scripts/Abilities/Visuals/SkillDebugVfxPresenter.cs`

## Casos V2 disponibles

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

## Layout cubierto por la escena

- `Single target`: `Enemy_SingleTarget`
- `Area / Splash / Explosive`: cluster alrededor de `Enemy_AreaPrimary`
- `Line / Piercing`: `Enemy_LinePrimary` y followers alineados, con un enemigo fuera de eje
- `Bounce`: cadena de rebote con un objetivo fuera de rango
- `MultiTarget`: mas enemigos que `MaxTargets`
- `SelfHeal`: caster aliado con vida reducida
- `Ally heal / buff`: `Ally_HealTarget` con vida reducida

## Resultados esperados

- `DirectDamage`: un impacto hostil directo.
- `LineDamage`: solo impactos alineados.
- `PiercingLineDamage`: varios impactos alineados.
- `MultiTargetDamage`: no supera `MaxTargets`.
- `DirectStun`: aplica dano y stun al target valido.
- `AreaTaunt`: aplica taunt alrededor del caster.
- `AreaHeal`: cura aliados validos en area.
- `SpawnMinions`: usa `SummonUnitSkillEffect`, no `SkillShape`.
- `Splash`, `Explosive` y `Bounce`: alteran el set final de `SkillImpact` sin salirse del contrato declarativo.

## Deuda y alcance

- La validacion automatizada fue retirada por friccion operativa.
- Los assets, prefabs y la escena siguen disponibles para validacion manual.
- `GroundCell` no debe abrirse hasta validar manualmente los casos criticos del catalogo V2.
- Casos criticos pendientes:
  - `Tank_AreaDamage`
  - `DPS_DirectExplosiveDamage`
  - `DPS_DirectBounceDamage`
  - `DPS_LinePiercingExplosiveDamage`
  - `Tank_SpawnMinions`
  - `Support_SpawnMinions`
