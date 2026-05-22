# Creature Duel Sandbox

`CreatureDuelTestController` agrega un sandbox manual minimo para probar dos criaturas en una escena comun, sin `RoomGrid`, sin `CombatRoomController` y sin dependencias de sala.

## Proposito

- probar ataque basico real por input manual
- verificar dano recibido, muerte y lifecycle
- verificar carga de skill por basic action
- probar skills directas compatibles con contexto sin grid

No reemplaza `SkillRuntimeValidationScene`.

## Como usarlo

1. Crea o abre una escena comun.
2. Coloca dos criaturas con sus componentes runtime normales.
3. Ubicalas a distancia valida para su basic action.
4. Agrega `CreatureDuelTestController` a cualquier `GameObject` de la escena.
5. Asigna:
   - `_attacker`
   - `_defender`
   - `_secondaryDefenders` si quieres probar skills que afecten a mas de una unidad
6. En Play Mode manual:
   - `Alpha1`: intenta la basic action real del attacker
   - `Alpha2`: intenta la skill del attacker

## Setup minimo por criatura

Obligatorio para que una unidad funcione en el sandbox:

- `Unit`
- `LifeController`
- `RecruitableUnitState`
- `UnitDeathHandler`
- `UnitAffiliationState`
- `UnitMovement`
- `UnitCombat`
- `StatusEffectController`
- `UnitData` asignado en `Unit`

Para basic action:

- la unidad debe resolver una `IBasicAction`
- eso puede venir de:
  - un `BasicUnitAction` adjunto, o
  - el fallback de `Unit`, que usa `UnitCombat`
- si la basic action es hostil, el defender debe ser de team opuesto
- si la basic action es aliada, el defender debe ser del mismo team
- si la basic action aliada requiere target herido, el defender no puede estar en full health

Para skills:

- `SkillCaster`
- una `SkillData` compatible asignada en `UnitData`
- skills `Hostile` necesitan target de team opuesto
- skills `Ally` necesitan target del mismo team
- skills con `RequiresInjuredTarget` necesitan target herido
- skills `Self` se resuelven sobre el attacker
- si la skill necesita evaluar mas de una unidad sin sala, agrega esas unidades en `_secondaryDefenders`

No hace falta para este sandbox:

- `RoomGrid`
- `RoomContext`
- `CombatRoomController`
- `UnitBrain`

Notas de funcionamiento:

- `UnitCombat` aplica el dano/heal real, consume cooldown basico y notifica carga de skill.
- `LifeController` resuelve dano, muerte y lifecycle real.
- `UnitTargetValidator` usa `Vector3.Distance` cuando no hay `RoomGrid`, asi que el rango basico y el rango de skills directas siguen funcionando.
- `CreatureDuelTestController` emite warnings de setup si el attacker o defender tienen componentes faltantes.
- Cuando no hay `RoomContext`, el sandbox registra un roster debug local con attacker, defender y `_secondaryDefenders`.
- `SkillHitCollector` puede usar ese roster para patrones que operan sobre listas de unidades sin requerir una sala real.

## Que reutiliza

- La basic action usa `Unit.TryBasicActionForDebug(...)`, que pasa por `IBasicAction.Execute(...)` y por `UnitCombat`.
- La skill usa `SkillCaster.TryUse(...)`.
- No hay fake grid, fake room ni fake controller.

## Que se puede probar

- ataque basico manual
- heal basico si la unidad usa basic action aliada
- dano recibido
- muerte
- carga de skill por basic action
- cast interrumpido por muerte o estado, si la skill usa el flujo normal de `SkillCaster`
- skills `Direct` compatibles:
  - hostile
  - ally
  - self
- skills `Area`, `Line` y `MultiTarget` compatibles sin grid si tienen unidades suficientes en `_secondaryDefenders`
- modifiers compatibles sin grid:
  - `SplashSkillModifier`
  - `PiercingSkillModifier`
  - `ExplosiveSkillModifier`
  - `BounceSkillModifier`
- effects compatibles:
  - `DamageSkillEffect`
  - `HealSkillEffect`
  - `ApplyStatusSkillEffect`

## Que no se puede probar

- pathfinding
- IA completa
- movimiento automatico
- `GroundCell`
- summon
- knockback que dependa del grid
- cualquier skill que requiera `RoomGrid`, `roomUnits` o targeting espacial

Cuando una skill quede fuera de alcance, el controller loguea:

`This skill requires room/grid validation. Use SkillRuntimeValidationScene.`

## Cuando usar SkillRuntimeValidationScene

Usa `SkillRuntimeValidationScene` para:

- summon
- casos que dependan de room/grid real
- validacion visual completa del catalogo V2

## Que deberia hacer cada prefab debug

Los prefabs en `Assets/Combat/Prefabs/Debug/Attackers/` son los pensados para `CreatureDuelTestController`.

### Targets

- `Debug_Target_Ally`
  - usalo como target hostil para attackers enemigos
  - es el target normal para `Debug_DPS_*`, `Debug_Tank_*` y debuffs hostiles de support
- `Debug_Target_Enemy`
  - usalo como target aliado para supports enemigos
  - es el target normal para `Debug_Support_AllyHeal`, `Debug_Support_AreaHeal` y `Debug_Support_AllyBuffDamage`

### DPS

- `Debug_DPS_DirectDamage`
  - target recomendado: `Debug_Target_Ally`
  - deberia hacer: dano directo a un unico objetivo hostil
- `Debug_DPS_DirectStun`
  - target recomendado: `Debug_Target_Ally`
  - deberia hacer: aplicar stun directo a un unico objetivo hostil
- `Debug_DPS_DirectSplashDamage`
  - target recomendado: `Debug_Target_Ally`
  - secundarios recomendados: unidades cerca del target primario
  - deberia hacer: dano directo al primario y splash a secundarios dentro del radio
- `Debug_DPS_DirectExplosiveDamage`
  - target recomendado: `Debug_Target_Ally`
  - secundarios recomendados: unidades cerca del target primario
  - deberia hacer: dano directo al primario y explosion en el punto de impacto que dañe secundarios cercanos
- `Debug_DPS_DirectBounceDamage`
  - target recomendado: `Debug_Target_Ally`
  - secundarios recomendados: unidades validas cerca del ultimo impacto
  - deberia hacer: primer impacto al target primario y rebotes hacia otros objetivos hostiles validos
- `Debug_DPS_LineDamage`
  - target recomendado: `Debug_Target_Ally`
  - secundarios recomendados: unidades alineadas con la trayectoria
  - deberia hacer: dano en linea usando fallback espacial sin grid
- `Debug_DPS_PiercingLineDamage`
  - target recomendado: `Debug_Target_Ally`
  - secundarios recomendados: varias unidades alineadas
  - deberia hacer: dano en linea con piercing, afectando mas de un objetivo alineado
- `Debug_DPS_MultiTargetDamage`
  - target recomendado: `Debug_Target_Ally`
  - secundarios recomendados: varias unidades hostiles en `_secondaryDefenders`
  - deberia hacer: dano a multiples objetivos hostiles del roster debug
- `Debug_DPS_LinePiercingExplosiveDamage`
  - target recomendado: `Debug_Target_Ally`
  - secundarios recomendados: unidades alineadas y cercanas entre si
  - deberia hacer: linea con piercing y explosion en impactos, afectando multiples objetivos

### Tank

- `Debug_Tank_AreaTaunt`
  - target recomendado: `Debug_Target_Ally`
  - secundarios recomendados: varias unidades dentro del area
  - deberia hacer: taunt en area sobre objetivos hostiles dentro del radio
- `Debug_Tank_AreaDamage`
  - target recomendado: `Debug_Target_Ally`
  - secundarios recomendados: varias unidades dentro del area
  - deberia hacer: dano en area sobre el target primario y secundarios dentro del radio
- `Debug_Tank_MultiTargetDamage`
  - target recomendado: `Debug_Target_Ally`
  - secundarios recomendados: varias unidades hostiles en `_secondaryDefenders`
  - deberia hacer: dano a multiples objetivos hostiles del roster debug

### Support

- `Debug_Support_SelfHeal`
  - target recomendado: cualquiera, la skill es self
  - deberia hacer: curar al attacker
- `Debug_Support_AllyHeal`
  - target recomendado: `Debug_Target_Enemy`
  - condicion importante: el target tiene que estar herido
  - deberia hacer: curacion directa a un aliado
- `Debug_Support_AreaHeal`
  - target recomendado: `Debug_Target_Enemy`
  - secundarios recomendados: aliados heridos cerca del target primario
  - deberia hacer: curacion en area sobre aliados dentro del radio
- `Debug_Support_AllyBuffDamage`
  - target recomendado: `Debug_Target_Enemy`
  - deberia hacer: buff de dano a un aliado
- `Debug_Support_EnemyDebuffDefense`
  - target recomendado: `Debug_Target_Ally`
  - deberia hacer: debuff de defensa a un objetivo hostil

### Prefabs fuera del duel sandbox

Estos prefabs fueron movidos a `Assets/Combat/Prefabs/Debug/RequiresRoomValidation/Attackers/` porque siguen necesitando validacion de sala o grid real:

- `Debug_Tank_SpawnMinions`
  - usar en `SkillRuntimeValidationScene`
  - deberia hacer: summon alrededor del anchor configurado
- `Debug_Support_SpawnMinions`
  - usar en `SkillRuntimeValidationScene`
  - deberia hacer: summon alrededor del anchor configurado

## Notas

- La validacion sigue siendo manual desde Unity.
- Codex no debe abrir Unity ni ejecutar testing automatico para este flujo.
- Si el controller rechaza una basic action o skill, ahora loguea el motivo exacto de targeting, team, salud o rango.
